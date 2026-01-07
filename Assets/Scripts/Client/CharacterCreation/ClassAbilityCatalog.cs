using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Client.CharacterCreation
{
    public static class ClassAbilityCatalog
    {
        private static ClassAbilityProgression _progression;
        private static bool _attemptedLoad;

        public static IReadOnlyList<ClassAbilityDisplayInfo> GetAbilityDisplayInfo(string classId)
        {
            EnsureLoaded();
            if (_progression == null || string.IsNullOrWhiteSpace(classId))
            {
                return Array.Empty<ClassAbilityDisplayInfo>();
            }

            var progression = _progression.GetProgression(classId);
            if (progression == null || progression.Count == 0)
            {
                return Array.Empty<ClassAbilityDisplayInfo>();
            }

            var results = new List<ClassAbilityDisplayInfo>();
            foreach (var levelEntry in progression)
            {
                if (levelEntry?.Abilities == null)
                {
                    continue;
                }

                foreach (var ability in levelEntry.Abilities)
                {
                    if (ability == null)
                    {
                        continue;
                    }

                    results.Add(new ClassAbilityDisplayInfo(levelEntry.Level, ability.DisplayName, ability.Description, ability.Tooltip));
                }
            }

            return results;
        }

        public static IReadOnlyList<ClassAbilityProgression.ClassAbilityDefinition> GetAbilitiesForLevel(string classId, int level)
        {
            EnsureLoaded();
            if (_progression == null)
            {
                return Array.Empty<ClassAbilityProgression.ClassAbilityDefinition>();
            }

            return _progression.GetAbilitiesUnlockedAtLevel(classId, level);
        }

        public static IReadOnlyList<ClassAbilityDockEntry> GetAbilityDockEntries(string classId)
        {
            EnsureLoaded();
            if (_progression == null || string.IsNullOrWhiteSpace(classId))
            {
                return Array.Empty<ClassAbilityDockEntry>();
            }

            var progression = _progression.GetProgression(classId);
            if (progression == null || progression.Count == 0)
            {
                return Array.Empty<ClassAbilityDockEntry>();
            }

            var lookup = new Dictionary<string, ClassAbilityDockEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var levelEntry in progression)
            {
                if (levelEntry?.Abilities == null)
                {
                    continue;
                }

                foreach (var ability in levelEntry.Abilities)
                {
                    if (ability == null || string.IsNullOrWhiteSpace(ability.AbilityGuid))
                    {
                        continue;
                    }

                    var abilityId = ability.AbilityGuid.Trim();
                    lookup[abilityId] = new ClassAbilityDockEntry(
                        abilityId,
                        ability.DisplayName,
                        ability.Description,
                        ability.Tooltip,
                        levelEntry.Level);
                }
            }

            if (lookup.Count == 0)
            {
                return Array.Empty<ClassAbilityDockEntry>();
            }

            return lookup.Values
                .OrderBy(entry => entry.Level)
                .ThenBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static IReadOnlyList<string> GetKnownClassIds()
        {
            EnsureLoaded();
            if (_progression == null)
            {
                return Array.Empty<string>();
            }

            return _progression.GetTrackedClassIds();
        }

        private static void EnsureLoaded()
        {
            if (_attemptedLoad)
            {
                return;
            }

            _progression = Resources.Load<ClassAbilityProgression>("Progression/ClassAbilityProgression");
            if (_progression == null)
            {
                Debug.LogWarning("ClassAbilityCatalog could not locate the ClassAbilityProgression asset under Resources/Progression.");
            }

            _attemptedLoad = true;
        }

        public readonly struct ClassAbilityDisplayInfo
        {
            public readonly int Level;
            public readonly string DisplayName;
            public readonly string Description;
            public readonly string Tooltip;

            public ClassAbilityDisplayInfo(int level, string displayName, string description, string tooltip)
            {
                Level = level;
                DisplayName = displayName;
                Description = description;
                Tooltip = tooltip;
            }
        }

        public readonly struct ClassAbilityDockEntry
        {
            public readonly string AbilityId;
            public readonly string DisplayName;
            public readonly string Description;
            public readonly string Tooltip;
            public readonly int Level;

            public bool IsValid => !string.IsNullOrWhiteSpace(AbilityId);

            public ClassAbilityDockEntry(string abilityId, string displayName, string description, string tooltip, int level)
            {
                AbilityId = abilityId;
                DisplayName = displayName;
                Description = description;
                Tooltip = tooltip;
                Level = level;
            }
        }
    }
}
