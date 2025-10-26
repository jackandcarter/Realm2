using System;
using System.Collections.Generic;

namespace Client.CharacterCreation
{
    public static class ClassUnlockRepository
    {
        private static readonly Dictionary<string, ClassUnlockState[]> StatesByCharacter = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, CharacterInfo> Characters = new(StringComparer.OrdinalIgnoreCase);

        public static event Action<string, ClassUnlockState[]> ClassUnlockStatesChanged;

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

        public static ClassUnlockState[] GetStates(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || !StatesByCharacter.TryGetValue(characterId, out var states))
            {
                return Array.Empty<ClassUnlockState>();
            }

            return ClassUnlockUtility.CloneStates(states);
        }

        public static bool UnlockClass(string characterId, string classId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(classId))
            {
                return false;
            }

            if (!StatesByCharacter.TryGetValue(characterId, out var states))
            {
                states = ClassUnlockUtility.SanitizeStates(null);
                StatesByCharacter[characterId] = states;
            }

            var changed = false;
            foreach (var state in states)
            {
                if (state != null && string.Equals(state.ClassId, classId, StringComparison.OrdinalIgnoreCase))
                {
                    if (!state.Unlocked)
                    {
                        state.Unlocked = true;
                        changed = true;
                    }

                    break;
                }
            }

            if (!changed)
            {
                return false;
            }

            if (Characters.TryGetValue(characterId, out var character))
            {
                character.classStates = ClassUnlockUtility.CloneStates(states);
            }

            RaiseChanged(characterId);
            return true;
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
    }
}
