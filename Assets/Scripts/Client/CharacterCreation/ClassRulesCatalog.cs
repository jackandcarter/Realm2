using System;
using System.Collections.Generic;

namespace Client.CharacterCreation
{
    public enum ClassUnlockMethod
    {
        Starter,
        Quest
    }

    public class ClassRuleDefinition
    {
        public string ClassId;
        public ClassUnlockMethod UnlockMethod;
        public string[] ExclusiveRaceIds;
    }

    public static class ClassRulesCatalog
    {
        private static readonly ClassRuleDefinition[] Rules =
        {
            new ClassRuleDefinition { ClassId = "warrior", UnlockMethod = ClassUnlockMethod.Starter },
            new ClassRuleDefinition { ClassId = "wizard", UnlockMethod = ClassUnlockMethod.Starter },
            new ClassRuleDefinition { ClassId = "time-mage", UnlockMethod = ClassUnlockMethod.Starter },
            new ClassRuleDefinition { ClassId = "sage", UnlockMethod = ClassUnlockMethod.Starter },
            new ClassRuleDefinition { ClassId = "rogue", UnlockMethod = ClassUnlockMethod.Starter },
            new ClassRuleDefinition
            {
                ClassId = "ranger",
                UnlockMethod = ClassUnlockMethod.Starter,
                ExclusiveRaceIds = new[] { "felarian" }
            },
            new ClassRuleDefinition
            {
                ClassId = "necromancer",
                UnlockMethod = ClassUnlockMethod.Starter,
                ExclusiveRaceIds = new[] { "revenant" }
            },
            new ClassRuleDefinition
            {
                ClassId = "technomancer",
                UnlockMethod = ClassUnlockMethod.Starter,
                ExclusiveRaceIds = new[] { "gearling" }
            },
            new ClassRuleDefinition { ClassId = "builder", UnlockMethod = ClassUnlockMethod.Quest }
        };

        private static readonly Dictionary<string, ClassRuleDefinition> Lookup;

        static ClassRulesCatalog()
        {
            Lookup = new Dictionary<string, ClassRuleDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var rule in Rules)
            {
                if (rule?.ClassId == null || Lookup.ContainsKey(rule.ClassId))
                {
                    continue;
                }

                Lookup[rule.ClassId] = rule;
            }
        }

        public static IReadOnlyList<ClassRuleDefinition> GetAllRules()
        {
            return Rules;
        }

        public static bool TryGetRule(string classId, out ClassRuleDefinition rule)
        {
            if (string.IsNullOrWhiteSpace(classId))
            {
                rule = null;
                return false;
            }

            return Lookup.TryGetValue(classId, out rule);
        }

        public static bool IsClassAllowedForRace(string classId, string raceId)
        {
            if (string.IsNullOrWhiteSpace(classId) || string.IsNullOrWhiteSpace(raceId))
            {
                return false;
            }

            if (!TryGetRule(classId, out var rule) || rule == null)
            {
                return false;
            }

            if (rule.ExclusiveRaceIds == null || rule.ExclusiveRaceIds.Length == 0)
            {
                return true;
            }

            foreach (var allowedRace in rule.ExclusiveRaceIds)
            {
                if (!string.IsNullOrWhiteSpace(allowedRace)
                    && string.Equals(allowedRace.Trim(), raceId.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static string[] GetAllowedClassIdsForRace(string raceId)
        {
            if (string.IsNullOrWhiteSpace(raceId))
            {
                return Array.Empty<string>();
            }

            var results = new List<string>();
            foreach (var rule in Rules)
            {
                if (rule != null && IsClassAllowedForRace(rule.ClassId, raceId))
                {
                    results.Add(rule.ClassId);
                }
            }

            return results.ToArray();
        }

        public static string[] GetStarterClassIdsForRace(string raceId)
        {
            if (string.IsNullOrWhiteSpace(raceId))
            {
                return Array.Empty<string>();
            }

            var results = new List<string>();
            foreach (var rule in Rules)
            {
                if (rule != null
                    && rule.UnlockMethod == ClassUnlockMethod.Starter
                    && IsClassAllowedForRace(rule.ClassId, raceId))
                {
                    results.Add(rule.ClassId);
                }
            }

            return results.ToArray();
        }

        public static bool IsStarterClassForRace(string classId, string raceId)
        {
            if (string.IsNullOrWhiteSpace(classId) || string.IsNullOrWhiteSpace(raceId))
            {
                return false;
            }

            foreach (var starterId in GetStarterClassIdsForRace(raceId))
            {
                if (string.Equals(starterId, classId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
