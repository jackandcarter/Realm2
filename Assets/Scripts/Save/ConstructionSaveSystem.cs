using System;
using System.Collections.Generic;
using Building;
using Client;
using UnityEngine;

namespace Client.Save
{
    public static class ConstructionSaveSystem
    {
        private const string PlayerPrefsKey = "realm_construction_payload";

        private static readonly Dictionary<string, ConstructionSavePayload> PayloadCache =
            new Dictionary<string, ConstructionSavePayload>(StringComparer.OrdinalIgnoreCase);

        public static void RecordInstance(ConstructionInstance instance)
        {
            if (instance == null)
            {
                return;
            }

            var key = BuildStorageKey();
            if (key == null)
            {
                return;
            }

            if (!PayloadCache.TryGetValue(key, out var payload))
            {
                payload = LoadPayload(key);
            }

            var state = instance.CaptureState();
            if (payload == null)
            {
                payload = new ConstructionSavePayload();
            }

            payload.Upsert(state);
            Persist(key, payload);
        }

        public static IReadOnlyList<ConstructionInstance.SerializableConstructionState> LoadInstances()
        {
            var key = BuildStorageKey();
            if (key == null)
            {
                return Array.Empty<ConstructionInstance.SerializableConstructionState>();
            }

            if (!PayloadCache.TryGetValue(key, out var payload))
            {
                payload = LoadPayload(key);
            }

            if (payload?.Instances == null || payload.Instances.Count == 0)
            {
                return Array.Empty<ConstructionInstance.SerializableConstructionState>();
            }

            return payload.Instances;
        }

        private static ConstructionSavePayload LoadPayload(string key)
        {
            var json = PlayerPrefs.GetString(key, null);
            if (string.IsNullOrWhiteSpace(json))
            {
                var empty = new ConstructionSavePayload();
                PayloadCache[key] = empty;
                return empty;
            }

            try
            {
                var payload = JsonUtility.FromJson<ConstructionSavePayload>(json);
                if (payload == null)
                {
                    payload = new ConstructionSavePayload();
                }

                PayloadCache[key] = payload;
                return payload;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to deserialize construction payload for key {key}: {ex.Message}");
                var payload = new ConstructionSavePayload();
                PayloadCache[key] = payload;
                return payload;
            }
        }

        private static void Persist(string key, ConstructionSavePayload payload)
        {
            if (payload == null)
            {
                return;
            }

            try
            {
                var json = JsonUtility.ToJson(payload);
                PlayerPrefs.SetString(key, json);
                PlayerPrefs.Save();
                PayloadCache[key] = payload;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to persist construction payload for key {key}: {ex.Message}");
            }
        }

        private static string BuildStorageKey()
        {
            var realmId = SessionManager.SelectedRealmId;
            var characterId = SessionManager.SelectedCharacterId;

            if (string.IsNullOrWhiteSpace(realmId) || string.IsNullOrWhiteSpace(characterId))
            {
                return null;
            }

            return $"{realmId}_{characterId}_{PlayerPrefsKey}";
        }

        [Serializable]
        private class ConstructionSavePayload
        {
            public List<ConstructionInstance.SerializableConstructionState> Instances =
                new List<ConstructionInstance.SerializableConstructionState>();

            public void Upsert(ConstructionInstance.SerializableConstructionState state)
            {
                if (string.IsNullOrWhiteSpace(state.InstanceId))
                {
                    return;
                }

                var normalized = state.InstanceId.Trim();
                for (var i = 0; i < Instances.Count; i++)
                {
                    if (Instances[i].InstanceId == normalized)
                    {
                        Instances[i] = state;
                        return;
                    }
                }

                Instances.Add(state);
            }
        }
    }
}
