using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Client.UI.HUD.Dock
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class DockMagnifier : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        [Header("Magnification")]
        [SerializeField] private float influenceRadius = 160f;
        [SerializeField] private float maxScaleIncrease = 0.45f;
        [SerializeField] private float maxLift = 12f;
        [SerializeField] private AnimationCurve influenceCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private readonly List<DockItemAnimator> _items = new();
        private RectTransform _rectTransform;
        private bool _pointerInside;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            RefreshItems();
        }

        private void OnEnable()
        {
            RefreshItems();
        }

        private void OnDisable()
        {
            ClearInfluence();
        }

        private void OnTransformChildrenChanged()
        {
            RefreshItems();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _pointerInside = true;
            ApplyInfluence(eventData.position, eventData.enterEventCamera);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _pointerInside = false;
            ClearInfluence();
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (!_pointerInside)
            {
                return;
            }

            ApplyInfluence(eventData.position, eventData.enterEventCamera);
        }

        private void RefreshItems()
        {
            _items.Clear();
            GetComponentsInChildren(true, _items);
        }

        private void ApplyInfluence(Vector2 screenPointer, Camera eventCamera)
        {
            if (_rectTransform == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform,
                screenPointer,
                eventCamera,
                out var localPointer);

            foreach (var item in _items)
            {
                if (item == null)
                {
                    continue;
                }

                var itemRect = item.GetComponent<RectTransform>();
                var localItem = _rectTransform.InverseTransformPoint(itemRect.position);
                var distance = Mathf.Abs(localPointer.x - localItem.x);
                var t = Mathf.Clamp01(1f - (distance / Mathf.Max(1f, influenceRadius)));
                var weight = influenceCurve != null ? influenceCurve.Evaluate(t) : t;
                item.SetHoverInfluence(weight * maxScaleIncrease, weight * maxLift);
            }
        }

        private void ClearInfluence()
        {
            foreach (var item in _items)
            {
                item?.ClearHoverInfluence();
            }
        }
    }
}
