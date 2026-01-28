using System;
using System.Collections.Generic;
using Building;
using Client;
using Client.BuildState;

namespace Client.Save
{
    public static class ConstructionSaveSystem
    {
        public static void RecordInstance(ConstructionInstance instance)
        {
            if (instance == null)
            {
                return;
            }

            var (realmId, characterId) = ResolveSession();
            if (string.IsNullOrWhiteSpace(realmId) || string.IsNullOrWhiteSpace(characterId))
            {
                return;
            }

            var existing = BuildStateRepository.GetConstructions(realmId, characterId);
            var updated = Upsert(existing, instance.CaptureState());
            BuildStateRepository.SaveConstructions(realmId, characterId, updated);
        }

        public static IReadOnlyList<ConstructionInstance.SerializableConstructionState> LoadInstances()
        {
            var (realmId, characterId) = ResolveSession();
            if (string.IsNullOrWhiteSpace(realmId) || string.IsNullOrWhiteSpace(characterId))
            {
                return Array.Empty<ConstructionInstance.SerializableConstructionState>();
            }

            return BuildStateRepository.GetConstructions(realmId, characterId);
        }

        private static List<ConstructionInstance.SerializableConstructionState> Upsert(
            IReadOnlyList<ConstructionInstance.SerializableConstructionState> existing,
            ConstructionInstance.SerializableConstructionState state)
        {
            var list = existing != null
                ? new List<ConstructionInstance.SerializableConstructionState>(existing)
                : new List<ConstructionInstance.SerializableConstructionState>();

            var resolvedId = string.IsNullOrWhiteSpace(state.ConstructionId) ? state.InstanceId : state.ConstructionId;
            if (string.IsNullOrWhiteSpace(resolvedId))
            {
                return list;
            }

            var normalized = resolvedId.Trim();
            for (var i = 0; i < list.Count; i++)
            {
                var existingId = string.IsNullOrWhiteSpace(list[i].ConstructionId)
                    ? list[i].InstanceId
                    : list[i].ConstructionId;
                if (existingId == normalized)
                {
                    list[i] = state;
                    return list;
                }
            }

            list.Add(state);
            return list;
        }

        private static (string realmId, string characterId) ResolveSession()
        {
            return (SessionManager.SelectedRealmId, SessionManager.SelectedCharacterId);
        }
    }
}
