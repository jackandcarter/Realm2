using System;
using System.Collections;
using System.Collections.Generic;
using Client.Progression;
using UnityEngine;

namespace Client.Inventory
{
    public static class InventoryRepository
    {
        private static readonly Dictionary<string, CharacterInventoryItemEntry[]> ItemsByCharacter =
            new(StringComparer.OrdinalIgnoreCase);
        private static CharacterProgressionClient _progressionClient;

        public static event Action<string, CharacterInventoryItemEntry[]> InventoryChanged;

        public static void SetProgressionClient(CharacterProgressionClient client)
        {
            _progressionClient = client;
        }

        public static CharacterInventoryItemEntry[] GetItems(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || !ItemsByCharacter.TryGetValue(characterId, out var items))
            {
                return Array.Empty<CharacterInventoryItemEntry>();
            }

            return CloneItems(items);
        }

        public static IEnumerator SyncWithServer(
            string characterId,
            Action<CharacterProgressionEnvelope> onSuccess = null,
            Action<ApiError> onError = null)
        {
            if (string.IsNullOrWhiteSpace(characterId) || _progressionClient == null)
            {
                onSuccess?.Invoke(null);
                yield break;
            }

            CharacterProgressionEnvelope snapshot = null;
            ApiError error = null;

            yield return _progressionClient.GetProgression(
                characterId,
                payload => snapshot = payload,
                apiError => error = apiError);

            if (error != null)
            {
                onError?.Invoke(error);
                yield break;
            }

            if (snapshot != null)
            {
                ApplySnapshot(characterId, snapshot);
            }

            onSuccess?.Invoke(snapshot);
        }

        public static IEnumerator ReplaceInventoryAsync(
            string characterId,
            CharacterInventoryItemEntry[] items,
            Action<bool> onSuccess,
            Action<ApiError> onError)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                onSuccess?.Invoke(false);
                yield break;
            }

            var sanitized = Sanitize(items);

            if (_progressionClient == null)
            {
                UnityEngine.Debug.LogWarning(
                    "Inventory updates are server-authoritative. Configure a progression client before requesting updates.");
                onSuccess?.Invoke(false);
                yield break;
            }

            var expectedVersion = CharacterProgressionCache.TryGet(characterId, out var snapshot) && snapshot?.inventory != null
                ? snapshot.inventory.version
                : 0;

            CharacterProgressionEnvelope response = null;
            ApiError error = null;

            yield return _progressionClient.UpdateInventory(
                characterId,
                sanitized,
                expectedVersion,
                payload => response = payload,
                apiError => error = apiError);

            if (error != null)
            {
                onError?.Invoke(error);
                yield break;
            }

            if (response != null)
            {
                ApplySnapshot(characterId, response);
            }

            onSuccess?.Invoke(true);
        }

        public static IEnumerator AddItemAsync(
            string characterId,
            string itemId,
            int quantity,
            Action<bool> onSuccess,
            Action<ApiError> onError)
        {
            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
            {
                onSuccess?.Invoke(false);
                yield break;
            }

            var items = GetItems(characterId);
            var updated = MergeItem(items, itemId.Trim(), quantity);
            yield return ReplaceInventoryAsync(characterId, updated, onSuccess, onError);
        }

        public static IEnumerator RemoveItemAsync(
            string characterId,
            string itemId,
            int quantity,
            Action<bool> onSuccess,
            Action<ApiError> onError)
        {
            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
            {
                onSuccess?.Invoke(false);
                yield break;
            }

            var items = GetItems(characterId);
            var updated = RemoveQuantity(items, itemId.Trim(), quantity);
            yield return ReplaceInventoryAsync(characterId, updated, onSuccess, onError);
        }

        public static void ApplySnapshot(string characterId, CharacterProgressionEnvelope snapshot)
        {
            if (string.IsNullOrWhiteSpace(characterId) || snapshot == null)
            {
                return;
            }

            CharacterProgressionCache.Store(characterId, snapshot);

            ApplyInventoryState(characterId, snapshot.inventory);
        }

        public static void ApplyInventoryState(string characterId, CharacterInventoryCollection inventory)
        {
            if (string.IsNullOrWhiteSpace(characterId) || inventory?.items == null)
            {
                return;
            }

            UpdateLocalInventory(characterId, inventory.items);
        }

        private static void UpdateLocalInventory(string characterId, CharacterInventoryItemEntry[] items)
        {
            ItemsByCharacter[characterId] = CloneItems(items);
            InventoryChanged?.Invoke(characterId, CloneItems(items));
        }

        private static CharacterInventoryItemEntry[] Sanitize(CharacterInventoryItemEntry[] items)
        {
            if (items == null || items.Length == 0)
            {
                return Array.Empty<CharacterInventoryItemEntry>();
            }

            var sanitized = new List<CharacterInventoryItemEntry>();
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.itemId) || item.quantity <= 0)
                {
                    continue;
                }

                sanitized.Add(new CharacterInventoryItemEntry
                {
                    itemId = item.itemId.Trim(),
                    quantity = item.quantity,
                    metadataJson = string.IsNullOrWhiteSpace(item.metadataJson) ? "{}" : item.metadataJson.Trim()
                });
            }

            return sanitized.ToArray();
        }

        private static CharacterInventoryItemEntry[] MergeItem(
            CharacterInventoryItemEntry[] items,
            string itemId,
            int quantity)
        {
            var updated = items == null ? new List<CharacterInventoryItemEntry>() : new List<CharacterInventoryItemEntry>(items);
            var found = false;

            for (var i = 0; i < updated.Count; i++)
            {
                var item = updated[i];
                if (item == null || !string.Equals(item.itemId, itemId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                item.quantity += quantity;
                updated[i] = item;
                found = true;
                break;
            }

            if (!found)
            {
                updated.Add(new CharacterInventoryItemEntry
                {
                    itemId = itemId,
                    quantity = quantity,
                    metadataJson = "{}"
                });
            }

            return updated.ToArray();
        }

        private static CharacterInventoryItemEntry[] RemoveQuantity(
            CharacterInventoryItemEntry[] items,
            string itemId,
            int quantity)
        {
            if (items == null || items.Length == 0)
            {
                return Array.Empty<CharacterInventoryItemEntry>();
            }

            var updated = new List<CharacterInventoryItemEntry>();
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.itemId))
                {
                    continue;
                }

                if (!string.Equals(item.itemId, itemId, StringComparison.OrdinalIgnoreCase))
                {
                    updated.Add(item);
                    continue;
                }

                var remaining = item.quantity - quantity;
                if (remaining > 0)
                {
                    updated.Add(new CharacterInventoryItemEntry
                    {
                        itemId = item.itemId,
                        quantity = remaining,
                        metadataJson = item.metadataJson
                    });
                }
            }

            return updated.ToArray();
        }

        private static CharacterInventoryItemEntry[] CloneItems(CharacterInventoryItemEntry[] items)
        {
            if (items == null || items.Length == 0)
            {
                return Array.Empty<CharacterInventoryItemEntry>();
            }

            var clone = new CharacterInventoryItemEntry[items.Length];
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                clone[i] = item == null
                    ? null
                    : new CharacterInventoryItemEntry
                    {
                        itemId = item.itemId,
                        quantity = item.quantity,
                        metadataJson = item.metadataJson
                    };
            }

            return clone;
        }
    }
}
