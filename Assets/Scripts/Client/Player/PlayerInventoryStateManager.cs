using System;
using Client.Inventory;
using Client.Progression;

namespace Client.Player
{
    public static class PlayerInventoryStateManager
    {
        private static bool _initialized;
        private static string _currentCharacterId;

        public static event Action<string, CharacterInventoryItemEntry[]> InventoryChanged;

        public static CharacterInventoryItemEntry[] GetCurrentInventory()
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(_currentCharacterId))
            {
                return Array.Empty<CharacterInventoryItemEntry>();
            }

            return InventoryRepository.GetItems(_currentCharacterId);
        }

        public static CharacterInventoryItemEntry[] GetInventory(string characterId)
        {
            EnsureInitialized();
            return InventoryRepository.GetItems(characterId);
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _currentCharacterId = SessionManager.SelectedCharacterId;

            SessionManager.SelectedCharacterChanged += OnSelectedCharacterChanged;
            SessionManager.SessionCleared += OnSessionCleared;
            CharacterProgressionCache.ProgressionSnapshotChanged += OnProgressionSnapshotChanged;
            InventoryRepository.InventoryChanged += OnInventoryChanged;
        }

        private static void OnSelectedCharacterChanged(string characterId)
        {
            _currentCharacterId = string.IsNullOrWhiteSpace(characterId) ? null : characterId;
            RaiseChanged();
        }

        private static void OnSessionCleared()
        {
            _currentCharacterId = null;
            RaiseChanged();
        }

        private static void OnProgressionSnapshotChanged(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || !string.Equals(characterId, _currentCharacterId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (CharacterProgressionCache.TryGet(characterId, out var snapshot) && snapshot?.inventory?.items != null)
            {
                InventoryRepository.ApplyInventoryState(characterId, snapshot.inventory);
            }
        }

        private static void OnInventoryChanged(string characterId, CharacterInventoryItemEntry[] items)
        {
            if (string.IsNullOrWhiteSpace(_currentCharacterId) ||
                !string.Equals(characterId, _currentCharacterId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            InventoryChanged?.Invoke(characterId, items);
        }

        private static void RaiseChanged()
        {
            var items = string.IsNullOrWhiteSpace(_currentCharacterId)
                ? Array.Empty<CharacterInventoryItemEntry>()
                : InventoryRepository.GetItems(_currentCharacterId);
            InventoryChanged?.Invoke(_currentCharacterId, items);
        }
    }
}
