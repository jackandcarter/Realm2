using System;
using System.Collections.Generic;

namespace Client.CharacterCreation
{
    public static class ClassUnlockUtility
    {
        public const string BuilderClassId = "builder";

        public static ClassUnlockState[] SanitizeStates(ClassUnlockState[] states)
        {
            var lookup = new Dictionary<string, ClassUnlockState>(StringComparer.OrdinalIgnoreCase);

            if (states != null)
            {
                foreach (var state in states)
                {
                    if (state == null || string.IsNullOrWhiteSpace(state.ClassId))
                    {
                        continue;
                    }

                    var trimmedId = state.ClassId.Trim();
                    if (lookup.TryGetValue(trimmedId, out var existing))
                    {
                        existing.Unlocked |= state.Unlocked;
                    }
                    else
                    {
                        lookup[trimmedId] = new ClassUnlockState
                        {
                            ClassId = trimmedId,
                            Unlocked = state.Unlocked
                        };
                    }
                }
            }

            if (!lookup.ContainsKey(BuilderClassId))
            {
                lookup[BuilderClassId] = new ClassUnlockState
                {
                    ClassId = BuilderClassId,
                    Unlocked = false
                };
            }

            var result = new ClassUnlockState[lookup.Count];
            lookup.Values.CopyTo(result, 0);
            return result;
        }

        public static bool TryGetState(ClassUnlockState[] states, string classId, out ClassUnlockState state)
        {
            state = null;
            if (states == null || string.IsNullOrWhiteSpace(classId))
            {
                return false;
            }

            foreach (var entry in states)
            {
                if (entry != null && string.Equals(entry.ClassId, classId, StringComparison.OrdinalIgnoreCase))
                {
                    state = entry;
                    return true;
                }
            }

            return false;
        }

        public static ClassUnlockState[] CloneStates(ClassUnlockState[] states)
        {
            if (states == null || states.Length == 0)
            {
                return Array.Empty<ClassUnlockState>();
            }

            var clone = new ClassUnlockState[states.Length];
            var index = 0;
            foreach (var state in states)
            {
                if (state == null || string.IsNullOrWhiteSpace(state.ClassId))
                {
                    continue;
                }

                clone[index++] = new ClassUnlockState
                {
                    ClassId = state.ClassId.Trim(),
                    Unlocked = state.Unlocked
                };
            }

            if (index == clone.Length)
            {
                return clone;
            }

            var resized = new ClassUnlockState[index];
            Array.Copy(clone, resized, index);
            return resized;
        }
    }
}
