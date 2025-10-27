using Client.UI.Maps;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Client.UI
{
    [DisallowMultipleComponent]
    public class MiniMapController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button worldMapButton;
        [SerializeField] private WorldMapOverlayController worldMapOverlay;

        [Header("Events")]
        [SerializeField] private UnityEvent onWorldMapOpened;
        [SerializeField] private UnityEvent onWorldMapClosed;

        private void Awake()
        {
            if (worldMapOverlay == null)
            {
                worldMapOverlay = FindObjectOfType<WorldMapOverlayController>(true);
            }
        }

        private void OnEnable()
        {
            if (worldMapButton != null)
            {
                worldMapButton.onClick.AddListener(OnWorldMapButtonClicked);
            }
        }

        private void OnDisable()
        {
            if (worldMapButton != null)
            {
                worldMapButton.onClick.RemoveListener(OnWorldMapButtonClicked);
            }
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
            if (worldMapOverlay == null)
            {
                return;
            }

            worldMapOverlay.Hide();
            onWorldMapClosed?.Invoke();
        }

        private void OnWorldMapButtonClicked()
        {
            if (worldMapOverlay == null)
            {
                return;
            }

            var wasOpen = worldMapOverlay.IsOpen;
            worldMapOverlay.Toggle();

            if (wasOpen)
            {
                onWorldMapClosed?.Invoke();
            }
            else
            {
                onWorldMapOpened?.Invoke();
            }
        }

        public bool CanInitiateTeleportFromOverlay()
        {
            return false;
        }
    }
}
