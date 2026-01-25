using Client.Inventory;
using UnityEngine;

namespace Client.Quests
{
    public class QuestItemPickup : MonoBehaviour
    {
        [SerializeField]
        private string itemId;

        [SerializeField]
        private int quantity = 1;

        [SerializeField]
        private bool destroyOnPickup = true;

        [SerializeField]
        private bool logEvents = true;

        public void Collect()
        {
            var characterId = SessionManager.SelectedCharacterId;
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(itemId))
            {
                return;
            }

            StartCoroutine(InventoryRepository.AddItemAsync(
                characterId,
                itemId,
                Mathf.Max(1, quantity),
                success =>
                {
                    if (logEvents && success)
                    {
                        Debug.Log($"Granted {quantity}x {itemId} to {characterId}.", this);
                    }

                    if (destroyOnPickup && success)
                    {
                        Destroy(gameObject);
                    }
                },
                error =>
                {
                    if (logEvents)
                    {
                        Debug.LogWarning($"Failed to grant {itemId}: {error.Message}", this);
                    }
                }));
        }
    }
}
