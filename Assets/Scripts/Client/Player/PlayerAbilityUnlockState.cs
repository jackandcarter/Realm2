using System;
using Client.Progression;

namespace Client.Player
{
    public static class PlayerAbilityUnlockState
    {
        private static bool _initialized;
        private static string _currentCharacterId;
        public static event Action AbilityUnlocksChanged;

        public static bool IsAbilityUnlocked(string classId, string abilityId)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(abilityId))
            {
                return false;
            }

            var normalizedAbilityId = abilityId.Trim();
            if (string.IsNullOrWhiteSpace(_currentCharacterId))
            {
                return false;
            }

            if (!CharacterProgressionCache.TryGet(_currentCharacterId, out var snapshot) ||
                snapshot?.abilityUnlocks?.unlocks == null)
            {
                return false;
            }

            foreach (var entry in snapshot.abilityUnlocks.unlocks)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.abilityId))
                {
                    continue;
                }

                if (string.Equals(entry.abilityId.Trim(), normalizedAbilityId, StringComparison.OrdinalIgnoreCase))
                {
                    return entry.unlocked;
                }
            }

            return false;
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
        }

        private static void OnSelectedCharacterChanged(string characterId)
        {
            EnsureInitialized();
            _currentCharacterId = string.IsNullOrWhiteSpace(characterId) ? null : characterId;
            AbilityUnlocksChanged?.Invoke();
        }

        private static void OnSessionCleared()
        {
            EnsureInitialized();
            _currentCharacterId = null;
            AbilityUnlocksChanged?.Invoke();
        }

        private static void OnProgressionSnapshotChanged(string characterId)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(_currentCharacterId))
            {
                return;
            }

            if (string.Equals(_currentCharacterId, characterId, StringComparison.Ordinal))
            {
                AbilityUnlocksChanged?.Invoke();
            }
        }
    }
}
