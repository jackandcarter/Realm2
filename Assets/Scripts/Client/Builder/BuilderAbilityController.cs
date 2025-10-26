using System;
using System.Collections.Generic;
using Building;
using Client.Player;
using UnityEngine;

namespace Client.Builder
{
    [DisallowMultipleComponent]
    public class BuilderAbilityController : MonoBehaviour
    {
        [SerializeField] private BuilderAbilitySet abilitySet;
        [SerializeField] private BlueprintSpawner blueprintSpawner;
        [SerializeField] private FloatModeController floatModeController;

        private readonly Dictionary<string, AbilityRuntimeState> _states =
            new Dictionary<string, AbilityRuntimeState>(StringComparer.OrdinalIgnoreCase);

        private ConstructionInstance _selectedInstance;

        public event Action<BuilderAbilityRuntimeStatus> AbilityStatusChanged;
        public event Action<ConstructionInstance> SelectionChanged;

        public BuilderAbilitySet AbilitySet
        {
            get => abilitySet;
            set
            {
                abilitySet = value;
                RebuildStateCache();
            }
        }

        private void Awake()
        {
            if (blueprintSpawner == null)
            {
                blueprintSpawner = FindObjectOfType<BlueprintSpawner>();
            }

            if (floatModeController == null)
            {
                floatModeController = FindObjectOfType<FloatModeController>();
            }

            RebuildStateCache();
            WireFloatModeEvents();
        }

        private void OnEnable()
        {
            WireFloatModeEvents();
        }

        private void OnDisable()
        {
            if (floatModeController != null)
            {
                floatModeController.FloatModeStarted -= OnFloatModeStarted;
                floatModeController.FloatModeEnded -= OnFloatModeEnded;
                floatModeController.PlacementFinalized -= OnPlacementFinalized;
            }
        }

        private void Update()
        {
            var now = Time.unscaledTime;
            foreach (var pair in _states)
            {
                var state = pair.Value;
                var wasReady = state.IsReady;
                var ready = now >= state.NextReadyTime;
                if (wasReady == ready)
                {
                    continue;
                }

                state.IsReady = ready;
                _states[pair.Key] = state;
                RaiseStatusChanged(pair.Key, state);
            }
        }

        public bool TryActivate(string abilityId)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                return false;
            }

            var normalizedId = abilityId.Trim();

            if (!EnsureAbilityState(normalizedId, out var state))
            {
                return false;
            }

            if (!state.IsReady)
            {
                return false;
            }

            if (state.Definition.RequiresBuilderClass && !PlayerClassStateManager.IsArkitectAvailable)
            {
                return false;
            }

            var activated = ExecuteAbility(state.Definition);
            if (!activated)
            {
                return false;
            }

            state.NextReadyTime = Time.unscaledTime + state.Definition.CooldownSeconds;
            state.IsReady = state.Definition.CooldownSeconds <= 0f;
            _states[normalizedId] = state;
            RaiseStatusChanged(normalizedId, state);
            return true;
        }

        public ConstructionInstance GetSelectedInstance()
        {
            if (_selectedInstance == null)
            {
                _selectedInstance = floatModeController != null ? floatModeController.ActiveInstance : null;
            }

            return _selectedInstance;
        }

        private bool ExecuteAbility(BuilderAbilityDefinition definition)
        {
            switch (definition.AbilityKind)
            {
                case BuilderAbilityKind.SpawnBlueprint:
                    return TrySpawnBlueprint();
                case BuilderAbilityKind.FloatSelection:
                    return TryBeginFloat();
                case BuilderAbilityKind.PlaceSelection:
                    return TryFinalizePlacement();
                default:
                    Debug.LogWarning($"Unhandled builder ability kind: {definition.AbilityKind}");
                    return false;
            }
        }

        private bool TrySpawnBlueprint()
        {
            if (blueprintSpawner == null)
            {
                Debug.LogWarning("BuilderAbilityController has no BlueprintSpawner reference.");
                return false;
            }

            if (!blueprintSpawner.TrySpawnDefault(out var instance))
            {
                return false;
            }

            _selectedInstance = instance;
            SelectionChanged?.Invoke(instance);
            if (floatModeController != null)
            {
                floatModeController.BeginFloat(instance);
            }

            return true;
        }

        private bool TryBeginFloat()
        {
            var instance = GetSelectedInstance();
            if (instance == null)
            {
                return false;
            }

            if (floatModeController == null)
            {
                Debug.LogWarning("BuilderAbilityController is missing a FloatModeController reference.");
                return false;
            }

            floatModeController.BeginFloat(instance);
            return true;
        }

        private bool TryFinalizePlacement()
        {
            if (floatModeController == null)
            {
                Debug.LogWarning("BuilderAbilityController is missing a FloatModeController reference.");
                return false;
            }

            if (!floatModeController.IsFloating)
            {
                return false;
            }

            floatModeController.FinalizePlacement();
            return true;
        }

        private bool EnsureAbilityState(string abilityId, out AbilityRuntimeState state)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                state = default;
                return false;
            }

            var normalized = abilityId.Trim();
            if (_states.TryGetValue(normalized, out state))
            {
                return true;
            }

            if (abilitySet == null || !abilitySet.TryGetAbility(normalized, out var definition))
            {
                return false;
            }

            state = new AbilityRuntimeState(definition);
            _states[normalized] = state;
            return true;
        }

        private void RebuildStateCache()
        {
            _states.Clear();
            if (abilitySet == null)
            {
                return;
            }

            foreach (var definition in abilitySet.Abilities)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.AbilityId))
                {
                    continue;
                }

                var id = definition.AbilityId.Trim();
                if (_states.ContainsKey(id))
                {
                    continue;
                }

                _states[id] = new AbilityRuntimeState(definition);
                RaiseStatusChanged(id, _states[id]);
            }
        }

        private void WireFloatModeEvents()
        {
            if (floatModeController == null)
            {
                return;
            }

            floatModeController.FloatModeStarted -= OnFloatModeStarted;
            floatModeController.FloatModeEnded -= OnFloatModeEnded;
            floatModeController.PlacementFinalized -= OnPlacementFinalized;

            floatModeController.FloatModeStarted += OnFloatModeStarted;
            floatModeController.FloatModeEnded += OnFloatModeEnded;
            floatModeController.PlacementFinalized += OnPlacementFinalized;
        }

        private void OnFloatModeStarted(ConstructionInstance instance)
        {
            if (instance == null)
            {
                return;
            }

            _selectedInstance = instance;
            SelectionChanged?.Invoke(instance);
        }

        private void OnFloatModeEnded(ConstructionInstance instance)
        {
            if (instance == null)
            {
                return;
            }

            _selectedInstance = instance;
            SelectionChanged?.Invoke(instance);
        }

        private void OnPlacementFinalized(ConstructionInstance instance)
        {
            if (instance == null)
            {
                return;
            }

            _selectedInstance = instance;
            SelectionChanged?.Invoke(instance);
        }

        private void RaiseStatusChanged(string abilityId, AbilityRuntimeState runtimeState)
        {
            AbilityStatusChanged?.Invoke(new BuilderAbilityRuntimeStatus(
                abilityId,
                runtimeState.Definition,
                runtimeState.IsReady,
                Mathf.Max(0f, runtimeState.NextReadyTime - Time.unscaledTime)));
        }

        private readonly struct AbilityRuntimeState
        {
            public readonly BuilderAbilityDefinition Definition;
            public readonly float Cooldown;
            public readonly bool HasCooldown;
            public bool IsReady;
            public float NextReadyTime;

            public AbilityRuntimeState(BuilderAbilityDefinition definition)
            {
                Definition = definition;
                Cooldown = definition?.CooldownSeconds ?? 0f;
                HasCooldown = Cooldown > 0f;
                IsReady = true;
                NextReadyTime = 0f;
            }
        }
    }

    public readonly struct BuilderAbilityRuntimeStatus
    {
        public readonly string AbilityId;
        public readonly BuilderAbilityDefinition Definition;
        public readonly bool IsReady;
        public readonly float CooldownRemaining;

        public BuilderAbilityRuntimeStatus(string abilityId, BuilderAbilityDefinition definition, bool isReady, float cooldownRemaining)
        {
            AbilityId = abilityId;
            Definition = definition;
            IsReady = isReady;
            CooldownRemaining = cooldownRemaining;
        }
    }
}
