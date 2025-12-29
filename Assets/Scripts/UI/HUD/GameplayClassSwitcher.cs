using System;
using System.Collections.Generic;
using Client;
using Client.CharacterCreation;
using Client.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Client.UI.HUD
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class GameplayClassSwitcher : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private CanvasGroup rootCanvasGroup;
        [SerializeField] private RectTransform buttonContainer;
        [SerializeField] private Button buttonTemplate;

        [Header("Styling")]
        [SerializeField] private Color activeColor = new Color(0.988f, 0.792f, 0.341f, 1f);
        [SerializeField] private Color inactiveColor = new Color(0.862f, 0.901f, 0.956f, 1f);
        [SerializeField] private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        [SerializeField] private float selectionDebounceSeconds = 0.25f;
        [SerializeField] private Vector2 buttonSize = new Vector2(120f, 40f);
        [SerializeField] private float buttonSpacing = 12f;

        private readonly Dictionary<string, ButtonBinding> _buttonBindings =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly List<string> _orderedClassIds = new();
        private readonly HashSet<string> _classScratch = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _removalScratch = new();

        private string _currentCharacterId;
        private string _activeClassId;
        private string _pendingClassId;
        private bool _selectionPending;
        private float _selectionCooldownUntil;

        private RectTransform _templateParent;

        private void Awake()
        {
            if (rootCanvasGroup == null)
            {
                rootCanvasGroup = GetComponent<CanvasGroup>();
                if (rootCanvasGroup == null)
                {
                    Debug.LogWarning("GameplayClassSwitcher requires a CanvasGroup component.", this);
                }
            }

            if (buttonContainer == null)
            {
                buttonContainer = GetComponent<RectTransform>();
            }

            if (buttonTemplate == null && buttonContainer != null)
            {
                buttonTemplate = buttonContainer.GetComponentInChildren<Button>(true);
            }

            if (buttonTemplate != null)
            {
                _templateParent = buttonTemplate.transform.parent as RectTransform;
                buttonTemplate.gameObject.SetActive(false);
                buttonTemplate.onClick.RemoveAllListeners();

                var templateRect = buttonTemplate.GetComponent<RectTransform>();
                if (templateRect != null && templateRect.sizeDelta.sqrMagnitude > 0f)
                {
                    buttonSize = templateRect.sizeDelta;
                }
            }
        }

        private void OnEnable()
        {
            SessionManager.SelectedCharacterChanged += OnSelectedCharacterChanged;
            ClassUnlockRepository.ClassUnlockStatesChanged += OnClassUnlockStatesChanged;
            PlayerClassStateManager.ActiveClassChanged += OnActiveClassChanged;

            _currentCharacterId = SessionManager.SelectedCharacterId;
            _activeClassId = NormalizeClassId(PlayerClassStateManager.ActiveClassId);

            RefreshUnlockedClasses();
            UpdateButtonStates();
            ApplyVisibility(_orderedClassIds.Count > 1);
        }

        private void OnDisable()
        {
            SessionManager.SelectedCharacterChanged -= OnSelectedCharacterChanged;
            ClassUnlockRepository.ClassUnlockStatesChanged -= OnClassUnlockStatesChanged;
            PlayerClassStateManager.ActiveClassChanged -= OnActiveClassChanged;
        }

        private void OnDestroy()
        {
            SessionManager.SelectedCharacterChanged -= OnSelectedCharacterChanged;
            ClassUnlockRepository.ClassUnlockStatesChanged -= OnClassUnlockStatesChanged;
            PlayerClassStateManager.ActiveClassChanged -= OnActiveClassChanged;

            foreach (var binding in _buttonBindings.Values)
            {
                DestroyButton(binding);
            }

            _buttonBindings.Clear();
        }

        private void OnSelectedCharacterChanged(string characterId)
        {
            _currentCharacterId = string.IsNullOrWhiteSpace(characterId) ? null : characterId;
            RefreshUnlockedClasses();
            UpdateButtonStates();
            ApplyVisibility(_orderedClassIds.Count > 1);
        }

        private void OnClassUnlockStatesChanged(string characterId, ClassUnlockState[] states)
        {
            if (!string.Equals(characterId, _currentCharacterId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            RefreshUnlockedClasses(states);
            UpdateButtonStates();
            ApplyVisibility(_orderedClassIds.Count > 1);
        }

        private void OnActiveClassChanged(string classId)
        {
            _activeClassId = NormalizeClassId(classId);
            _selectionPending = false;
            _pendingClassId = null;
            _selectionCooldownUntil = Mathf.Max(_selectionCooldownUntil, Time.unscaledTime + selectionDebounceSeconds);
            UpdateButtonStates();
        }

        private void RefreshUnlockedClasses(ClassUnlockState[] overrideStates = null)
        {
            _orderedClassIds.Clear();
            _classScratch.Clear();

            if (string.IsNullOrWhiteSpace(_currentCharacterId))
            {
                SyncButtons();
                return;
            }

            var states = overrideStates != null ? ClassUnlockUtility.SanitizeStates(overrideStates) :
                ClassUnlockRepository.GetStates(_currentCharacterId);

            if (states != null)
            {
                foreach (var state in states)
                {
                    if (state == null || !state.Unlocked)
                    {
                        continue;
                    }

                    var normalized = NormalizeClassId(state.ClassId);
                    if (normalized != null)
                    {
                        _classScratch.Add(normalized);
                    }
                }
            }

            var activeClass = NormalizeClassId(PlayerClassStateManager.ActiveClassId);
            if (activeClass != null)
            {
                _classScratch.Add(activeClass);
            }

            foreach (var definition in ClassCatalog.GetAllClasses())
            {
                var normalized = NormalizeClassId(definition?.Id);
                if (normalized != null && _classScratch.Remove(normalized))
                {
                    _orderedClassIds.Add(normalized);
                }
            }

            if (_classScratch.Count > 0)
            {
                var extras = ListPool<string>.Get();
                try
                {
                    extras.AddRange(_classScratch);
                    extras.Sort(StringComparer.OrdinalIgnoreCase);
                    _orderedClassIds.AddRange(extras);
                }
                finally
                {
                    ListPool<string>.Release(extras);
                }
            }

            SyncButtons();
        }

        private void SyncButtons()
        {
            _removalScratch.Clear();
            foreach (var kvp in _buttonBindings)
            {
                if (!_orderedClassIds.Contains(kvp.Key))
                {
                    DestroyButton(kvp.Value);
                    _removalScratch.Add(kvp.Key);
                }
            }

            foreach (var key in _removalScratch)
            {
                _buttonBindings.Remove(key);
            }

            for (var index = 0; index < _orderedClassIds.Count; index++)
            {
                var classId = _orderedClassIds[index];
                if (!_buttonBindings.TryGetValue(classId, out var binding))
                {
                    binding = CreateButtonBinding(classId);
                    if (binding == null)
                    {
                        continue;
                    }

                    _buttonBindings[classId] = binding;
                }

                binding.Root.transform.SetSiblingIndex(index);
                binding.Root.name = $"ClassSwitcherButton_{classId}";
                if (binding.Label != null)
                {
                    binding.Label.text = ResolveDisplayName(classId);
                }

                if (!binding.Root.activeSelf)
                {
                    binding.Root.SetActive(true);
                }
            }

            ApplyButtonLayout();
        }

        private ButtonBinding CreateButtonBinding(string classId)
        {
            if (buttonTemplate == null)
            {
                Debug.LogWarning("GameplayClassSwitcher is missing a button template.", this);
                return null;
            }

            var parent = buttonContainer != null ? buttonContainer : _templateParent != null ? _templateParent : transform as RectTransform;
            Button instance;
            if (Application.isPlaying)
            {
                instance = Instantiate(buttonTemplate, parent);
            }
            else
            {
                instance = UnityEngine.Object.Instantiate(buttonTemplate, parent);
            }

            var go = instance.gameObject;
            go.SetActive(true);

            var label = go.GetComponentInChildren<Text>(true);
            var canvasGroup = go.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                Debug.LogWarning($"GameplayClassSwitcher button '{go.name}' is missing a CanvasGroup component.", go);
            }

            instance.onClick.RemoveAllListeners();
            instance.onClick.AddListener(() => OnClassButtonClicked(classId));

            return new ButtonBinding
            {
                ClassId = classId,
                Root = go,
                Button = instance,
                Label = label,
                CanvasGroup = canvasGroup,
                TargetGraphic = instance.targetGraphic
            };
        }

        private void DestroyButton(ButtonBinding binding)
        {
            if (binding == null)
            {
                return;
            }

            if (binding.Button != null)
            {
                binding.Button.onClick.RemoveAllListeners();
            }

            if (binding.Root == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(binding.Root);
            }
            else
            {
                DestroyImmediate(binding.Root);
            }
        }

        private void ApplyButtonLayout()
        {
            if (_orderedClassIds.Count == 0)
            {
                return;
            }

            var step = buttonSize.x + buttonSpacing;
            var totalWidth = step * _orderedClassIds.Count - buttonSpacing;
            var start = -0.5f * totalWidth + 0.5f * buttonSize.x;

            for (var index = 0; index < _orderedClassIds.Count; index++)
            {
                if (!_buttonBindings.TryGetValue(_orderedClassIds[index], out var binding) ||
                    binding?.Root == null)
                {
                    continue;
                }

                var rectTransform = binding.Root.GetComponent<RectTransform>();
                if (rectTransform == null)
                {
                    Debug.LogWarning($"GameplayClassSwitcher button '{binding.Root.name}' is missing a RectTransform.", binding.Root);
                    continue;
                }

                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = buttonSize;
                rectTransform.anchoredPosition = new Vector2(start + index * step, 0f);
            }
        }

        private void OnClassButtonClicked(string classId)
        {
            if (string.IsNullOrWhiteSpace(classId))
            {
                return;
            }

            if (_selectionPending || Time.unscaledTime < _selectionCooldownUntil)
            {
                return;
            }

            if (string.Equals(_activeClassId, classId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _selectionPending = true;
            _pendingClassId = classId;
            UpdateButtonStates();

            var success = PlayerClassStateManager.TrySetActiveClass(classId);
            if (!success)
            {
                _selectionPending = false;
                _pendingClassId = null;
                _selectionCooldownUntil = Time.unscaledTime + selectionDebounceSeconds;
                UpdateButtonStates();
            }
        }

        private void UpdateButtonStates()
        {
            var allowInteraction = !_selectionPending && Time.unscaledTime >= _selectionCooldownUntil;
            foreach (var kvp in _buttonBindings)
            {
                var classId = kvp.Key;
                var binding = kvp.Value;
                if (binding == null || binding.Button == null)
                {
                    continue;
                }

                var isActive = string.Equals(_activeClassId, classId, StringComparison.OrdinalIgnoreCase);
                var isPending = _selectionPending && string.Equals(_pendingClassId, classId, StringComparison.OrdinalIgnoreCase);
                binding.Button.interactable = allowInteraction && !isActive;

                if (binding.TargetGraphic != null)
                {
                    binding.TargetGraphic.color = isActive ? activeColor : inactiveColor;
                }

                if (binding.CanvasGroup != null)
                {
                    if (isPending)
                    {
                        binding.CanvasGroup.alpha = 0.5f;
                    }
                    else if (binding.Button.interactable)
                    {
                        binding.CanvasGroup.alpha = 1f;
                    }
                    else
                    {
                        binding.CanvasGroup.alpha = disabledColor.a;
                    }
                }
            }
        }

        private void ApplyVisibility(bool shouldShow)
        {
            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.alpha = shouldShow ? 1f : 0f;
                rootCanvasGroup.interactable = shouldShow;
                rootCanvasGroup.blocksRaycasts = shouldShow;
            }

            if (buttonContainer != null && !shouldShow)
            {
                buttonContainer.anchoredPosition3D = buttonContainer.anchoredPosition3D; // noop to keep layout data
            }
        }

        private static string NormalizeClassId(string classId)
        {
            return string.IsNullOrWhiteSpace(classId) ? null : classId.Trim();
        }

        private static string ResolveDisplayName(string classId)
        {
            if (ClassCatalog.TryGetClass(classId, out var definition) && !string.IsNullOrWhiteSpace(definition?.DisplayName))
            {
                return definition.DisplayName;
            }

            return string.IsNullOrWhiteSpace(classId) ? "Unknown" : classId;
        }

        private class ButtonBinding
        {
            public string ClassId;
            public GameObject Root;
            public Button Button;
            public Text Label;
            public CanvasGroup CanvasGroup;
            public Graphic TargetGraphic;
        }
    }

    internal static class ListPool<T>
    {
        private static readonly Stack<List<T>> Pool = new();

        public static List<T> Get()
        {
            return Pool.Count > 0 ? Pool.Pop() : new List<T>();
        }

        public static void Release(List<T> list)
        {
            if (list == null)
            {
                return;
            }

            list.Clear();
            Pool.Push(list);
        }
    }
}
