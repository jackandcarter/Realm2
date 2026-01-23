using System;
using System.Collections;
using System.Collections.Generic;
using Client.Progression;
using UnityEngine;

namespace Client.CharacterCreation
{
    public static class ClassUnlockRepository
    {
        private static readonly Dictionary<string, ClassUnlockState[]> StatesByCharacter = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, CharacterInfo> Characters = new(StringComparer.OrdinalIgnoreCase);
        private static CharacterProgressionClient _progressionClient;

        public static event Action<string, ClassUnlockState[]> ClassUnlockStatesChanged;

        public static void SetProgressionClient(CharacterProgressionClient client)
        {
            _progressionClient = client;
        }

        public static void TrackCharacter(CharacterInfo character)
        {
            if (character == null || string.IsNullOrWhiteSpace(character.id))
            {
                return;
            }

            var sanitized = ClassUnlockUtility.SanitizeStates(character.classStates);
            StatesByCharacter[character.id] = sanitized;
            character.classStates = ClassUnlockUtility.CloneStates(sanitized);
            Characters[character.id] = character;
            RaiseChanged(character.id);
        }

        public static bool TryGetCharacter(string characterId, out CharacterInfo character)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                character = null;
                return false;
            }

            return Characters.TryGetValue(characterId, out character);
        }

        public static ClassUnlockState[] GetStates(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || !StatesByCharacter.TryGetValue(characterId, out var states))
            {
                return Array.Empty<ClassUnlockState>();
            }

            return ClassUnlockUtility.CloneStates(states);
        }

        public static bool IsClassUnlocked(string characterId, string classId)
        {
            if (string.IsNullOrWhiteSpace(classId))
            {
                return false;
            }

            var states = GetStates(characterId);
            if (states == null || states.Length == 0)
            {
                return false;
            }

            return ClassUnlockUtility.TryGetState(states, classId, out var state) && state != null && state.Unlocked;
        }

        public static IEnumerator UnlockClassAsync(
            string characterId,
            string classId,
            Action<bool> onSuccess,
            Action<ApiError> onError)
        {
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(classId))
            {
                onSuccess?.Invoke(false);
                yield break;
            }

            if (_progressionClient == null)
            {
                UnityEngine.Debug.LogWarning(
                    "Class unlocks are server-authoritative. Configure a progression client before requesting unlocks.");
                onSuccess?.Invoke(false);
                yield break;
            }

            var currentStates = GetStates(characterId);
            var sanitized = ClassUnlockUtility.SanitizeStates(currentStates);
            sanitized = EnsureUnlocked(sanitized, classId, out var unlockChanged);

            if (!unlockChanged)
            {
                onSuccess?.Invoke(false);
                yield break;
            }

            var entries = ToEntries(sanitized);
            var expectedVersion = CharacterProgressionCache.GetClassUnlockVersion(characterId);

            CharacterProgressionEnvelope response = null;
            ApiError error = null;

            yield return _progressionClient.UpdateClassUnlocks(
                characterId,
                entries,
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

        public static void UpdateStates(string characterId, ClassUnlockState[] states)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return;
            }

            var sanitized = ClassUnlockUtility.SanitizeStates(states);
            StatesByCharacter[characterId] = sanitized;

            if (Characters.TryGetValue(characterId, out var character))
            {
                character.classStates = ClassUnlockUtility.CloneStates(sanitized);
            }

            RaiseChanged(characterId);
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

        public static void ApplySnapshot(string characterId, CharacterProgressionEnvelope snapshot)
        {
            if (string.IsNullOrWhiteSpace(characterId) || snapshot == null)
            {
                return;
            }

            CharacterProgressionCache.Store(characterId, snapshot);

            if (snapshot.classUnlocks?.unlocks != null)
            {
                var states = ConvertEntriesToStates(snapshot.classUnlocks.unlocks);
                UpdateStates(characterId, states);
            }
        }

        private static void RaiseChanged(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return;
            }

            if (!StatesByCharacter.TryGetValue(characterId, out var states))
            {
                states = Array.Empty<ClassUnlockState>();
            }

            ClassUnlockStatesChanged?.Invoke(characterId, ClassUnlockUtility.CloneStates(states));
        }

        private static ClassUnlockState[] EnsureUnlocked(ClassUnlockState[] states, string classId, out bool changed)
        {
            changed = false;
            if (states == null)
            {
                states = Array.Empty<ClassUnlockState>();
            }

            var normalized = classId.Trim();
            foreach (var state in states)
            {
                if (state != null && string.Equals(state.ClassId, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    if (!state.Unlocked)
                    {
                        state.Unlocked = true;
                        changed = true;
                    }
                    return states;
                }
            }

            var expanded = new ClassUnlockState[states.Length + 1];
            Array.Copy(states, expanded, states.Length);
            expanded[^1] = new ClassUnlockState { ClassId = normalized, Unlocked = true };
            changed = true;
            return expanded;
        }

        private static ClassUnlockState[] ConvertEntriesToStates(CharacterClassUnlockEntry[] entries)
        {
            if (entries == null || entries.Length == 0)
            {
                return Array.Empty<ClassUnlockState>();
            }

            var states = new ClassUnlockState[entries.Length];
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                states[i] = entry == null
                    ? null
                    : new ClassUnlockState { ClassId = entry.classId, Unlocked = entry.unlocked };
            }

            return ClassUnlockUtility.SanitizeStates(states);
        }

        private static CharacterClassUnlockEntry[] ToEntries(ClassUnlockState[] states)
        {
            if (states == null || states.Length == 0)
            {
                return Array.Empty<CharacterClassUnlockEntry>();
            }

            var entries = new CharacterClassUnlockEntry[states.Length];
            for (var i = 0; i < states.Length; i++)
            {
                var state = states[i];
                entries[i] = state == null
                    ? null
                    : new CharacterClassUnlockEntry
                    {
                        classId = state.ClassId,
                        unlocked = state.Unlocked,
                        unlockedAt = state.Unlocked ? DateTime.UtcNow.ToString("O") : string.Empty
                    };
            }

            return entries;
        }
    }
}
