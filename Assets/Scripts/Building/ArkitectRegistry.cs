using System;
using System.Collections.Generic;
using Client;
using Client.Save;
using UnityEngine;

namespace Building
{
    /// <summary>
    /// Central registry that keeps track of Arkitect UI panels and construction instances.
    /// The registry is used to prevent duplicate runtime instantiation and to reconcile
    /// saved state with the currently loaded scene.
    /// </summary>
    public static class ArkitectRegistry
    {
        private static readonly Dictionary<string, WeakReference<GameObject>> UiPanels =
            new Dictionary<string, WeakReference<GameObject>>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, ConstructionInstance> ConstructionInstances =
            new Dictionary<string, ConstructionInstance>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, GameObject> BlueprintPrefabs =
            new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

        private static readonly List<ConstructionInstance.SerializableConstructionState> PendingStates =
            new List<ConstructionInstance.SerializableConstructionState>();

        private static readonly object SyncRoot = new object();

        private static bool _initialized;
        private static bool _isReconciling;

        /// <summary>
        /// Registers a UI panel under a stable identifier.
        /// </summary>
        public static void RegisterUiPanel(string panelId, GameObject panel)
        {
            if (string.IsNullOrWhiteSpace(panelId) || panel == null)
            {
                return;
            }

            EnsureInitialized();

            lock (SyncRoot)
            {
                UiPanels[panelId.Trim()] = new WeakReference<GameObject>(panel);
            }
        }

        /// <summary>
        /// Attempts to retrieve a previously registered UI panel.
        /// </summary>
        public static bool TryGetUiPanel(string panelId, out GameObject panel)
        {
            panel = null;
            if (string.IsNullOrWhiteSpace(panelId))
            {
                return false;
            }

            EnsureInitialized();

            lock (SyncRoot)
            {
                if (!UiPanels.TryGetValue(panelId.Trim(), out var weakReference))
                {
                    return false;
                }

                if (weakReference.TryGetTarget(out panel) && panel != null)
                {
                    return true;
                }

                UiPanels.Remove(panelId.Trim());
                panel = null;
                return false;
            }
        }

        /// <summary>
        /// Removes a UI panel from the registry if the provided instance matches.
        /// </summary>
        public static void UnregisterUiPanel(string panelId, GameObject panel)
        {
            if (string.IsNullOrWhiteSpace(panelId))
            {
                return;
            }

            EnsureInitialized();

            lock (SyncRoot)
            {
                var key = panelId.Trim();
                if (!UiPanels.TryGetValue(key, out var weakReference))
                {
                    return;
                }

                if (panel == null || !weakReference.TryGetTarget(out var tracked) || tracked == panel)
                {
                    UiPanels.Remove(key);
                }
            }
        }

        /// <summary>
        /// Registers the supplied construction instance and associates it with its instance identifier.
        /// </summary>
        public static void RegisterConstructionInstance(ConstructionInstance instance)
        {
            if (instance == null)
            {
                return;
            }

            EnsureInitialized();
            instance.EnsureInstanceId();

            lock (SyncRoot)
            {
                if (string.IsNullOrWhiteSpace(instance.InstanceId))
                {
                    return;
                }

                ConstructionInstances[instance.InstanceId.Trim()] = instance;
            }
        }

        /// <summary>
        /// Unregisters a construction instance from the registry.
        /// </summary>
        public static void UnregisterConstructionInstance(ConstructionInstance instance)
        {
            if (instance == null || string.IsNullOrWhiteSpace(instance.InstanceId))
            {
                return;
            }

            EnsureInitialized();

            lock (SyncRoot)
            {
                var key = instance.InstanceId.Trim();
                if (ConstructionInstances.TryGetValue(key, out var tracked) && tracked == instance)
                {
                    ConstructionInstances.Remove(key);
                }
            }
        }

        /// <summary>
        /// Attempts to locate a tracked construction instance.
        /// </summary>
        public static bool TryGetConstructionInstance(string instanceId, out ConstructionInstance instance)
        {
            instance = null;
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return false;
            }

            EnsureInitialized();

            lock (SyncRoot)
            {
                if (!ConstructionInstances.TryGetValue(instanceId.Trim(), out instance) || instance == null)
                {
                    ConstructionInstances.Remove(instanceId.Trim());
                    instance = null;
                    return false;
                }
            }

            return instance != null;
        }

        /// <summary>
        /// Registers a prefab association for a blueprint identifier.
        /// </summary>
        public static void RegisterBlueprintPrefab(string blueprintId, GameObject prefab)
        {
            if (string.IsNullOrWhiteSpace(blueprintId) || prefab == null)
            {
                return;
            }

            EnsureInitialized();

            lock (SyncRoot)
            {
                BlueprintPrefabs[blueprintId.Trim()] = prefab;
            }

            TryResolvePendingStates();
        }

        /// <summary>
        /// Reconciles construction instances with the saved state for the active session.
        /// </summary>
        public static void ReconcileSavedConstructions()
        {
            EnsureInitialized();

            lock (SyncRoot)
            {
                if (_isReconciling)
                {
                    return;
                }

                _isReconciling = true;
            }

            try
            {
                var states = ConstructionSaveSystem.LoadInstances();
                if (states == null || states.Count == 0)
                {
                    return;
                }

                foreach (var state in states)
                {
                    ProcessState(state);
                }
            }
            finally
            {
                lock (SyncRoot)
                {
                    _isReconciling = false;
                }
            }
        }

        private static void ProcessState(ConstructionInstance.SerializableConstructionState state)
        {
            if (string.IsNullOrWhiteSpace(state.InstanceId))
            {
                return;
            }

            var normalizedId = state.InstanceId.Trim();
            ConstructionInstance instance = null;

            lock (SyncRoot)
            {
                if (ConstructionInstances.TryGetValue(normalizedId, out var tracked) && tracked != null)
                {
                    instance = tracked;
                }
            }

            if (instance == null)
            {
                instance = FindSceneInstance(normalizedId);
            }

            if (instance == null)
            {
                instance = SpawnInstanceFromState(state);
                if (instance == null)
                {
                    lock (SyncRoot)
                    {
                        PendingStates.Add(state);
                    }

                    return;
                }
            }

            instance.Initialize(state.BlueprintId, state.IsPlaced);
            instance.ApplyState(state);
            RegisterConstructionInstance(instance);
            if (state.IsPlaced)
            {
                instance.Persist();
            }
        }

        private static ConstructionInstance FindSceneInstance(string instanceId)
        {
            var candidates = UnityEngine.Object.FindObjectsByType<ConstructionInstance>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var candidate in candidates)
            {
                if (candidate == null)
                {
                    continue;
                }

                candidate.EnsureInstanceId();
                if (string.Equals(candidate.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static ConstructionInstance SpawnInstanceFromState(ConstructionInstance.SerializableConstructionState state)
        {
            if (string.IsNullOrWhiteSpace(state.BlueprintId))
            {
                return null;
            }

            GameObject prefab;
            lock (SyncRoot)
            {
                BlueprintPrefabs.TryGetValue(state.BlueprintId.Trim(), out prefab);
            }

            if (prefab == null)
            {
                return null;
            }

            var go = UnityEngine.Object.Instantiate(prefab, state.Position, state.Rotation);
            go.transform.localScale = state.Scale;

            var instance = go.GetComponent<ConstructionInstance>();
            if (instance == null)
            {
                instance = go.AddComponent<ConstructionInstance>();
            }

            return instance;
        }

        private static void TryResolvePendingStates()
        {
            lock (SyncRoot)
            {
                if (PendingStates.Count == 0)
                {
                    return;
                }

                var pendingCopy = PendingStates.ToArray();
                PendingStates.Clear();

                foreach (var state in pendingCopy)
                {
                    ProcessState(state);
                }
            }
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            SessionManager.SelectedCharacterChanged += OnSelectedCharacterChanged;
            SessionManager.SessionCleared += OnSessionCleared;

            _initialized = true;

            RefreshSceneConstructionCache();
            ReconcileSavedConstructions();
        }

        private static void RefreshSceneConstructionCache()
        {
            var instances = UnityEngine.Object.FindObjectsByType<ConstructionInstance>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var instance in instances)
            {
                RegisterConstructionInstance(instance);
            }
        }

        private static void OnSelectedCharacterChanged(string _)
        {
            ReconcileSavedConstructions();
        }

        private static void OnSessionCleared()
        {
            lock (SyncRoot)
            {
                UiPanels.Clear();
                ConstructionInstances.Clear();
                PendingStates.Clear();
            }
        }

#if UNITY_EDITOR
        public static void ResetForTests()
        {
            lock (SyncRoot)
            {
                UiPanels.Clear();
                ConstructionInstances.Clear();
                BlueprintPrefabs.Clear();
                PendingStates.Clear();
                _initialized = false;
                _isReconciling = false;
            }

            SessionManager.SelectedCharacterChanged -= OnSelectedCharacterChanged;
            SessionManager.SessionCleared -= OnSessionCleared;
        }
#endif
    }
}
