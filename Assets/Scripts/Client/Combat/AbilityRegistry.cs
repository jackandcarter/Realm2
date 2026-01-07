using System;
using System.Collections.Generic;
using Realm.Abilities;
using UnityEngine;

namespace Client.Combat
{
    public static class AbilityRegistry
    {
        private static readonly Dictionary<string, AbilityDefinition> Lookup =
            new Dictionary<string, AbilityDefinition>(StringComparer.OrdinalIgnoreCase);

        private static bool _initialized;
        private static bool _resourcesLoaded;

        public static void RegisterAbilities(IEnumerable<AbilityDefinition> abilities)
        {
            if (abilities == null)
            {
                return;
            }

            foreach (var ability in abilities)
            {
                if (ability == null || string.IsNullOrWhiteSpace(ability.Guid))
                {
                    continue;
                }

                Lookup[ability.Guid] = ability;
            }

            _initialized = true;
        }

        public static bool TryGetAbility(string guid, out AbilityDefinition ability)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(guid))
            {
                ability = null;
                return false;
            }

            return Lookup.TryGetValue(guid, out ability);
        }

        public static AbilityDefinition GetAbility(string guid)
        {
            return TryGetAbility(guid, out var ability) ? ability : null;
        }

        private static void EnsureLoaded()
        {
            if (_initialized)
            {
                return;
            }

            LoadFromResources();
            _initialized = true;
        }

        private static void LoadFromResources()
        {
            if (_resourcesLoaded)
            {
                return;
            }

            var resources = Resources.LoadAll<AbilityDefinition>(string.Empty);
            if (resources != null && resources.Length > 0)
            {
                RegisterAbilities(resources);
            }

            _resourcesLoaded = true;
        }
    }
}
