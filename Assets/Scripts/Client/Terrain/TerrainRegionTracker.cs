using UnityEngine;

namespace Client.Terrain
{
    [DisallowMultipleComponent]
    public class TerrainRegionTracker : MonoBehaviour
    {
        [SerializeField] private TerrainRegionManager regionManager;
        [SerializeField] private Transform target;

        private void Awake()
        {
            if (regionManager == null)
            {
                regionManager = FindFirstObjectByType<TerrainRegionManager>(FindObjectsInactive.Include);
            }
        }

        private void LateUpdate()
        {
            if (regionManager == null || target == null)
            {
                return;
            }

            regionManager.UpdateActiveRegion(target.position);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
