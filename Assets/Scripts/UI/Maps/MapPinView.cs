using Client.Map;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Client.UI.Maps
{
    /// <summary>
    /// Handles binding a map pin prefab to runtime data, relaying tooltip requests and keeping highlight visuals in sync
    /// with the <see cref="MapPinService"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapPinView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerMoveHandler
    {
        [SerializeField] private MapPinService service;
        [SerializeField] private GameObject selectionHighlight;
        [SerializeField] private GameObject hoverHighlight;
        [SerializeField] private UnityEvent<string> onTooltipRequested;
        [SerializeField] private UnityEvent onTooltipCleared;
        [SerializeField] private MapTooltipController.TooltipContext tooltipContext = MapTooltipController.TooltipContext.WorldMap;
        [SerializeField] private RectTransform tooltipRootOverride;

        private MapPinData _data;
        private bool _subscribed;

        /// <summary>
        /// Current data bound to this view.
        /// </summary>
        public MapPinData Data => _data;

        private void Awake()
        {
            if (service == null)
            {
                service = FindObjectOfType<MapPinService>(true);
            }
        }

        private void OnEnable()
        {
            Subscribe();
            RefreshHighlights();
        }

        /// <summary>
        /// Binds the provided data to the view.
        /// </summary>
        public void SetData(MapPinData data)
        {
            if (_data == data)
            {
                return;
            }

            _data = data;
            RefreshHighlights();
        }

        /// <summary>
        /// Allows runtime configuration of the service reference.
        /// </summary>
        public void SetService(MapPinService targetService)
        {
            if (service == targetService)
            {
                return;
            }

            Unsubscribe();
            service = targetService;
            Subscribe();
            RefreshHighlights();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_data == null)
            {
                return;
            }

            SetHover(true);

            if (!string.IsNullOrWhiteSpace(_data.Tooltip))
            {
                onTooltipRequested?.Invoke(_data.Tooltip);
            }

            var tooltipController = MapTooltipController.Instance;
            if (tooltipController != null)
            {
                var parent = tooltipRootOverride != null ? tooltipRootOverride : transform.parent as RectTransform;
                tooltipController.ShowTooltip(_data, parent, tooltipContext,
                    eventData != null ? eventData.position : (Vector2)Input.mousePosition,
                    eventData != null ? eventData.pressEventCamera : null);
            }

            service?.SetHighlightedPin(_data);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHover(false);
            onTooltipCleared?.Invoke();

            var tooltipController = MapTooltipController.Instance;
            tooltipController?.HideTooltip(tooltipContext);

            if (service != null && service.HighlightedPin == _data)
            {
                service.ClearHighlightedPin();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (_data == null)
            {
                return;
            }

            service?.SetSelectedPin(_data);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (_data == null || tooltipContext != MapTooltipController.TooltipContext.WorldMap)
            {
                return;
            }

            var tooltipController = MapTooltipController.Instance;
            tooltipController?.UpdateCursorPosition(
                eventData != null ? eventData.position : (Vector2)Input.mousePosition,
                eventData != null ? eventData.pressEventCamera : null);
        }

        private void Subscribe()
        {
            if (_subscribed || service == null)
            {
                return;
            }

            service.SelectedPinChanged += HandleSelectedPinChanged;
            service.HighlightedPinChanged += HandleHighlightedPinChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || service == null)
            {
                return;
            }

            service.SelectedPinChanged -= HandleSelectedPinChanged;
            service.HighlightedPinChanged -= HandleHighlightedPinChanged;
            _subscribed = false;
        }

        private void OnDisable()
        {
            Unsubscribe();
            SetHover(false);
            SetSelection(false);

            var tooltipController = MapTooltipController.Instance;
            tooltipController?.HideTooltip(tooltipContext);
        }

        private void HandleSelectedPinChanged(MapPinData pin)
        {
            SetSelection(pin != null && pin == _data);
        }

        private void HandleHighlightedPinChanged(MapPinData pin)
        {
            if (pin != null && pin != _data)
            {
                if (service != null && service.HighlightedPin != pin)
                {
                    return;
                }

                SetHover(false);
                return;
            }

            SetHover(pin != null);
        }

        private void RefreshHighlights()
        {
            SetSelection(service != null && service.SelectedPin == _data);
            SetHover(service != null && service.HighlightedPin == _data);
        }

        private void SetSelection(bool active)
        {
            if (selectionHighlight != null)
            {
                selectionHighlight.SetActive(active);
            }
        }

        private void SetHover(bool active)
        {
            if (hoverHighlight != null)
            {
                hoverHighlight.SetActive(active);
            }
        }
    }
}
