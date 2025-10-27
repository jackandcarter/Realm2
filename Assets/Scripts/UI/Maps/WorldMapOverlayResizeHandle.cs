using UnityEngine;
using UnityEngine.EventSystems;

namespace Client.UI.Maps
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class WorldMapOverlayResizeHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private WorldMapOverlayController _controller;

        internal void Initialize(WorldMapOverlayController controller)
        {
            _controller = controller;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _controller?.BeginWindowResize(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _controller?.ResizeWindow(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _controller?.EndWindowResize();
        }
    }
}
