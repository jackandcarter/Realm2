using UnityEngine;
using UnityEngine.EventSystems;

namespace Client.UI.Maps
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class WorldMapOverlayDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private WorldMapOverlayController _controller;

        internal void Initialize(WorldMapOverlayController controller)
        {
            _controller = controller;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _controller?.BeginWindowDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _controller?.DragWindow(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _controller?.EndWindowDrag();
        }
    }
}
