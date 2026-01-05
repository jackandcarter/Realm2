using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Client.Terrain
{
    public enum TerrainRegionSelectionMode
    {
        All,
        RegionId,
        ZoneId,
        Explicit
    }

    [Serializable]
    public struct TerrainRegionSelection
    {
        public TerrainRegionSelectionMode Mode;
        public string RegionId;
        public string ZoneId;
        public List<TerrainRegion> ExplicitRegions;
    }

    public static class TerrainRegionSelectionUtility
    {
        public static List<TerrainRegion> FindRegionsInScene()
        {
            var regions = FindObjectsByType<TerrainRegion>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            return regions == null ? new List<TerrainRegion>() : regions.Where(region => region != null).Distinct().ToList();
        }

        public static IReadOnlyList<TerrainRegion> FilterRegions(IEnumerable<TerrainRegion> regions, TerrainRegionSelection selection)
        {
            if (regions == null)
            {
                return Array.Empty<TerrainRegion>();
            }

            IEnumerable<TerrainRegion> filtered = regions.Where(region => region != null);
            switch (selection.Mode)
            {
                case TerrainRegionSelectionMode.RegionId:
                    if (!string.IsNullOrWhiteSpace(selection.RegionId))
                    {
                        filtered = filtered.Where(region => string.Equals(region.RegionId, selection.RegionId, StringComparison.OrdinalIgnoreCase));
                    }
                    else
                    {
                        filtered = Enumerable.Empty<TerrainRegion>();
                    }
                    break;
                case TerrainRegionSelectionMode.ZoneId:
                    if (!string.IsNullOrWhiteSpace(selection.ZoneId))
                    {
                        filtered = filtered.Where(region => string.Equals(region.ZoneId, selection.ZoneId, StringComparison.OrdinalIgnoreCase));
                    }
                    else
                    {
                        filtered = Enumerable.Empty<TerrainRegion>();
                    }
                    break;
                case TerrainRegionSelectionMode.Explicit:
                    if (selection.ExplicitRegions == null || selection.ExplicitRegions.Count == 0)
                    {
                        filtered = Enumerable.Empty<TerrainRegion>();
                    }
                    else
                    {
                        var set = new HashSet<TerrainRegion>(selection.ExplicitRegions.Where(region => region != null));
                        filtered = filtered.Where(set.Contains);
                    }
                    break;
                case TerrainRegionSelectionMode.All:
                default:
                    break;
            }

            return filtered.Distinct().ToList();
        }
    }
}
