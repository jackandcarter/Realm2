using System;
using Client.CharacterCreation;
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

            if (string.IsNullOrWhiteSpace(classId) || string.IsNullOrWhiteSpace(abilityId))
            {
                return false;
            }

            if (!ClassAbilityCatalog.TryGetAbilityUnlockLevel(classId, abilityId, out var unlockLevel))
            {
                return false;
            }

            var level = GetCharacterLevel(_currentCharacterId);
            return level >= unlockLevel;
        }

        private static int GetCharacterLevel(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return 0;
            }

            if (CharacterProgressionCache.TryGet(characterId, out var snapshot) && snapshot?.progression != null)
            {
                return Math.Max(1, snapshot.progression.level);
            }

            return 1;
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
