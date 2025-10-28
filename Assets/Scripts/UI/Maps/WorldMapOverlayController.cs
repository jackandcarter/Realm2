using System;
using System.Collections;
using Client.Map;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Client.UI.Maps
{
    [DisallowMultipleComponent]
    public class WorldMapOverlayController : MonoBehaviour
    {
        private const float DefaultAnimationDuration = 0.25f;
        private const float DefaultMinimumTransparency = 0.2f;
        private const float DefaultMaximumTransparency = 1f;

        [Header("Window")]
        [SerializeField] private RectTransform windowRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform dragHandle;
        [SerializeField] private RectTransform resizeHandle;
        [SerializeField] private Button closeButton;
        [SerializeField] private float animationDuration = DefaultAnimationDuration;
        [SerializeField] private Vector2 minimumWindowSize = new(640f, 360f);
        [SerializeField] private Vector2 maximumWindowSize = new(1920f, 1080f);

        [Header("Map Elements")]
        [SerializeField] private RawImage mapTexture;
        [SerializeField] private RectTransform mapViewport;
        [SerializeField] private Transform pinContainer;
        [SerializeField] private Slider transparencySlider;
        [SerializeField] private CanvasGroup mapCanvasGroup;

        [Header("Services")]
        [SerializeField] private MapPinService pinService;

        [Header("Details Panel")]
        [SerializeField] private GameObject detailsPanel;
        [SerializeField] private GameObject detailsContentRoot;
        [SerializeField] private Text detailsTitleLabel;
        [SerializeField] private GameObject detailsSubtitleContainer;
        [SerializeField] private Text detailsSubtitleLabel;
        [SerializeField] private GameObject detailsStatusContainer;
        [SerializeField] private Text detailsStatusLabel;
        [SerializeField] private GameObject detailsDescriptionContainer;
        [SerializeField] private Text detailsDescriptionLabel;
        [SerializeField] private GameObject detailsEmptyStateContainer;
        [SerializeField] private Text detailsEmptyStateLabel;
        [SerializeField] private string emptyDetailsTitle = "Select a pin";
        [SerializeField, TextArea] private string emptyDetailsBody = "Hover over a point of interest to view its lore and travel status.";
        [SerializeField] private string unlockedStatusLabel = "Status: Unlocked";
        [SerializeField] private string lockedStatusLabel = "Status: Locked";

        [Header("Persistence")]
        [SerializeField] private string preferenceKey = "world_map_overlay_state";

        private Coroutine _animationRoutine;
        private bool _isOpen;
        private bool _isDragging;
        private bool _isResizing;
        private Vector2 _dragOffset;
        private Vector2 _resizeStartSize;
        private Vector2 _resizeStartMouseLocalPoint;
        private RectTransform _parentRect;
        private OverlayState _state;
        private bool _serviceSubscribed;

        public bool IsOpen => _isOpen;

        [Serializable]
        private struct OverlayState
        {
            public bool open;
            public Vector2 position;
            public Vector2 size;
            public float transparency;
        }

        private void Awake()
        {
            if (windowRoot == null)
            {
                windowRoot = GetComponent<RectTransform>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            _parentRect = windowRoot != null ? windowRoot.parent as RectTransform : null;

            if (mapCanvasGroup == null && mapViewport != null)
            {
                mapCanvasGroup = mapViewport.GetComponent<CanvasGroup>();
                if (mapCanvasGroup == null)
                {
                    mapCanvasGroup = mapViewport.gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (pinService == null)
            {
                pinService = FindFirstObjectByType<MapPinService>(FindObjectsInactive.Include);
            }

            ConfigureHandle(dragHandle, typeof(WorldMapOverlayDragHandle));
            ConfigureHandle(resizeHandle, typeof(WorldMapOverlayResizeHandle));

            LoadState();
            ApplyState(_state, true);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = _isOpen ? 1f : 0f;
                canvasGroup.interactable = _isOpen;
                canvasGroup.blocksRaycasts = _isOpen;
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }

            if (transparencySlider != null)
            {
                transparencySlider.minValue = DefaultMinimumTransparency;
                transparencySlider.maxValue = DefaultMaximumTransparency;
                transparencySlider.onValueChanged.AddListener(OnTransparencySliderValueChanged);
            }

            UpdateDetailsPanel(pinService != null ? pinService.SelectedPin : null);
        }

        private void OnEnable()
        {
            if (transparencySlider != null)
            {
                transparencySlider.onValueChanged.AddListener(OnTransparencySliderValueChanged);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }

            SubscribeToPinService();
            UpdateDetailsPanel(pinService != null ? pinService.SelectedPin : null);
        }

        private void OnDisable()
        {
            if (transparencySlider != null)
            {
                transparencySlider.onValueChanged.RemoveListener(OnTransparencySliderValueChanged);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
            }

            UnsubscribeFromPinService();
        }

        private void OnDestroy()
        {
            if (transparencySlider != null)
            {
                transparencySlider.onValueChanged.RemoveListener(OnTransparencySliderValueChanged);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
            }

            UnsubscribeFromPinService();
        }

        public void Toggle()
        {
            if (_isOpen)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        public void Show()
        {
            if (_isOpen)
            {
                return;
            }

            _isOpen = true;
            if (canvasGroup != null)
            {
                canvasGroup.gameObject.SetActive(true);
            }

            StartAnimation(true);
            SaveState();
        }

        public void Hide()
        {
            if (!_isOpen)
            {
                return;
            }

            _isOpen = false;
            StartAnimation(false);
            SaveState();
        }

        public void HandleTeleportCompleted()
        {
            Show();
        }

        public void SetPinService(MapPinService targetService)
        {
            if (pinService == targetService)
            {
                return;
            }

            UnsubscribeFromPinService();
            pinService = targetService;
            SubscribeToPinService();
            UpdateDetailsPanel(pinService != null ? pinService.SelectedPin : null);
        }

        internal void BeginWindowDrag(PointerEventData eventData)
        {
            if (windowRoot == null || _parentRect == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, eventData.position, eventData.pressEventCamera, out var localPoint))
            {
                return;
            }

            _isDragging = true;
            _dragOffset = windowRoot.anchoredPosition - localPoint;
        }

        internal void DragWindow(PointerEventData eventData)
        {
            if (!_isDragging || windowRoot == null || _parentRect == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, eventData.position, eventData.pressEventCamera, out var localPoint))
            {
                return;
            }

            windowRoot.anchoredPosition = localPoint + _dragOffset;
        }

        internal void EndWindowDrag()
        {
            if (!_isDragging)
            {
                return;
            }

            _isDragging = false;
            SaveState();
        }

        internal void BeginWindowResize(PointerEventData eventData)
        {
            if (windowRoot == null)
            {
                return;
            }

            _isResizing = true;
            _resizeStartSize = windowRoot.rect.size;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(windowRoot, eventData.position, eventData.pressEventCamera, out _resizeStartMouseLocalPoint))
            {
                _resizeStartMouseLocalPoint = Vector2.zero;
            }
        }

        internal void ResizeWindow(PointerEventData eventData)
        {
            if (!_isResizing || windowRoot == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(windowRoot, eventData.position, eventData.pressEventCamera, out var localPoint))
            {
                return;
            }

            var delta = localPoint - _resizeStartMouseLocalPoint;
            var targetSize = _resizeStartSize + new Vector2(delta.x, -delta.y);
            targetSize.x = Mathf.Clamp(targetSize.x, minimumWindowSize.x, maximumWindowSize.x);
            targetSize.y = Mathf.Clamp(targetSize.y, minimumWindowSize.y, maximumWindowSize.y);

            SetWindowSize(targetSize);
        }

        internal void EndWindowResize()
        {
            if (!_isResizing)
            {
                return;
            }

            _isResizing = false;
            SaveState();
        }

        private void ConfigureHandle(RectTransform handle, Type handleType)
        {
            if (handle == null)
            {
                return;
            }

            if (handleType == typeof(WorldMapOverlayDragHandle))
            {
                var component = handle.GetComponent<WorldMapOverlayDragHandle>() ?? handle.gameObject.AddComponent<WorldMapOverlayDragHandle>();
                component.Initialize(this);
            }
            else if (handleType == typeof(WorldMapOverlayResizeHandle))
            {
                var component = handle.GetComponent<WorldMapOverlayResizeHandle>() ?? handle.gameObject.AddComponent<WorldMapOverlayResizeHandle>();
                component.Initialize(this);
            }
        }

        private void StartAnimation(bool opening)
        {
            if (canvasGroup == null || windowRoot == null)
            {
                canvasGroup = canvasGroup ?? GetComponent<CanvasGroup>();
                windowRoot = windowRoot ?? GetComponent<RectTransform>();
            }

            if (canvasGroup == null || windowRoot == null)
            {
                return;
            }

            if (_animationRoutine != null)
            {
                StopCoroutine(_animationRoutine);
            }

            _animationRoutine = StartCoroutine(AnimateWindow(opening));
        }

        private IEnumerator AnimateWindow(bool opening)
        {
            var duration = Mathf.Max(0.01f, animationDuration);
            var initialAlpha = canvasGroup.alpha;
            var targetAlpha = opening ? 1f : 0f;
            var initialScale = windowRoot.localScale;
            var targetScale = opening ? Vector3.one : Vector3.one * 0.96f;

            if (opening)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
                if (!gameObject.activeSelf)
                {
                    gameObject.SetActive(true);
                }
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = Mathf.SmoothStep(0f, 1f, t);
                canvasGroup.alpha = Mathf.Lerp(initialAlpha, targetAlpha, eased);
                windowRoot.localScale = Vector3.Lerp(initialScale, targetScale, eased);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            windowRoot.localScale = targetScale;

            if (opening)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            else
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            _animationRoutine = null;
        }

        private void OnTransparencySliderValueChanged(float value)
        {
            ApplyTransparency(value);
            SaveState();
        }

        private void ApplyTransparency(float value)
        {
            if (mapCanvasGroup != null)
            {
                mapCanvasGroup.alpha = Mathf.Clamp(value, DefaultMinimumTransparency, DefaultMaximumTransparency);
            }
            else if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Clamp(value, DefaultMinimumTransparency, DefaultMaximumTransparency);
            }
        }

        private void SetWindowSize(Vector2 size)
        {
            if (windowRoot == null)
            {
                return;
            }

            windowRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            windowRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        }

        private void ApplyState(OverlayState state, bool fromPersistence)
        {
            _isOpen = state.open;

            if (windowRoot != null)
            {
                windowRoot.anchoredPosition = state.position;
                SetWindowSize(new Vector2(
                    Mathf.Clamp(state.size.x, minimumWindowSize.x, maximumWindowSize.x),
                    Mathf.Clamp(state.size.y, minimumWindowSize.y, maximumWindowSize.y)));
            }

            if (transparencySlider != null)
            {
                transparencySlider.SetValueWithoutNotify(Mathf.Clamp(state.transparency, DefaultMinimumTransparency, DefaultMaximumTransparency));
            }

            ApplyTransparency(Mathf.Clamp(state.transparency, DefaultMinimumTransparency, DefaultMaximumTransparency));

            if (!fromPersistence && _isOpen)
            {
                Show();
            }
        }

        private void LoadState()
        {
            _state = new OverlayState
            {
                open = false,
                position = windowRoot != null ? windowRoot.anchoredPosition : Vector2.zero,
                size = windowRoot != null ? windowRoot.rect.size : minimumWindowSize,
                transparency = DefaultMaximumTransparency
            };

            if (!PlayerPrefs.HasKey(preferenceKey))
            {
                return;
            }

            try
            {
                var json = PlayerPrefs.GetString(preferenceKey);
                if (!string.IsNullOrEmpty(json))
                {
                    var loaded = JsonUtility.FromJson<OverlayState>(json);
                    if (loaded.size.sqrMagnitude > 0f)
                    {
                        _state = loaded;
                    }
                }
            }
            catch (Exception)
            {
                // Swallow errors: invalid payloads should not break the UI.
            }
        }

        private void SaveState()
        {
            if (windowRoot != null)
            {
                _state.position = windowRoot.anchoredPosition;
                _state.size = windowRoot.rect.size;
            }

            _state.open = _isOpen;
            _state.transparency = transparencySlider != null
                ? transparencySlider.value
                : mapCanvasGroup != null ? mapCanvasGroup.alpha : DefaultMaximumTransparency;

            try
            {
                var json = JsonUtility.ToJson(_state);
                PlayerPrefs.SetString(preferenceKey, json);
                PlayerPrefs.Save();
            }
            catch (Exception)
            {
                // Ignore persistence failures in runtime builds.
            }
        }

        [ContextMenu("Reset Saved State")]
        public void ResetSavedState()
        {
            PlayerPrefs.DeleteKey(preferenceKey);
            LoadState();
            ApplyState(_state, false);
        }

        private void SubscribeToPinService()
        {
            if (_serviceSubscribed || pinService == null)
            {
                return;
            }

            pinService.SelectedPinChanged += HandleSelectedPinChanged;
            _serviceSubscribed = true;
        }

        private void UnsubscribeFromPinService()
        {
            if (!_serviceSubscribed || pinService == null)
            {
                return;
            }

            pinService.SelectedPinChanged -= HandleSelectedPinChanged;
            _serviceSubscribed = false;
        }

        private void HandleSelectedPinChanged(MapPinData pin)
        {
            UpdateDetailsPanel(pin);
        }

        private void UpdateDetailsPanel(MapPinData pin)
        {
            if (detailsPanel == null)
            {
                return;
            }

            bool hasPin = pin != null;
            if (detailsContentRoot != null)
            {
                detailsContentRoot.SetActive(hasPin);
            }

            if (detailsEmptyStateContainer != null)
            {
                detailsEmptyStateContainer.SetActive(!hasPin);
            }

            if (!hasPin)
            {
                if (detailsTitleLabel != null)
                {
                    detailsTitleLabel.text = string.IsNullOrWhiteSpace(emptyDetailsTitle)
                        ? string.Empty
                        : emptyDetailsTitle.Trim();
                }

                if (detailsEmptyStateLabel != null)
                {
                    detailsEmptyStateLabel.text = string.IsNullOrWhiteSpace(emptyDetailsBody)
                        ? string.Empty
                        : emptyDetailsBody.Trim();
                }

                if (detailsSubtitleContainer != null)
                {
                    detailsSubtitleContainer.SetActive(false);
                }

                if (detailsStatusContainer != null)
                {
                    detailsStatusContainer.SetActive(false);
                }

                if (detailsDescriptionContainer != null)
                {
                    detailsDescriptionContainer.SetActive(false);
                }

                return;
            }

            if (detailsTitleLabel != null)
            {
                var title = string.IsNullOrWhiteSpace(pin.DisplayName) ? emptyDetailsTitle : pin.DisplayName.Trim();
                detailsTitleLabel.text = title;
            }

            bool hasSubtitle = !string.IsNullOrWhiteSpace(pin.DisplaySubTitle);
            if (detailsSubtitleLabel != null)
            {
                detailsSubtitleLabel.text = hasSubtitle ? pin.DisplaySubTitle.Trim() : string.Empty;
            }

            if (detailsSubtitleContainer != null)
            {
                detailsSubtitleContainer.SetActive(hasSubtitle);
            }

            bool unlocked = pinService != null && pinService.IsPinUnlocked(pin.Id);
            if (detailsStatusLabel != null)
            {
                detailsStatusLabel.text = unlocked ? unlockedStatusLabel : lockedStatusLabel;
            }

            if (detailsStatusContainer != null)
            {
                detailsStatusContainer.SetActive(detailsStatusLabel != null);
            }

            bool hasDescription = !string.IsNullOrWhiteSpace(pin.Tooltip);
            if (detailsDescriptionLabel != null)
            {
                detailsDescriptionLabel.text = hasDescription ? pin.Tooltip.Trim() : string.Empty;
            }

            if (detailsDescriptionContainer != null)
            {
                detailsDescriptionContainer.SetActive(hasDescription);
            }

            if (detailsEmptyStateLabel != null)
            {
                detailsEmptyStateLabel.text = string.Empty;
            }
        }
    }
}
