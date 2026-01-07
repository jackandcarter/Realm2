using System;
using System.Collections.Generic;
using Client.Builder;
using UnityEngine;

namespace Client.UI.HUD.Dock
{
    [DisallowMultipleComponent]
    public class BuilderAbilityDockStateSource : MonoBehaviour, IDockAbilityStateSource
    {
        [SerializeField] private BuilderAbilityController abilityController;
        [SerializeField] private List<AbilityCastOverride> castOverrides = new();

        private readonly Dictionary<string, DockAbilityState> _states =
            new Dictionary<string, DockAbilityState>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, float> _castLookup =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        public event Action<string, DockAbilityState> AbilityStateChanged;

        private void Awake()
        {
            if (abilityController == null)
            {
#if UNITY_2023_1_OR_NEWER
                abilityController = UnityEngine.Object.FindFirstObjectByType<BuilderAbilityController>(FindObjectsInactive.Include);
#else
                abilityController = FindObjectOfType<BuilderAbilityController>(true);
#endif
            }

            BuildCastLookup();
        }

        private void OnValidate()
        {
            BuildCastLookup();
        }

        private void OnEnable()
        {
            if (abilityController != null)
            {
                abilityController.AbilityStatusChanged += OnAbilityStatusChanged;
            }
        }

        private void OnDisable()
        {
            if (abilityController != null)
            {
                abilityController.AbilityStatusChanged -= OnAbilityStatusChanged;
            }
        }

        public bool TryGetState(string abilityId, out DockAbilityState state)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                state = default;
                return false;
            }

            return _states.TryGetValue(abilityId.Trim(), out state);
        }

        public void TriggerCast(string abilityId, float castDuration)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                return;
            }

            var key = abilityId.Trim();
            var current = _states.TryGetValue(key, out var existing) ? existing : default;
            var next = new DockAbilityState(
                castDuration,
                castDuration,
                current.CooldownDuration,
                current.CooldownRemaining);
            _states[key] = next;
            AbilityStateChanged?.Invoke(key, next);
        }

        private void OnAbilityStatusChanged(BuilderAbilityRuntimeStatus status)
        {
            if (string.IsNullOrWhiteSpace(status.AbilityId))
            {
                return;
            }

            var abilityId = status.AbilityId.Trim();
            var cooldownDuration = status.Definition != null ? status.Definition.CooldownSeconds : 0f;
            var castDuration = ResolveCastDuration(abilityId);
            var state = new DockAbilityState(
                castDuration,
                0f,
                cooldownDuration,
                status.CooldownRemaining);
            _states[abilityId] = state;
            AbilityStateChanged?.Invoke(abilityId, state);
        }

        private void BuildCastLookup()
        {
            _castLookup.Clear();
            if (castOverrides == null)
            {
                return;
            }

            foreach (var entry in castOverrides)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.AbilityId))
                {
                    continue;
                }

                var key = entry.AbilityId.Trim();
                if (!_castLookup.ContainsKey(key))
                {
                    _castLookup[key] = Mathf.Max(0f, entry.CastDuration);
                }
            }
        }

        private float ResolveCastDuration(string abilityId)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                return 0f;
            }

            return _castLookup.TryGetValue(abilityId.Trim(), out var duration) ? duration : 0f;
        }

        [Serializable]
        public class AbilityCastOverride
        {
            public string AbilityId;
            public float CastDuration;
        }
    }
}
