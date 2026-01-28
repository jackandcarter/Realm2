using Client.Terrain;
using UnityEngine;

namespace Client.World
{
    [DisallowMultipleComponent]
    public class WorldSceneBootstrapper : MonoBehaviour
    {
        [Header("Realm Binding")]
        [SerializeField] private string boundRealmId = "realm-elysium-nexus";
        [SerializeField] private string worldSceneName = "SampleScene";
        [SerializeField] private string worldServiceUrl;

        [Header("Managers")]
        [SerializeField] private bool autoCreateManagers = true;
        [SerializeField] private Transform managerRoot;
        [SerializeField] private TerrainSyncManager terrainSyncManager;
        [SerializeField] private TerrainSyncBootstrap terrainSyncBootstrap;
        [SerializeField] private RuntimePlotManager runtimePlotManager;

        private void Awake()
        {
            EnsureRealmContext();
            EnsureManagers();
        }

        private void EnsureRealmContext()
        {
            if (string.IsNullOrWhiteSpace(boundRealmId))
            {
                return;
            }

            var normalizedRealmId = boundRealmId.Trim();
            if (string.IsNullOrWhiteSpace(SessionManager.SelectedRealmId))
            {
                SessionManager.SetRealmContext(normalizedRealmId, worldSceneName, worldServiceUrl);
                return;
            }

            if (!string.Equals(SessionManager.SelectedRealmId, normalizedRealmId, System.StringComparison.Ordinal))
            {
                Debug.LogWarning($"World scene '{gameObject.scene.name}' is bound to realm '{normalizedRealmId}', but session is set to '{SessionManager.SelectedRealmId}'.", this);
                return;
            }

            SessionManager.SetRealmContext(normalizedRealmId, worldSceneName, worldServiceUrl);
        }

        private void EnsureManagers()
        {
            if (!autoCreateManagers)
            {
                return;
            }

            var root = managerRoot != null ? managerRoot : transform;

            terrainSyncManager = EnsureComponent(terrainSyncManager, root, "TerrainSyncManager");
            terrainSyncBootstrap = EnsureComponent(terrainSyncBootstrap, root, "TerrainSyncBootstrap");
            runtimePlotManager = EnsureComponent(runtimePlotManager, root, "RuntimePlotManager");
        }

        private static T EnsureComponent<T>(T existing, Transform parent, string name) where T : Component
        {
            if (existing != null)
            {
                return existing;
            }

            var found = FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (found != null)
            {
                return found;
            }

            if (parent == null)
            {
                return null;
            }

            var host = new GameObject(name);
            host.transform.SetParent(parent, false);
            return host.AddComponent<T>();
        }
    }
}
