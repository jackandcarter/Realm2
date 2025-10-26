using Client.Player;
using UnityEngine;

namespace Client.UI
{
    public class ArkitectUiAvailabilityController : MonoBehaviour
    {
        [SerializeField] private GameObject arkitectRoot;

        private void Awake()
        {
            if (arkitectRoot == null)
            {
                arkitectRoot = gameObject;
            }
        }

        private void OnEnable()
        {
            PlayerClassStateManager.ArkitectAvailabilityChanged += OnArkitectAvailabilityChanged;
            ApplyAvailability(PlayerClassStateManager.IsArkitectAvailable);
        }

        private void OnDisable()
        {
            PlayerClassStateManager.ArkitectAvailabilityChanged -= OnArkitectAvailabilityChanged;
        }

        private void OnArkitectAvailabilityChanged(bool available)
        {
            ApplyAvailability(available);
        }

        private void ApplyAvailability(bool available)
        {
            if (arkitectRoot == null)
            {
                return;
            }

            if (arkitectRoot.activeSelf != available)
            {
                arkitectRoot.SetActive(available);
            }
        }
    }
}
