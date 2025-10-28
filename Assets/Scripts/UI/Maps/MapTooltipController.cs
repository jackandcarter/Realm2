using Client.Map;
using UnityEngine;
using UnityEngine.UI;

namespace Client.UI.Maps
{
    /// <summary>
    /// Shared tooltip manager used by both the world map overlay and the mini map. Handles positioning the tooltip
    /// within the provided bounds and clamps the result so it always remains visible.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapTooltipController : MonoBehaviour
    {
        public enum TooltipContext
        {
            WorldMap,
            MiniMap
        }

        private static MapTooltipController _instance;

        [Header("Prefab")]
        [SerializeField] private MapTooltipView tooltipPrefab;

        [Header("World Map")] 
        [SerializeField] private Vector2 worldMapCursorOffset = new(24f, -24f);
        [SerializeField] private Vector2 worldMapPadding = new(24f, 24f);

        [Header("Mini Map")]
        [SerializeField] private Vector2 miniMapFixedOffset = new(18f, -18f);
        [SerializeField] private Vector2 miniMapPadding = new(12f, 12f);

        private MapTooltipView _tooltipInstance;
        private RectTransform _currentParent;
        private TooltipContext _currentContext;
        private bool _visible;
        private Vector2 _currentCursorPosition;
        private Camera _currentCamera;

        /// <summary>
        /// Globally accessible instance. The first enabled controller in the scene becomes the active reference.
        /// </summary>
        public static MapTooltipController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<MapTooltipController>(FindObjectsInactive.Include);
                }

                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void LateUpdate()
        {
            if (_visible && _currentContext == TooltipContext.WorldMap)
            {
                UpdateWorldTooltipPosition(_currentCursorPosition, _currentCamera);
            }
        }

        /// <summary>
        /// Shows the tooltip for the provided pin within the supplied parent rect. World map tooltips follow the cursor.
        /// </summary>
        public void ShowTooltip(MapPinData pin, RectTransform parentRect, TooltipContext context, Vector2 screenPosition, Camera uiCamera)
        {
            if (pin == null || parentRect == null)
            {
                return;
            }

            EnsureInstance(parentRect);
            if (_tooltipInstance == null)
            {
                return;
            }

            _currentParent = parentRect;
            _currentContext = context;
            _currentCursorPosition = screenPosition;
            _currentCamera = uiCamera;

            _tooltipInstance.SetContent(pin);
            _tooltipInstance.SetVisible(true);
            _visible = true;

            if (context == TooltipContext.WorldMap)
            {
                UpdateWorldTooltipPosition(screenPosition, uiCamera);
            }
            else
            {
                PositionMiniMapTooltip(parentRect);
            }
        }

        /// <summary>
        /// Updates the world map cursor position so the tooltip can follow the pointer.
        /// </summary>
        public void UpdateCursorPosition(Vector2 screenPosition, Camera uiCamera)
        {
            if (!_visible || _currentContext != TooltipContext.WorldMap)
            {
                return;
            }

            _currentCursorPosition = screenPosition;
            _currentCamera = uiCamera;
            UpdateWorldTooltipPosition(screenPosition, uiCamera);
        }

        /// <summary>
        /// Hides the tooltip when no longer required.
        /// </summary>
        public void HideTooltip(TooltipContext context)
        {
            if (!_visible || _tooltipInstance == null)
            {
                return;
            }

            if (_currentContext != context)
            {
                return;
            }

            _visible = false;
            _tooltipInstance.SetVisible(false);
        }

        private void EnsureInstance(RectTransform parentRect)
        {
            if (_tooltipInstance != null)
            {
                if (_tooltipInstance.transform.parent != parentRect)
                {
                    _tooltipInstance.transform.SetParent(parentRect, false);
                }

                return;
            }

            if (tooltipPrefab == null)
            {
                return;
            }

            _tooltipInstance = Instantiate(tooltipPrefab, parentRect);
            _tooltipInstance.gameObject.SetActive(false);
        }

        private void UpdateWorldTooltipPosition(Vector2 screenPosition, Camera uiCamera)
        {
            if (!_visible || _tooltipInstance == null || _currentParent == null)
            {
                return;
            }

            RectTransform tooltipRect = _tooltipInstance.RectTransform;
            if (tooltipRect == null)
            {
                return;
            }

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_currentParent, screenPosition, uiCamera, out localPoint))
            {
                return;
            }

            localPoint += worldMapCursorOffset;

            _tooltipInstance.RecalculateLayout();
            Vector2 halfSize = tooltipRect.rect.size * 0.5f;
            Rect bounds = _currentParent.rect;

            float minX = bounds.xMin + worldMapPadding.x + halfSize.x;
            float maxX = bounds.xMax - worldMapPadding.x - halfSize.x;
            float minY = bounds.yMin + worldMapPadding.y + halfSize.y;
            float maxY = bounds.yMax - worldMapPadding.y - halfSize.y;

            if (minX > maxX)
            {
                float midX = (minX + maxX) * 0.5f;
                minX = maxX = midX;
            }

            if (minY > maxY)
            {
                float midY = (minY + maxY) * 0.5f;
                minY = maxY = midY;
            }

            Vector2 clamped = new(
                Mathf.Clamp(localPoint.x, minX, maxX),
                Mathf.Clamp(localPoint.y, minY, maxY));

            tooltipRect.anchorMin = tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
            tooltipRect.pivot = new Vector2(0.5f, 0.5f);
            tooltipRect.anchoredPosition = clamped;
        }

        private void PositionMiniMapTooltip(RectTransform parentRect)
        {
            if (_tooltipInstance == null)
            {
                return;
            }

            var tooltipRect = _tooltipInstance.RectTransform;
            _tooltipInstance.RecalculateLayout();

            Vector2 halfSize = tooltipRect.rect.size * 0.5f;
            Rect bounds = parentRect.rect;

            float minX = bounds.xMin + miniMapPadding.x + halfSize.x;
            float maxX = bounds.xMax - miniMapPadding.x - halfSize.x;
            float minY = bounds.yMin + miniMapPadding.y + halfSize.y;
            float maxY = bounds.yMax - miniMapPadding.y - halfSize.y;

            if (minX > maxX)
            {
                float midX = (minX + maxX) * 0.5f;
                minX = maxX = midX;
            }

            if (minY > maxY)
            {
                float midY = (minY + maxY) * 0.5f;
                minY = maxY = midY;
            }

            float targetX = Mathf.Clamp(bounds.xMin + miniMapPadding.x + halfSize.x + miniMapFixedOffset.x, minX, maxX);
            float targetY = Mathf.Clamp(bounds.yMax - miniMapPadding.y - halfSize.y + miniMapFixedOffset.y, minY, maxY);

            tooltipRect.anchorMin = tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
            tooltipRect.pivot = new Vector2(0.5f, 0.5f);
            tooltipRect.anchoredPosition = new Vector2(targetX, targetY);
        }
    }
}
