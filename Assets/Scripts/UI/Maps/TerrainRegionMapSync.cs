using Client.Terrain;
using UnityEngine;

namespace Client.UI.Maps
{
    [DisallowMultipleComponent]
    public class TerrainRegionMapSync : MonoBehaviour
    {
        [SerializeField] private TerrainRegionManager regionManager;
        [SerializeField] private MiniMapController miniMapController;
        [SerializeField] private WorldMapOverlayController worldMapOverlay;
        [SerializeField] private bool fallbackToMiniMapTexture = true;

        private void Awake()
        {
            if (regionManager == null)
            {
                regionManager = FindFirstObjectByType<TerrainRegionManager>(FindObjectsInactive.Include);
            }

            if (miniMapController == null)
            {
                miniMapController = FindFirstObjectByType<MiniMapController>(FindObjectsInactive.Include);
            }

            if (worldMapOverlay == null)
            {
                worldMapOverlay = FindFirstObjectByType<WorldMapOverlayController>(FindObjectsInactive.Include);
            }
        }

        private void OnEnable()
        {
            if (regionManager != null)
            {
                regionManager.ActiveRegionChanged += HandleRegionChanged;
                HandleRegionChanged(regionManager.ActiveRegion);
            }
        }

        private void OnDisable()
        {
            if (regionManager != null)
            {
                regionManager.ActiveRegionChanged -= HandleRegionChanged;
            }
        }

        private void HandleRegionChanged(TerrainRegion region)
        {
            if (region == null)
            {
                return;
            }

            if (miniMapController != null)
            {
                miniMapController.SetZoneTexture(region.MiniMapTexture);
                miniMapController.SetWorldBounds(region.MapWorldBounds);
            }

            if (worldMapOverlay != null)
            {
                var texture = region.WorldMapTexture != null ? region.WorldMapTexture : (fallbackToMiniMapTexture ? region.MiniMapTexture : null);
                worldMapOverlay.SetMapTexture(texture);
                worldMapOverlay.SetWorldBounds(region.MapWorldBounds);
            }
        }
    }
}
