using System;
using System.Collections.Generic;
using UnityEngine;

namespace Client.Map
{
    /// <summary>
    /// Serialisable container describing a single point of interest on either the world map or a mini map.
    /// </summary>
    [Serializable]
    public sealed class MapPinData
    {
        [SerializeField] private string id = Guid.NewGuid().ToString("N");
        [SerializeField] private string displayName;
        [SerializeField] private string displaySubTitle;
        [SerializeField] private Vector2 normalizedWorldPosition = new(0.5f, 0.5f);
        [SerializeField] private string worldZoneId;
        [SerializeField] private List<string> zoneIds = new();
        [SerializeField] private Vector2 miniMapOffset = Vector2.zero;
        [SerializeField] private float miniMapVisibilityRadius = 25f;
        [SerializeField] private bool unlockedByDefault = true;
        [SerializeField, TextArea] private string tooltip;

        /// <summary>
        /// Globally unique identifier for the pin.
        /// </summary>
        public string Id => id;

        /// <summary>
        /// Primary name shown alongside the pin.
        /// </summary>
        public string DisplayName => displayName;

        /// <summary>
        /// Optional secondary text rendered under or beside the primary display name.
        /// </summary>
        public string DisplaySubTitle => displaySubTitle;

        /// <summary>
        /// Normalised position on the world map (0-1 range for both axes).
        /// </summary>
        public Vector2 NormalizedWorldPosition => new(Mathf.Clamp01(normalizedWorldPosition.x), Mathf.Clamp01(normalizedWorldPosition.y));

        /// <summary>
        /// Identifier describing which global world zone this pin belongs to.
        /// </summary>
        public string WorldZoneId => worldZoneId;

        /// <summary>
        /// Zone identifiers that should display this pin on mini maps.
        /// </summary>
        public IReadOnlyList<string> ZoneIds => zoneIds;

        /// <summary>
        /// Offset applied when rendering the pin on mini maps.
        /// </summary>
        public Vector2 MiniMapOffset => miniMapOffset;

        /// <summary>
        /// Visibility radius for the pin on mini maps.
        /// </summary>
        public float MiniMapVisibilityRadius => Mathf.Max(0f, miniMapVisibilityRadius);

        /// <summary>
        /// Whether the pin is unlocked when the player first loads the game.
        /// </summary>
        public bool UnlockedByDefault => unlockedByDefault;

        /// <summary>
        /// Tooltip text displayed when hovering over the pin.
        /// </summary>
        public string Tooltip => tooltip;

        /// <summary>
        /// Provides access to the modifiable list of zone identifiers.
        /// </summary>
        /// <remarks>
        /// The inspector will serialise any modifications made to the returned list.
        /// </remarks>
        public List<string> MutableZoneIds => zoneIds;

        /// <summary>
        /// Safely updates the world position for this pin.
        /// </summary>
        public void SetNormalizedWorldPosition(Vector2 position)
        {
            normalizedWorldPosition = new Vector2(Mathf.Clamp01(position.x), Mathf.Clamp01(position.y));
        }

        /// <summary>
        /// Adds a mini map zone identifier if it is not already present.
        /// </summary>
        public void AddZoneId(string zoneId)
        {
            if (string.IsNullOrWhiteSpace(zoneId))
            {
                return;
            }

            if (!zoneIds.Contains(zoneId))
            {
                zoneIds.Add(zoneId);
            }
        }

        /// <summary>
        /// Removes a mini map zone identifier if it exists.
        /// </summary>
        public void RemoveZoneId(string zoneId)
        {
            if (string.IsNullOrWhiteSpace(zoneId))
            {
                return;
            }

            zoneIds.Remove(zoneId);
        }
    }
}
