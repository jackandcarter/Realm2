using Client.CharacterCreation;
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

            if (dockAnimator == null)
            {
                dockAnimator = GetComponent<DockItemAnimator>();
                if (dockAnimator == null)
                {
                    dockAnimator = gameObject.AddComponent<DockItemAnimator>();
                }
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
            dockAnimator?.SetAbilityTiming(
                state.CastDuration,
                state.CastRemaining,
                state.CooldownDuration,
                state.CooldownRemaining);
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
    }
}
