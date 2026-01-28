using System;
using System.Collections.Generic;
using Building;
using Client;
using Client.Progression;
using Client.Terrain;
using UnityEngine;

namespace Client.BuildState
{
    public static class BuildStateRepository
    {
        private static readonly Dictionary<string, BuildStateSnapshot> Cache = new(StringComparer.OrdinalIgnoreCase);
        private static BuildStateClient _client;
        private static string _activeKey;

        public static event Action<string, string> BuildStateUpdated;

        public static void SetClient(BuildStateClient client)
        {
            _client = client;
            SessionManager.SelectedCharacterChanged -= HandleCharacterChanged;
            SessionManager.SelectedCharacterChanged += HandleCharacterChanged;
            SessionManager.SessionCleared -= HandleSessionCleared;
            SessionManager.SessionCleared += HandleSessionCleared;
        }

        public static BuildPlotDefinition[] GetPlots(string realmId, string characterId)
        {
            var snapshot = GetSnapshot(realmId, characterId);
            return snapshot?.plots ?? Array.Empty<BuildPlotDefinition>();
        }

        public static IReadOnlyList<ConstructionInstance.SerializableConstructionState> GetConstructions(
            string realmId,
            string characterId)
        {
            var snapshot = GetSnapshot(realmId, characterId);
            return snapshot?.constructions ?? Array.Empty<ConstructionInstance.SerializableConstructionState>();
        }

        public static void SavePlots(
            string realmId,
            string characterId,
            IEnumerable<BuildPlotDefinition> plots)
        {
            if (plots == null)
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(realmId) || string.IsNullOrWhiteSpace(characterId))
            {
                Debug.LogWarning("BuildStateRepository.SavePlots called without an active realm or character.");
                return;
            }

            var snapshot = GetOrCreateSnapshot(realmId, characterId);
            snapshot.plots = CopyPlots(plots);
            snapshot.updatedAt = DateTime.UtcNow.ToString("O");

            if (_client == null)
            {
                Cache[BuildKey(realmId, characterId)] = snapshot;
                Debug.LogWarning("BuildStateRepository has no client configured; plot updates are cached locally.");
                return;
            }

            PersistSnapshot(snapshot);
        }

        public static void SaveConstructions(
            string realmId,
            string characterId,
            IEnumerable<ConstructionInstance.SerializableConstructionState> constructions)
        {
            if (constructions == null)
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(realmId) || string.IsNullOrWhiteSpace(characterId))
            {
                Debug.LogWarning("BuildStateRepository.SaveConstructions called without an active realm or character.");
                return;
            }

            var snapshot = GetOrCreateSnapshot(realmId, characterId);
            snapshot.constructions = CopyConstructions(constructions);
            snapshot.updatedAt = DateTime.UtcNow.ToString("O");

            if (_client == null)
            {
                Cache[BuildKey(realmId, characterId)] = snapshot;
                Debug.LogWarning("BuildStateRepository has no client configured; construction updates are cached locally.");
                return;
            }

            PersistSnapshot(snapshot);
        }

        public static void RequestLatest(string realmId, string characterId)
        {
            if (_client == null || string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(realmId))
            {
                return;
            }

            var key = BuildKey(realmId, characterId);
            _activeKey = key;
            ProgressionCoroutineRunner.Run(
                _client.GetBuildState(
                    characterId,
                    realmId,
                    snapshot =>
                    {
                        if (snapshot == null)
                        {
                            return;
                        }

                        Cache[key] = NormalizeSnapshot(snapshot, realmId, characterId);
                        BuildStateUpdated?.Invoke(realmId, characterId);
                    },
                    error =>
                    {
                        if (error != null)
                        {
                            Debug.LogWarning($"Build state sync failed: {error.Message}");
                        }
                    }
                )
            );
        }

        private static void PersistSnapshot(BuildStateSnapshot snapshot)
        {
            if (snapshot == null || _client == null)
            {
                return;
            }

            var realmId = snapshot.realmId;
            var characterId = snapshot.characterId;
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return;
            }

            var payload = new BuildStateUpdateRequest
            {
                plots = snapshot.plots ?? Array.Empty<BuildPlotDefinition>(),
                constructions = snapshot.constructions ?? Array.Empty<ConstructionInstance.SerializableConstructionState>()
            };

            ProgressionCoroutineRunner.Run(
                _client.ReplaceBuildState(
                    characterId,
                    realmId,
                    payload,
                    updated =>
                    {
                        if (updated != null)
                        {
                            Cache[BuildKey(realmId, characterId)] = NormalizeSnapshot(updated, realmId, characterId);
                            BuildStateUpdated?.Invoke(realmId, characterId);
                        }
                    },
                    error =>
                    {
                        if (error != null)
                        {
                            Debug.LogWarning($"Build state save failed: {error.Message}");
                        }
                    }
                )
            );
        }

        private static BuildStateSnapshot GetSnapshot(string realmId, string characterId)
        {
            if (string.IsNullOrWhiteSpace(realmId) || string.IsNullOrWhiteSpace(characterId))
            {
                return null;
            }

            var key = BuildKey(realmId, characterId);
            if (Cache.TryGetValue(key, out var snapshot))
            {
                return snapshot;
            }

            if (_client != null && _activeKey != key)
            {
                RequestLatest(realmId, characterId);
            }

            return null;
        }

        private static BuildStateSnapshot GetOrCreateSnapshot(string realmId, string characterId)
        {
            var key = BuildKey(realmId, characterId);
            if (!Cache.TryGetValue(key, out var snapshot) || snapshot == null)
            {
                snapshot = new BuildStateSnapshot
                {
                    id = string.Empty,
                    realmId = realmId,
                    characterId = characterId,
                    plots = Array.Empty<BuildPlotDefinition>(),
                    constructions = Array.Empty<ConstructionInstance.SerializableConstructionState>(),
                    updatedAt = DateTime.UtcNow.ToString("O")
                };
                Cache[key] = snapshot;
            }

            return snapshot;
        }

        private static BuildPlotDefinition[] CopyPlots(IEnumerable<BuildPlotDefinition> plots)
        {
            var list = new List<BuildPlotDefinition>();
            foreach (var plot in plots)
            {
                if (plot != null)
                {
                    list.Add(new BuildPlotDefinition(plot));
                }
            }

            return list.ToArray();
        }

        private static ConstructionInstance.SerializableConstructionState[] CopyConstructions(
            IEnumerable<ConstructionInstance.SerializableConstructionState> constructions)
        {
            var list = new List<ConstructionInstance.SerializableConstructionState>();
            foreach (var construction in constructions)
            {
                list.Add(construction);
            }

            return list.ToArray();
        }

        private static string BuildKey(string realmId, string characterId)
        {
            return $"{realmId}::{characterId}";
        }

        private static BuildStateSnapshot NormalizeSnapshot(
            BuildStateSnapshot snapshot,
            string realmId,
            string characterId)
        {
            snapshot.realmId = string.IsNullOrWhiteSpace(snapshot.realmId) ? realmId : snapshot.realmId;
            snapshot.characterId = string.IsNullOrWhiteSpace(snapshot.characterId) ? characterId : snapshot.characterId;
            snapshot.plots ??= Array.Empty<BuildPlotDefinition>();
            snapshot.constructions ??= Array.Empty<ConstructionInstance.SerializableConstructionState>();
            return snapshot;
        }

        private static void HandleCharacterChanged(string characterId)
        {
            _activeKey = null;
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return;
            }

            var realmId = SessionManager.SelectedRealmId;
            if (string.IsNullOrWhiteSpace(realmId))
            {
                return;
            }

            RequestLatest(realmId, characterId);
        }

        private static void HandleSessionCleared()
        {
            Cache.Clear();
            _activeKey = null;
        }
    }
}
