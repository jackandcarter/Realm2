using System.Collections.Generic;
using Digger.Modules.Core.Sources;
using UnityEngine;
using UnityTerrain = UnityEngine.Terrain;

namespace Client.Terrain
{
    [DisallowMultipleComponent]
    public class TerrainRegion : MonoBehaviour
    {
        private const float DefaultChunkSize = 256f;

        [Header("Identity")]
        [Tooltip("Unique identifier for this terrain region (e.g., shard or map chunk key).")]
        [SerializeField] private string regionId;
        [Tooltip("Optional display name used when publishing region metadata to backend services.")]
        [SerializeField] private string regionName;
        [Tooltip("Optional zone identifier used to group regions for streaming or gameplay rules.")]
        [SerializeField] private string zoneId;

        [Header("Terrains")]
        [Tooltip("When enabled, compute world bounds from the referenced Unity Terrains.")]
        [SerializeField] private bool useTerrainBounds = true;
        [Tooltip("Unity Terrain objects that define the bounds and surface of this region.")]
        [SerializeField] private List<UnityTerrain> terrains = new();
        [Tooltip("Fallback bounds used when terrain bounds are disabled or no terrains are assigned.")]
        [SerializeField] private Bounds manualWorldBounds = new(Vector3.zero, new Vector3(1024f, 512f, 1024f));

        [Header("Chunking")]
        [Tooltip("Offset applied before chunk coordinates are computed in local space.")]
        [SerializeField] private Vector2 chunkOriginOffset = Vector2.zero;
        [Tooltip("Override for chunk size; set to a value > 0 to ignore the digger system size.")]
        [SerializeField] private float chunkSizeOverride = 0f;
        [Tooltip("Optional Digger system used to determine default chunk size.")]
        [SerializeField] private DiggerSystem diggerSystem;

        [Header("Map")]
        [Tooltip("World-space rectangle used to place the region on 2D maps.")]
        [SerializeField] private Rect mapWorldBounds = new(-512f, -512f, 1024f, 1024f);
        [Tooltip("Texture used for the minimap representation of this region.")]
        [SerializeField] private Texture2D miniMapTexture;
        [Tooltip("Texture used for the world map representation of this region.")]
        [SerializeField] private Texture2D worldMapTexture;

        public string RegionId => regionId;
        public string RegionName => regionName;
        public string ZoneId => zoneId;
        public Rect MapWorldBounds => mapWorldBounds;
        public Texture2D MiniMapTexture => miniMapTexture;
        public Texture2D WorldMapTexture => worldMapTexture;
        public IReadOnlyList<UnityTerrain> Terrains => terrains;
        public Vector2 ChunkOriginOffset => chunkOriginOffset;
        public float ChunkSizeOverride => chunkSizeOverride;
        public bool UseTerrainBounds => useTerrainBounds;

        public string GetDisplayName()
        {
            return string.IsNullOrWhiteSpace(regionName) ? regionId : regionName;
        }

        public Bounds GetWorldBounds()
        {
            if (!useTerrainBounds || terrains == null || terrains.Count == 0)
            {
                return manualWorldBounds;
            }

            var bounds = new Bounds();
            var hasBounds = false;

            foreach (var terrain in terrains)
            {
                if (terrain == null || terrain.terrainData == null)
                {
                    continue;
                }

                var size = terrain.terrainData.size;
                var center = terrain.transform.position + size * 0.5f;
                var terrainBounds = new Bounds(center, size);

                if (!hasBounds)
                {
                    bounds = terrainBounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(terrainBounds);
                }
            }

            return hasBounds ? bounds : manualWorldBounds;
        }

        public bool ContainsWorldPosition(Vector3 worldPosition)
        {
            var bounds = GetWorldBounds();
            var flattened = new Vector3(worldPosition.x, bounds.center.y, worldPosition.z);
            return bounds.Contains(flattened);
        }

        public Vector3 WorldToLocal(Vector3 worldPosition)
        {
            return transform.InverseTransformPoint(worldPosition);
        }

        public float GetChunkSize()
        {
            if (chunkSizeOverride > 0f)
            {
                return chunkSizeOverride;
            }

            if (diggerSystem != null)
            {
                return diggerSystem.SizeOfMesh;
            }

            return DefaultChunkSize;
        }

        public bool TryGetChunkCoordinates(Vector3 worldPosition, out Vector2Int chunkCoords, out Vector3 localPosition)
        {
            localPosition = WorldToLocal(worldPosition);
            var chunkSize = GetChunkSize();
            if (chunkSize <= 0f)
            {
                chunkCoords = Vector2Int.zero;
                return false;
            }

            var localX = localPosition.x - chunkOriginOffset.x;
            var localZ = localPosition.z - chunkOriginOffset.y;

            chunkCoords = new Vector2Int(Mathf.FloorToInt(localX / chunkSize),
                                         Mathf.FloorToInt(localZ / chunkSize));
            return true;
        }

        public Vector3 GetChunkWorldCenter(Vector2Int chunkCoords, float worldY)
        {
            var chunkSize = GetChunkSize();
            var localX = (chunkCoords.x + 0.5f) * chunkSize + chunkOriginOffset.x;
            var localZ = (chunkCoords.y + 0.5f) * chunkSize + chunkOriginOffset.y;
            var localPosition = new Vector3(localX, 0f, localZ);
            var worldPosition = transform.TransformPoint(localPosition);
            worldPosition.y = worldY;
            return worldPosition;
        }

        public void RefreshTerrainsFromChildren()
        {
            terrains ??= new List<UnityTerrain>();
            terrains.Clear();
            terrains.AddRange(GetComponentsInChildren<UnityTerrain>(includeInactive: true));
        }

        private void OnValidate()
        {
            if (useTerrainBounds && (terrains == null || terrains.Count == 0))
            {
                RefreshTerrainsFromChildren();
            }
        }
    }
}
