using Client;
using UnityEngine;

namespace Client.Terrain
{
    [DisallowMultipleComponent]
    public class TerrainSyncBootstrap : MonoBehaviour
    {
        [SerializeField] private TerrainSyncManager syncManager;

        private void Awake()
        {
            if (syncManager == null)
            {
                syncManager = GetComponent<TerrainSyncManager>();
            }
        }

        private void OnEnable()
        {
            SessionManager.SelectedRealmChanged += HandleRealmChanged;
        }

        private void OnDisable()
        {
            SessionManager.SelectedRealmChanged -= HandleRealmChanged;
        }

        private void Start()
        {
            if (!string.IsNullOrWhiteSpace(SessionManager.SelectedRealmId))
            {
                TryStartSync(SessionManager.SelectedRealmId);
            }
        }

        private void HandleRealmChanged(string realmId)
        {
            TryStartSync(realmId);
        }

        private void TryStartSync(string realmId)
        {
            if (syncManager == null || string.IsNullOrWhiteSpace(realmId))
            {
                return;
            }

            syncManager.StartSync(realmId);
        }
    }
}
