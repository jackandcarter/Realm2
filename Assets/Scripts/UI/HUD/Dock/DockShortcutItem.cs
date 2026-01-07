using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Client.UI.HUD.Dock
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(CanvasGroup))]
    public class DockShortcutItem : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IDropHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text label;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button button;
        [SerializeField] private DockItemAnimator dockAnimator;

        private DockShortcutEntry _entry;
        private DockShortcutSection _owner;
        private bool _dragging;

        public string ShortcutId => _entry.ShortcutId;
        internal DockShortcutSection Owner => _owner;

        internal void Initialize(DockShortcutSection owner)
        {
            _owner = owner;

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (iconImage == null)
            {
                iconImage = GetComponent<Image>();
            }

            if (label == null)
            {
                label = GetComponentInChildren<TMP_Text>();
            }

            if (dockAnimator == null)
            {
                dockAnimator = GetComponent<DockItemAnimator>();
                if (dockAnimator == null)
                {
                    dockAnimator = gameObject.AddComponent<DockItemAnimator>();
                }
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClick);
            }
        }

        internal void Bind(DockShortcutEntry entry)
        {
            _entry = entry;

            if (iconImage != null)
            {
                iconImage.sprite = entry.Icon;
                iconImage.enabled = entry.Icon != null;
            }

            if (label != null)
            {
                label.text = string.IsNullOrWhiteSpace(entry.DisplayName) ? entry.ShortcutId : entry.DisplayName;
            }

            if (!string.IsNullOrWhiteSpace(entry.ShortcutId))
            {
                gameObject.name = $"DockShortcutItem_{entry.ShortcutId}";
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_owner == null || canvasGroup == null)
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
            if (!_dragging || canvasGroup == null)
            {
                return;
            }

            _dragging = false;
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;

            if (dockAnimator != null)
            {
                dockAnimator.enabled = true;
            }

            if (_owner != null && !_owner.IsDropTarget(eventData.pointerEnter))
            {
                _owner.RemoveShortcut(ShortcutId);
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (_owner == null)
            {
                return;
            }

            var source = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<DockShortcutItem>()
                : null;

            if (source == null || source == this)
            {
                return;
            }

            _owner.RequestSwap(source, this);
        }

        private void OnClick()
        {
            _owner?.ActivateShortcut(ShortcutId);
        }
    }
}
