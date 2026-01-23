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

            UnityEngine.Debug.LogWarning(
                "Client-side construction persistence is disabled. Construction state must be stored on the server.",
                instance);
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

            if (string.IsNullOrWhiteSpace(state.InstanceId))
            {
                return list;
            }

            var normalized = state.InstanceId.Trim();
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].InstanceId == normalized)
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
