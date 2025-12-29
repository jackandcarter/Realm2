using System;
using System.Collections.Generic;
using UnityEngine;

namespace Client.Terrain
{
    [DisallowMultipleComponent]
    public class TerrainRegionManager : MonoBehaviour
    {
        [SerializeField] private bool autoFindRegions = true;
        [SerializeField] private List<TerrainRegion> regions = new();

        private TerrainRegion _activeRegion;

        public event Action<TerrainRegion> ActiveRegionChanged;

        public TerrainRegion ActiveRegion => _activeRegion;
        public IReadOnlyList<TerrainRegion> Regions => regions;

        private void Awake()
        {
            if (autoFindRegions)
            {
                RefreshRegions();
            }
        }

        public void RefreshRegions()
        {
            regions ??= new List<TerrainRegion>();
            regions.Clear();

            var found = FindObjectsByType<TerrainRegion>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (found == null)
            {
                return;
            }

            var seen = new HashSet<TerrainRegion>();
            foreach (var region in found)
            {
                if (region == null || !seen.Add(region))
                {
                    continue;
                }

                regions.Add(region);
            }
        }

        public bool TryGetRegionForWorldPosition(Vector3 worldPosition, out TerrainRegion region)
        {
            region = null;
            if (regions == null)
            {
                return false;
            }

            foreach (var entry in regions)
            {
                if (entry == null)
                {
                    continue;
                }

                if (entry.ContainsWorldPosition(worldPosition))
                {
                    region = entry;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetChunkCoordinates(Vector3 worldPosition, out TerrainRegion region, out Vector2Int chunkCoords, out Vector3 localPosition)
        {
            region = null;
            chunkCoords = Vector2Int.zero;
            localPosition = Vector3.zero;

            if (!TryGetRegionForWorldPosition(worldPosition, out var found))
            {
                return false;
            }

            region = found;
            return found.TryGetChunkCoordinates(worldPosition, out chunkCoords, out localPosition);
        }

        public void UpdateActiveRegion(Vector3 worldPosition)
        {
            TerrainRegion nextRegion = null;
            TryGetRegionForWorldPosition(worldPosition, out nextRegion);

            if (_activeRegion == nextRegion)
            {
                return;
            }

            _activeRegion = nextRegion;
            ActiveRegionChanged?.Invoke(_activeRegion);
        }

        private void OnValidate()
        {
            regions?.RemoveAll(region => region == null);
        }
    }
}
