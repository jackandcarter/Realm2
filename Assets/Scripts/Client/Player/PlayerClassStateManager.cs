using System;
using System.Collections.Generic;
using Client.CharacterCreation;

namespace Client.Player
{
    public static class PlayerClassStateManager
    {
        private static readonly Dictionary<string, string> ActiveClassByCharacter = new(StringComparer.OrdinalIgnoreCase);

        private static bool _initialized;
        private static string _currentCharacterId;
        private static string _activeClassId;
        private static bool _isArkitectAvailable;

        public static event Action<string> ActiveClassChanged;
        public static event Action<bool> ArkitectAvailabilityChanged;

        public static string ActiveClassId
        {
            get
            {
                EnsureInitialized();
                return _activeClassId;
            }
        }

        public static bool IsArkitectAvailable
        {
            get
            {
                EnsureInitialized();
                return _isArkitectAvailable;
            }
        }

        public static bool TrySetActiveClass(string classId)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(_currentCharacterId))
            {
                return false;
            }

            var normalized = NormalizeClassId(classId);
            if (normalized == null)
            {
                return false;
            }

            if (!ClassUnlockRepository.IsClassUnlocked(_currentCharacterId, normalized))
            {
                return false;
            }

            return SetActiveClassForCharacter(_currentCharacterId, normalized, true);
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            SessionManager.SelectedCharacterChanged += OnSelectedCharacterChanged;
            SessionManager.SessionCleared += OnSessionCleared;
            ClassUnlockRepository.ClassUnlockStatesChanged += OnClassUnlockStatesChanged;

            if (!string.IsNullOrWhiteSpace(SessionManager.SelectedCharacterId))
            {
                _currentCharacterId = SessionManager.SelectedCharacterId;
                RefreshActiveClass(true);
            }
            else
            {
                UpdateActiveClass(null, true);
                UpdateAvailability(null, true);
            }
        }

        private static void OnSelectedCharacterChanged(string characterId)
        {
            EnsureInitialized();

            _currentCharacterId = string.IsNullOrWhiteSpace(characterId) ? null : characterId;
            RefreshActiveClass(true);
        }

        private static void OnSessionCleared()
        {
            EnsureInitialized();

            ActiveClassByCharacter.Clear();
            _currentCharacterId = null;
            UpdateActiveClass(null, true);
            UpdateAvailability(null, true);
        }

        private static void OnClassUnlockStatesChanged(string characterId, ClassUnlockState[] _)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(characterId))
            {
                return;
            }

            if (ActiveClassByCharacter.TryGetValue(characterId, out var cached) &&
                !ClassUnlockRepository.IsClassUnlocked(characterId, cached))
            {
                ActiveClassByCharacter.Remove(characterId);
            }

            if (string.Equals(characterId, _currentCharacterId, StringComparison.Ordinal))
            {
                RefreshActiveClass(true);
            }
        }

        private static void RefreshActiveClass(bool emitEvents)
        {
            if (string.IsNullOrWhiteSpace(_currentCharacterId))
            {
                UpdateActiveClass(null, emitEvents);
                UpdateAvailability(null, emitEvents);
                return;
            }

            var resolved = DetermineActiveClassFor(_currentCharacterId);
            SetActiveClassForCharacter(_currentCharacterId, resolved, emitEvents);
        }

        private static string DetermineActiveClassFor(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return null;
            }

            if (ActiveClassByCharacter.TryGetValue(characterId, out var cached) &&
                ClassUnlockRepository.IsClassUnlocked(characterId, cached))
            {
                return cached;
            }

            if (ClassUnlockRepository.TryGetCharacter(characterId, out var character))
            {
                var tracked = NormalizeClassId(character.classId);
                if (tracked != null && ClassUnlockRepository.IsClassUnlocked(characterId, tracked))
                {
                    ActiveClassByCharacter[characterId] = tracked;
                    return tracked;
                }
            }

            var states = ClassUnlockRepository.GetStates(characterId);
            if (states != null)
            {
                foreach (var state in states)
                {
                    if (state == null || !state.Unlocked)
                    {
                        continue;
                    }

                    var candidate = NormalizeClassId(state.ClassId);
                    if (candidate != null)
                    {
                        ActiveClassByCharacter[characterId] = candidate;
                        return candidate;
                    }
                }
            }

            ActiveClassByCharacter.Remove(characterId);
            return null;
        }

        private static bool SetActiveClassForCharacter(string characterId, string classId, bool emitEvents)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(classId))
            {
                ActiveClassByCharacter.Remove(characterId);
            }
            else
            {
                ActiveClassByCharacter[characterId] = classId;
            }

            if (!string.Equals(characterId, _currentCharacterId, StringComparison.Ordinal))
            {
                return false;
            }

            var activeChanged = UpdateActiveClass(classId, emitEvents);
            var availabilityChanged = UpdateAvailability(classId, emitEvents);
            return activeChanged || availabilityChanged;
        }

        private static bool UpdateActiveClass(string classId, bool emitEvents)
        {
            var normalized = NormalizeClassId(classId);
            if (string.Equals(_activeClassId, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            _activeClassId = normalized;
            if (emitEvents)
            {
                ActiveClassChanged?.Invoke(_activeClassId);
            }

            return true;
        }

        private static bool UpdateAvailability(string classId, bool emitEvents)
        {
            var normalized = NormalizeClassId(classId);
            var available = !string.IsNullOrWhiteSpace(_currentCharacterId) &&
                            normalized != null &&
                            string.Equals(normalized, ClassUnlockUtility.BuilderClassId, StringComparison.OrdinalIgnoreCase) &&
                            ClassUnlockRepository.IsClassUnlocked(_currentCharacterId, normalized);

            if (_isArkitectAvailable == available)
            {
                return false;
            }

            _isArkitectAvailable = available;
            if (emitEvents)
            {
                ArkitectAvailabilityChanged?.Invoke(_isArkitectAvailable);
            }

            return true;
        }

        private static string NormalizeClassId(string classId)
        {
            return string.IsNullOrWhiteSpace(classId) ? null : classId.Trim();
        }
    }
}
