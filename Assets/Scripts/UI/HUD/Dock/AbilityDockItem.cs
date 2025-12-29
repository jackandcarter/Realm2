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
        [SerializeField] private CanvasGroup canvasGroup;

        private ClassAbilityDockModule _owner;
        private ClassAbilityCatalog.ClassAbilityDockEntry _entry;
        private bool _dragging;

        public string AbilityId => _entry.AbilityId;

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
        }

        internal void Bind(ClassAbilityCatalog.ClassAbilityDockEntry entry, Sprite icon)
        {
            _entry = entry;

            if (label != null)
            {
                label.text = string.IsNullOrWhiteSpace(entry.DisplayName) ? entry.AbilityId : entry.DisplayName;
            }

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            gameObject.name = $"AbilityDockItem_{entry.AbilityId}";
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_owner == null)
            {
                return;
            }

            if (canvasGroup == null)
            {
                return;
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
