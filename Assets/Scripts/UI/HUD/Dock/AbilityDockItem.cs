using Client.CharacterCreation;
using Client.Combat;
using Realm.UI.Tooltips;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Client.UI.HUD.Dock
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(CanvasGroup))]
    public class AbilityDockItem : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IDropHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text hotkeyLabel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button button;
        [SerializeField] private DockItemAnimator dockAnimator;

        private ClassAbilityDockModule _owner;
        private ClassAbilityCatalog.ClassAbilityDockEntry _entry;
        private string _layoutId;
        private bool _isPlaceholder;
        private bool _isLocked;
        private bool _dragging;
        private bool _cooldownReveal;
        private DockAbilityState _lastState;
        private bool _hasState;
        private CombatTooltipTrigger _tooltipTrigger;

        public string AbilityId => _entry.AbilityId;
        public string LayoutId => _layoutId;
        internal bool IsPlaceholder => _isPlaceholder;

        internal void Initialize(ClassAbilityDockModule owner)
        {
            _owner = owner;

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (canvasGroup == null)
            {
                Debug.LogWarning("AbilityDockItem is missing a CanvasGroup component.", this);
            }

            if (iconImage == null)
            {
                iconImage = GetComponent<Image>();
            }

            if (label == null)
            {
                label = GetComponentInChildren<TMP_Text>();
            }

            if (hotkeyLabel == null)
            {
                var texts = GetComponentsInChildren<TMP_Text>(true);
                foreach (var text in texts)
                {
                    if (text != label)
                    {
                        hotkeyLabel = text;
                        break;
                    }
                }
            }

            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
                button.onClick.AddListener(HandleClick);
            }

            if (dockAnimator == null)
            {
                dockAnimator = GetComponent<DockItemAnimator>();
                if (dockAnimator == null)
                {
                    dockAnimator = gameObject.AddComponent<DockItemAnimator>();
                }
            }

            EnsureCooldownOverlay();
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }

        internal void Bind(ClassAbilityCatalog.ClassAbilityDockEntry entry, Sprite icon)
        {
            _entry = entry;
            _layoutId = entry.AbilityId;
            _isPlaceholder = false;

            if (label != null)
            {
                label.text = string.IsNullOrWhiteSpace(entry.DisplayName) ? entry.AbilityId : entry.DisplayName;
            }

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (button != null)
            {
                button.interactable = true;
            }

            dockAnimator?.ClearAbilityTiming();
            _hasState = false;
            UpdateCooldownOverlay();
            ConfigureTooltip(entry.AbilityId);

            gameObject.name = $"AbilityDockItem_{entry.AbilityId}";
        }

        internal void BindPlaceholder(string placeholderId)
        {
            _entry = default;
            _layoutId = placeholderId;
            _isPlaceholder = true;
            _isLocked = true;

            if (label != null)
            {
                label.text = string.Empty;
            }

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = true;
            }

            if (button != null)
            {
                button.interactable = false;
            }

            dockAnimator?.ClearAbilityTiming();
            _hasState = false;
            UpdateCooldownOverlay();
            DisableTooltip();

            gameObject.name = $"AbilityDockSlot_{placeholderId}";
        }

        internal void SetHotkeyLabel(string hotkey)
        {
            if (hotkeyLabel != null)
            {
                hotkeyLabel.text = hotkey;
            }
        }

        internal void SetAbilityState(DockAbilityState state)
        {
            _lastState = state;
            _hasState = true;
            dockAnimator?.SetAbilityTiming(
                state.CastDuration,
                state.CastRemaining,
                state.CooldownDuration,
                state.CooldownRemaining);
            UpdateCooldownOverlay();
        }

        internal void SetCooldownOverlayVisible(bool visible)
        {
            _cooldownReveal = visible;
            UpdateCooldownOverlay();
        }

        internal void SetLocked(bool locked)
        {
            _isLocked = locked;
            if (button != null)
            {
                button.interactable = !locked;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = locked ? 0.4f : 1f;
            }
        }

        private void HandleClick()
        {
            if (_owner == null || _isPlaceholder || _isLocked || _dragging)
            {
                return;
            }

            _owner.ActivateAbility(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_owner == null)
            {
                return;
            }

            if (_isPlaceholder)
            {
                return;
            }

            if (_isLocked)
            {
                return;
            }

            if (canvasGroup == null)
            {
                return;
            }

            if (dockAnimator != null)
            {
                dockAnimator.enabled = false;
            }

            _dragging = true;
            canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false;
            _owner.BeginDrag(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging)
            {
                return;
            }

            transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_owner == null || !_dragging)
            {
                return;
            }

            if (canvasGroup == null)
            {
                return;
            }

            _dragging = false;
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            _owner.EndDrag(this);

            if (dockAnimator != null)
            {
                dockAnimator.enabled = true;
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (_owner == null)
            {
                return;
            }

            var source = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<AbilityDockItem>()
                : null;

            if (source == null || source == this)
            {
                return;
            }

            _owner.RequestSwap(source, this);
        }

        private void EnsureCooldownOverlay()
        {
            if (cooldownOverlay != null)
            {
                cooldownOverlay.enabled = false;
                return;
            }

            var overlayObject = new GameObject("CooldownOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlayObject.transform.SetParent(transform, false);
            overlayObject.transform.SetSiblingIndex(0);

            var overlayRect = overlayObject.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            overlayRect.pivot = new Vector2(0.5f, 0.5f);

            cooldownOverlay = overlayObject.GetComponent<Image>();
            cooldownOverlay.raycastTarget = false;
            cooldownOverlay.color = new Color(0f, 0f, 0f, 0.55f);
            cooldownOverlay.type = Image.Type.Filled;
            cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
            cooldownOverlay.fillOrigin = (int)Image.Origin360.Top;
            cooldownOverlay.fillClockwise = false;
            cooldownOverlay.enabled = false;
        }

        private void UpdateCooldownOverlay()
        {
            if (cooldownOverlay == null)
            {
                return;
            }

            if (!_cooldownReveal || !_hasState || _isPlaceholder)
            {
                cooldownOverlay.enabled = false;
                return;
            }

            if (_lastState.CooldownDuration <= 0f || _lastState.CooldownRemaining <= 0f)
            {
                cooldownOverlay.enabled = false;
                return;
            }

            cooldownOverlay.enabled = true;
            cooldownOverlay.fillAmount = Mathf.Clamp01(_lastState.CooldownRemaining / _lastState.CooldownDuration);
        }

        private void ConfigureTooltip(string abilityId)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                DisableTooltip();
                return;
            }

            var ability = AbilityRegistry.GetAbility(abilityId);
            if (ability == null)
            {
                DisableTooltip();
                return;
            }

            _tooltipTrigger ??= gameObject.GetComponent<CombatTooltipTrigger>() ?? gameObject.AddComponent<CombatTooltipTrigger>();
            _tooltipTrigger.Configure(null, CombatTooltipSourceType.Ability, ability);
        }

        private void DisableTooltip()
        {
            if (_tooltipTrigger != null)
            {
                _tooltipTrigger.Configure(null, CombatTooltipSourceType.Ability, null);
            }
        }
    }
}
