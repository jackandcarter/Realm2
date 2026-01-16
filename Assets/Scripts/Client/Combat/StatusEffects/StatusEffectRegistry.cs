using System;
using System.Collections.Generic;
using Realm.Combat.Data;
using UnityEngine;

namespace Client.Combat.StatusEffects
{
    public static class StatusEffectRegistry
    {
        private static readonly Dictionary<string, StatusEffectDefinition> Lookup =
            new(StringComparer.OrdinalIgnoreCase);

        private static bool _initialized;
        private static bool _resourcesLoaded;

        public static void RegisterStatuses(IEnumerable<StatusEffectDefinition> definitions)
        {
            if (definitions == null)
            {
                return;
            }

            foreach (var definition in definitions)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.StatusId))
                {
                    continue;
                }

                Lookup[definition.StatusId.Trim()] = definition;
            }

            _initialized = true;
        }

        public static bool TryGetStatus(string statusId, out StatusEffectDefinition definition)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(statusId))
            {
                definition = null;
                return false;
            }

            return Lookup.TryGetValue(statusId.Trim(), out definition);
        }

        public static StatusEffectDefinition GetStatus(string statusId)
        {
            return TryGetStatus(statusId, out var definition) ? definition : null;
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

            var resources = Resources.LoadAll<StatusEffectDefinition>(string.Empty);
            if (resources != null && resources.Length > 0)
            {
                RegisterStatuses(resources);
            }

            _resourcesLoaded = true;
        }
    }
}
