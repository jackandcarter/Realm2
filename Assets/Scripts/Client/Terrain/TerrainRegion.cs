using System.Collections.Generic;
using Digger.Modules.Core.Sources;
using UnityEngine;

namespace Client.Terrain
{
    [DisallowMultipleComponent]
    public class TerrainRegion : MonoBehaviour
    {
        private const float DefaultChunkSize = 256f;

        [Header("Identity")]
        [SerializeField] private string regionId;
        [SerializeField] private string zoneId;

        [Header("Terrains")]
        [SerializeField] private bool useTerrainBounds = true;
        [SerializeField] private List<Terrain> terrains = new();
        [SerializeField] private Bounds manualWorldBounds = new(Vector3.zero, new Vector3(1024f, 512f, 1024f));

        [Header("Chunking")]
        [SerializeField] private Vector2 chunkOriginOffset = Vector2.zero;
        [SerializeField] private float chunkSizeOverride = DefaultChunkSize;
        [SerializeField] private DiggerSystem diggerSystem;

        [Header("Map")]
        [SerializeField] private Rect mapWorldBounds = new(-512f, -512f, 1024f, 1024f);
        [SerializeField] private Texture2D miniMapTexture;
        [SerializeField] private Texture2D worldMapTexture;

        public string RegionId => regionId;
        public string ZoneId => zoneId;
        public Rect MapWorldBounds => mapWorldBounds;
        public Texture2D MiniMapTexture => miniMapTexture;
        public Texture2D WorldMapTexture => worldMapTexture;
        public IReadOnlyList<Terrain> Terrains => terrains;

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

        public void RefreshTerrainsFromChildren()
        {
            terrains ??= new List<Terrain>();
            terrains.Clear();
            terrains.AddRange(GetComponentsInChildren<Terrain>(includeInactive: true));
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
