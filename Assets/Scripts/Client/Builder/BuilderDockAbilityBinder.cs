using System.Collections.Generic;
using Building;
using Client.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Client.Builder
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(BuilderAbilityController))]
    public class BuilderDockAbilityBinder : MonoBehaviour
    {
        [Header("Ability Data")]
        [SerializeField] private BuilderAbilitySet builderAbilities;

        [Header("Runtime Services")]
        [SerializeField] private BuilderAbilityController abilityController;

        [Header("UI")]
        [SerializeField] private RectTransform buttonContainer;
        [SerializeField] private GameObject buttonPrefab;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Color readyColor = new Color(0.3f, 0.8f, 0.5f, 1f);
        [SerializeField] private Color disabledColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        private readonly List<ButtonBinding> _buttons = new List<ButtonBinding>();
        private readonly Dictionary<string, BuilderAbilityRuntimeStatus> _statusLookup =
            new Dictionary<string, BuilderAbilityRuntimeStatus>(StringComparer.OrdinalIgnoreCase);

        private string _statusMessage = string.Empty;
        private string _selectionMessage = string.Empty;

        private bool _isMounted;
        private bool _hasPermissions;

        private void Reset()
        {
            abilityController = GetComponent<BuilderAbilityController>();
        }

        private void Awake()
        {
            if (abilityController == null)
            {
                abilityController = GetComponent<BuilderAbilityController>();
            }

            if (abilityController != null)
            {
                abilityController.AbilityStatusChanged += OnAbilityStatusChanged;
                abilityController.SelectionChanged += OnSelectionChanged;
                abilityController.AbilitySet = builderAbilities;
            }

            EnsureContainer();
            EnsureStatusLabel();
            BuildButtons();
        }

        private void OnEnable()
        {
            PlayerClassStateManager.ArkitectAvailabilityChanged += OnArkitectAvailabilityChanged;
            ApplyPermissions(PlayerClassStateManager.IsArkitectAvailable);
        }

        private void OnDisable()
        {
            PlayerClassStateManager.ArkitectAvailabilityChanged -= OnArkitectAvailabilityChanged;
        }

        private void OnDestroy()
        {
            if (abilityController != null)
            {
                abilityController.AbilityStatusChanged -= OnAbilityStatusChanged;
                abilityController.SelectionChanged -= OnSelectionChanged;
            }
        }

        public void Mount(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            var rectTransform = GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = new Vector2(0f, 0.5f);
            rectTransform.anchorMax = new Vector2(1f, 0.5f);
            rectTransform.sizeDelta = new Vector2(0f, 96f);
            rectTransform.anchoredPosition = Vector2.zero;
            gameObject.SetActive(true);
            _isMounted = true;
            RefreshButtons();
        }

        public void Unmount()
        {
            gameObject.SetActive(false);
            _isMounted = false;
            _statusMessage = string.Empty;
            _selectionMessage = string.Empty;
            RefreshDisplayedMessage();
        }

        public void OnAbilityStateChanged(string abilityId, bool enabled)
        {
            ApplyPermissions(enabled);
        }

        private void OnArkitectAvailabilityChanged(bool available)
        {
            ApplyPermissions(available);
        }

        private void ApplyPermissions(bool available)
        {
            _hasPermissions = available;
            RefreshButtons();
        }

        private void BuildButtons()
        {
            ClearButtons();
            if (builderAbilities == null)
            {
                return;
            }

            foreach (var ability in builderAbilities.Abilities)
            {
                if (ability == null || string.IsNullOrWhiteSpace(ability.AbilityId))
                {
                    continue;
                }

                var binding = CreateButton(ability);
                _buttons.Add(binding);
            }

            RefreshButtons();
        }

        private ButtonBinding CreateButton(BuilderAbilityDefinition definition)
        {
            var buttonRoot = buttonPrefab != null
                ? Instantiate(buttonPrefab, buttonContainer)
                : CreateDefaultButtonObject(definition.DisplayName);

            var button = buttonRoot.GetComponent<Button>();
            if (button == null)
            {
                button = buttonRoot.AddComponent<Button>();
            }

            var image = buttonRoot.GetComponent<Image>();
            if (image == null)
            {
                image = buttonRoot.AddComponent<Image>();
            }

            image.sprite = definition.GetIcon();
            image.color = disabledColor;

            button.onClick.AddListener(() => OnAbilityButtonClicked(definition.AbilityId));
            button.interactable = false;

            return new ButtonBinding(definition, button, image);
        }

        private GameObject CreateDefaultButtonObject(string label)
        {
            var go = new GameObject(string.IsNullOrWhiteSpace(label) ? "Ability" : label);
            go.transform.SetParent(buttonContainer, false);

            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(96f, 96f);

            var image = go.AddComponent<Image>();
            image.color = disabledColor;

            var textObj = new GameObject("Label");
            textObj.transform.SetParent(go.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObj.AddComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            return go;
        }

        private void ClearButtons()
        {
            foreach (var binding in _buttons)
            {
                if (binding.Button != null)
                {
                    binding.Button.onClick.RemoveAllListeners();
                }

                if (binding.Button != null)
                {
                    Destroy(binding.Button.gameObject);
                }
            }

            _buttons.Clear();
            _statusLookup.Clear();
        }

        private void RefreshButtons()
        {
            if (!_isMounted)
            {
                return;
            }

            SetStatusMessage(string.Empty);
            foreach (var binding in _buttons)
            {
                if (!_statusLookup.TryGetValue(binding.Definition.AbilityId, out var status))
                {
                    status = new BuilderAbilityRuntimeStatus(binding.Definition.AbilityId, binding.Definition, isReady: true, cooldownRemaining: 0f);
                }

                ApplyStatus(binding, status, allowSilent: true);
            }
        }

        private void OnAbilityStatusChanged(BuilderAbilityRuntimeStatus status)
        {
            _statusLookup[status.AbilityId] = status;
            var messageApplied = false;
            for (var i = 0; i < _buttons.Count; i++)
            {
                var binding = _buttons[i];
                if (!string.Equals(binding.Definition.AbilityId, status.AbilityId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                ApplyStatus(binding, status, allowSilent: false, ref messageApplied);
                _buttons[i] = binding;
            }

            if (!messageApplied)
            {
                SetStatusMessage(string.Empty);
            }
        }

        private void ApplyStatus(ButtonBinding binding, BuilderAbilityRuntimeStatus status, bool allowSilent)
        {
            var dummy = false;
            ApplyStatus(binding, status, allowSilent, ref dummy);
        }

        private void ApplyStatus(ButtonBinding binding, BuilderAbilityRuntimeStatus status, bool allowSilent, ref bool messageApplied)
        {
            var interactable = _hasPermissions && status.IsReady;
            binding.Button.interactable = interactable;
            binding.Image.color = interactable ? readyColor : disabledColor;

            var message = string.Empty;
            if (!_hasPermissions)
            {
                message = "Builder abilities require Arkitect access.";
            }
            else if (status.CooldownRemaining > 0.01f)
            {
                message = $"{binding.Definition.DisplayName} cooldown: {status.CooldownRemaining:F1}s";
            }
            else if (!status.IsReady)
            {
                message = $"{binding.Definition.DisplayName} cooling down...";
            }

            if (!allowSilent || !string.IsNullOrEmpty(message))
            {
                SetStatusMessage(message);
                messageApplied = !string.IsNullOrEmpty(message);
            }
        }

        private void EnsureContainer()
        {
            if (buttonContainer != null)
            {
                return;
            }

            var existing = transform.Find("AbilityButtons");
            if (existing != null)
            {
                buttonContainer = existing as RectTransform;
            }

            if (buttonContainer == null)
            {
                var go = new GameObject("AbilityButtons");
                go.transform.SetParent(transform, false);
                buttonContainer = go.AddComponent<RectTransform>();
                buttonContainer.anchorMin = new Vector2(0f, 0f);
                buttonContainer.anchorMax = new Vector2(1f, 1f);
                buttonContainer.offsetMin = new Vector2(8f, 32f);
                buttonContainer.offsetMax = new Vector2(-8f, -8f);
            }

            var layout = buttonContainer.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = buttonContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
        }

        private void EnsureStatusLabel()
        {
            if (statusLabel != null)
            {
                return;
            }

            var go = new GameObject("StatusLabel");
            go.transform.SetParent(transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(0f, 24f);
            rect.anchoredPosition = new Vector2(0f, 4f);

            statusLabel = go.AddComponent<Text>();
            statusLabel.alignment = TextAnchor.MiddleCenter;
            statusLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            statusLabel.color = Color.white;
            statusLabel.text = string.Empty;
        }

        private void SetStatusMessage(string message)
        {
            _statusMessage = string.IsNullOrWhiteSpace(message) ? string.Empty : message;
            RefreshDisplayedMessage();
        }

        private void OnSelectionChanged(ConstructionInstance instance)
        {
            if (instance == null)
            {
                _selectionMessage = string.Empty;
                RefreshDisplayedMessage();
                return;
            }

            _selectionMessage = instance.IsPlaced
                ? "Object placed. Use Float to reposition."
                : "Floating blueprint. Place to finalize.";
            RefreshDisplayedMessage();
        }

        private void OnAbilityButtonClicked(string abilityId)
        {
            if (abilityController == null || string.IsNullOrWhiteSpace(abilityId))
            {
                return;
            }

            if (!abilityController.TryActivate(abilityId))
            {
                SetStatusMessage("Ability on cooldown or unavailable.");
            }
        }

        private void RefreshDisplayedMessage()
        {
            if (statusLabel == null)
            {
                return;
            }

            var message = !string.IsNullOrEmpty(_statusMessage)
                ? _statusMessage
                : _selectionMessage;
            statusLabel.text = message;
        }

        private struct ButtonBinding
        {
            public readonly BuilderAbilityDefinition Definition;
            public readonly Button Button;
            public readonly Image Image;

            public ButtonBinding(BuilderAbilityDefinition definition, Button button, Image image)
            {
                Definition = definition;
                Button = button;
                Image = image;
            }
        }
    }
}
