using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Client.UI.HUD.Dock
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class DockCooldownHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private readonly List<AbilityDockItem> _items = new();
        private bool _pointerInside;

        private void Awake()
        {
            RefreshItems();
        }

        private void OnEnable()
        {
            RefreshItems();
            if (_pointerInside)
            {
                ApplyReveal(true);
            }
        }

        private void OnDisable()
        {
            ApplyReveal(false);
        }

        private void OnTransformChildrenChanged()
        {
            RefreshItems();
            ApplyReveal(_pointerInside);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _pointerInside = true;
            ApplyReveal(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _pointerInside = false;
            ApplyReveal(false);
        }

        private void RefreshItems()
        {
            _items.Clear();
            GetComponentsInChildren(true, _items);
        }

        private void ApplyReveal(bool reveal)
        {
            foreach (var item in _items)
            {
                item?.SetCooldownOverlayVisible(reveal);
            }
        }
    }
}
