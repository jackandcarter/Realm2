using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Client.UI.Maps
{
    [DisallowMultipleComponent]
    public class MiniMapController : MonoBehaviour
    {
        private const float DefaultVisibilityRadius = 150f;

        [Serializable]
        public class TrackedEntity
        {
            [Tooltip("World transform that drives the icon position.")]
            public Transform target;

            [Tooltip("UI element used to represent the entity on the mini map.")]
            public RectTransform icon;

            [Tooltip("If disabled, the icon remains visible even when outside the visibility radius.")]
            public bool hideWhenOutOfRange = true;

            [Tooltip("Visibility radius multiplier applied to this entry.")]
            public float visibilityRadiusMultiplier = 1f;
        }

        [Header("Map Display")]
        [SerializeField] private RawImage mapTexture;
        [SerializeField] private RectTransform mapContent;
        [SerializeField] private RectTransform pinContainer;
        [SerializeField] private RectTransform playerMarker;
        [SerializeField] private RectTransform headingIndicator;
        [SerializeField] private bool rotateMapWithPlayer = true;
        [SerializeField] private float headingOffsetDegrees = 0f;

        [Header("World Space")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Rect worldBounds = new(-512f, -512f, 1024f, 1024f);

        [Header("Entities")]
        [SerializeField] private float entityVisibilityRadius = DefaultVisibilityRadius;
        [SerializeField] private List<TrackedEntity> trackedEntities = new();

        [Header("World Map Overlay")]
        [SerializeField] private Button expandButton;
        [SerializeField] private WorldMapOverlayController worldMapOverlay;
        [SerializeField] private UnityEvent onWorldMapOpened;
        [SerializeField] private UnityEvent onWorldMapClosed;

        private readonly Dictionary<Transform, RectTransform> _runtimePins = new();
        private Texture _currentTexture;

        public Texture CurrentTexture => _currentTexture;
        public Rect WorldBounds => worldBounds;

        private void Awake()
        {
            if (mapContent == null && mapTexture != null)
            {
                mapContent = mapTexture.rectTransform;
            }

            if (pinContainer == null && mapContent != null)
            {
                pinContainer = mapContent;
            }

            if (worldMapOverlay == null)
            {
                worldMapOverlay = FindFirstObjectByType<WorldMapOverlayController>(FindObjectsInactive.Include);
            }

            EnsureIconsParented(pinContainer);
        }

        private void OnEnable()
        {
            if (expandButton != null)
            {
                expandButton.onClick.AddListener(OnExpandButtonClicked);
            }
        }

        private void OnDisable()
        {
            if (expandButton != null)
            {
                expandButton.onClick.RemoveListener(OnExpandButtonClicked);
            }
        }

        private void LateUpdate()
        {
            if (playerTransform == null || mapContent == null)
            {
                return;
            }

            var rect = mapContent.rect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            Vector2 mapSize = rect.size;
            Vector2 playerMapPosition = WorldToMapPosition(playerTransform.position, mapSize);

            mapContent.anchoredPosition = -playerMapPosition;

            if (playerMarker != null)
            {
                playerMarker.anchoredPosition = Vector2.zero;
            }

            UpdateHeading();
            UpdateEntities(playerMapPosition, mapSize);
        }

        public void SetPlayerTransform(Transform target)
        {
            playerTransform = target;
        }

        public void SetZoneTexture(Texture texture)
        {
            _currentTexture = texture;
            if (mapTexture != null)
            {
                mapTexture.texture = texture;
                mapTexture.enabled = texture != null;
            }
        }

        public void SetWorldBounds(Rect bounds)
        {
            worldBounds = bounds;
        }

        public void RegisterRuntimePin(Transform target, RectTransform icon)
        {
            if (target == null || icon == null)
            {
                return;
            }

            EnsureIconParent(icon);
            _runtimePins[target] = icon;
        }

        public void UnregisterRuntimePin(Transform target)
        {
            if (target == null)
            {
                return;
            }

            if (_runtimePins.TryGetValue(target, out var icon) && icon != null)
            {
                icon.gameObject.SetActive(false);
            }

            _runtimePins.Remove(target);
        }

        public void HandleTeleportCompleted()
        {
            if (worldMapOverlay == null)
            {
                return;
            }

            worldMapOverlay.Show();
            onWorldMapOpened?.Invoke();
        }

        public void CloseWorldMap()
        {
            if (worldMapOverlay == null || !worldMapOverlay.IsOpen)
            {
                return;
            }

            worldMapOverlay.Hide();
            onWorldMapClosed?.Invoke();
        }

        public void SetEntityVisibilityRadius(float radius)
        {
            entityVisibilityRadius = Mathf.Max(0f, radius);
        }

        private void OnExpandButtonClicked()
        {
            if (worldMapOverlay == null)
            {
                worldMapOverlay = FindFirstObjectByType<WorldMapOverlayController>(FindObjectsInactive.Include);
            }

            if (worldMapOverlay == null)
            {
                return;
            }

            bool wasOpen = worldMapOverlay.IsOpen;
            worldMapOverlay.Show();

            if (!wasOpen)
            {
                onWorldMapOpened?.Invoke();
            }
        }

        private void UpdateHeading()
        {
            if (mapContent == null || playerTransform == null)
            {
                return;
            }

            float heading = 0f;
            Vector3 forward = playerTransform.forward;
            if (forward.sqrMagnitude > Mathf.Epsilon)
            {
                heading = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            }

            heading += headingOffsetDegrees;

            if (rotateMapWithPlayer)
            {
                mapContent.localRotation = Quaternion.Euler(0f, 0f, heading);

                if (headingIndicator != null)
                {
                    headingIndicator.localRotation = Quaternion.identity;
                    headingIndicator.anchoredPosition = Vector2.zero;
                }
            }
            else
            {
                mapContent.localRotation = Quaternion.identity;

                if (headingIndicator != null)
                {
                    headingIndicator.localRotation = Quaternion.Euler(0f, 0f, -heading);
                    headingIndicator.anchoredPosition = Vector2.zero;
                }
            }
        }

        private void UpdateEntities(Vector2 playerMapPosition, Vector2 mapSize)
        {
            if (pinContainer == null)
            {
                return;
            }

            float baseRadius = Mathf.Max(0f, entityVisibilityRadius);
            UpdateTrackedCollection(trackedEntities, playerMapPosition, mapSize, baseRadius);

            if (_runtimePins.Count == 0)
            {
                return;
            }

            using var enumerator = _runtimePins.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var pair = enumerator.Current;
                UpdateIcon(pair.Value, pair.Key, 1f, true, playerMapPosition, mapSize, baseRadius);
            }
        }

        private void UpdateTrackedCollection(IEnumerable<TrackedEntity> collection, Vector2 playerMapPosition, Vector2 mapSize, float baseRadius)
        {
            if (collection == null)
            {
                return;
            }

            foreach (var tracked in collection)
            {
                if (tracked == null)
                {
                    continue;
                }

                UpdateIcon(tracked.icon, tracked.target, tracked.visibilityRadiusMultiplier, tracked.hideWhenOutOfRange, playerMapPosition, mapSize, baseRadius);
            }
        }

        private void UpdateIcon(RectTransform icon, Transform target, float radiusMultiplier, bool hideWhenOutOfRange, Vector2 playerMapPosition, Vector2 mapSize, float baseRadius)
        {
            if (icon == null || target == null)
            {
                return;
            }

            float maxRadius = baseRadius * Mathf.Max(0f, radiusMultiplier <= 0f ? 1f : radiusMultiplier);
            float distance = Vector2.Distance(ProjectToMapPlane(playerTransform.position), ProjectToMapPlane(target.position));
            bool visible = !hideWhenOutOfRange || distance <= maxRadius;

            icon.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            EnsureIconParent(icon);
            Vector2 iconPosition = WorldToMapPosition(target.position, mapSize) - playerMapPosition;
            icon.anchoredPosition = iconPosition;
            icon.localRotation = Quaternion.identity;
        }

        private void EnsureIconsParented(Transform expectedParent)
        {
            if (expectedParent == null)
            {
                return;
            }

            if (trackedEntities == null)
            {
                return;
            }

            foreach (var tracked in trackedEntities)
            {
                if (tracked?.icon == null)
                {
                    continue;
                }

                EnsureIconParent(tracked.icon);
            }
        }

        private void EnsureIconParent(RectTransform icon)
        {
            if (icon == null || pinContainer == null)
            {
                return;
            }

            if (icon.transform.parent != pinContainer)
            {
                icon.SetParent(pinContainer, false);
            }
        }

        private Vector2 WorldToMapPosition(Vector3 worldPosition, Vector2 mapSize)
        {
            if (mapSize.x <= 0f || mapSize.y <= 0f)
            {
                return Vector2.zero;
            }

            float normalizedX = Mathf.Approximately(worldBounds.width, 0f) ? 0.5f : Mathf.InverseLerp(worldBounds.xMin, worldBounds.xMax, worldPosition.x);
            float normalizedY = Mathf.Approximately(worldBounds.height, 0f) ? 0.5f : Mathf.InverseLerp(worldBounds.yMin, worldBounds.yMax, worldPosition.z);

            float centeredX = (normalizedX - 0.5f) * mapSize.x;
            float centeredY = (normalizedY - 0.5f) * mapSize.y;
            return new Vector2(centeredX, centeredY);
        }

        private static Vector2 ProjectToMapPlane(Vector3 position)
        {
            return new Vector2(position.x, position.z);
        }
    }
}
