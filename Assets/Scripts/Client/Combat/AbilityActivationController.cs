using System;
using System.Collections.Generic;
using Client.UI.HUD.Dock;
using Realm.Abilities;
using UnityEngine;

namespace Client.Combat
{
    [DisallowMultipleComponent]
    public class AbilityActivationController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CombatManager combatManager;
        [SerializeField] private WeaponAttackController attackController;
        [SerializeField] private DockAbilityStateHub abilityStateHub;

        private readonly Dictionary<string, AbilityRuntimeState> _states =
            new Dictionary<string, AbilityRuntimeState>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DockAbilityState> _lastPublishedStates =
            new Dictionary<string, DockAbilityState>(StringComparer.OrdinalIgnoreCase);

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
        }

        private void Update()
        {
            if (_states.Count == 0 || abilityStateHub == null)
            {
                return;
            }

            var now = Time.unscaledTime;
            foreach (var pair in _states)
            {
                var abilityId = pair.Key;
                var state = pair.Value;
                var castRemaining = Mathf.Max(0f, state.CastEndTime - now);
                var cooldownRemaining = Mathf.Max(0f, state.CooldownEndTime - now);
                var dockState = new DockAbilityState(
                    state.CastDuration,
                    castRemaining,
                    state.CooldownDuration,
                    cooldownRemaining);

                if (_lastPublishedStates.TryGetValue(abilityId, out var previous) &&
                    ApproximatelyEqual(previous, dockState))
                {
                    continue;
                }

                _lastPublishedStates[abilityId] = dockState;
                abilityStateHub.SetAbilityState(abilityId, dockState);
            }
        }

        public bool TryActivate(string abilityId)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                return false;
            }

            var normalized = abilityId.Trim();
            if (!TryResolveAbility(normalized, out var definition))
            {
                Debug.LogWarning($"AbilityActivationController could not resolve ability '{normalized}'.", this);
                return false;
            }

            var now = Time.unscaledTime;
            if (_states.TryGetValue(normalized, out var runtime) &&
                (runtime.CastEndTime > now || runtime.CooldownEndTime > now))
            {
                return false;
            }

            if (!RequestExecution(normalized))
            {
                return false;
            }

            var cooldownDuration = definition.Resource != null ? definition.Resource.CooldownSeconds : 0f;
            var castDuration = definition.Resource != null ? definition.Resource.CastSeconds : 0f;
            _states[normalized] = new AbilityRuntimeState(
                Mathf.Max(0f, castDuration),
                now + Mathf.Max(0f, castDuration),
                Mathf.Max(0f, cooldownDuration),
                now + Mathf.Max(0f, cooldownDuration));

            PublishImmediateState(normalized, _states[normalized]);
            return true;
        }

        private void ResolveReferences()
        {
            if (combatManager == null)
            {
#if UNITY_2023_1_OR_NEWER
                combatManager = UnityEngine.Object.FindFirstObjectByType<CombatManager>(FindObjectsInactive.Include);
#else
                combatManager = FindObjectOfType<CombatManager>(true);
#endif
            }

            if (attackController == null)
            {
#if UNITY_2023_1_OR_NEWER
                attackController = UnityEngine.Object.FindFirstObjectByType<WeaponAttackController>(FindObjectsInactive.Include);
#else
                attackController = FindObjectOfType<WeaponAttackController>(true);
#endif
            }

            if (abilityStateHub == null)
            {
#if UNITY_2023_1_OR_NEWER
                abilityStateHub = UnityEngine.Object.FindFirstObjectByType<DockAbilityStateHub>(FindObjectsInactive.Include);
#else
                abilityStateHub = FindObjectOfType<DockAbilityStateHub>(true);
#endif
            }
        }

        private bool TryResolveAbility(string abilityId, out AbilityDefinition definition)
        {
            if (combatManager != null && combatManager.TryGetAbilityDefinition(abilityId, out definition))
            {
                return true;
            }

            return AbilityRegistry.TryGetAbility(abilityId, out definition);
        }

        private bool RequestExecution(string abilityId)
        {
            if (attackController == null)
            {
                Debug.LogWarning($"AbilityActivationController has no WeaponAttackController for '{abilityId}'.", this);
                return false;
            }

            attackController.HandleSpecial(abilityId);
            return true;
        }

        private void PublishImmediateState(string abilityId, AbilityRuntimeState runtime)
        {
            if (abilityStateHub == null)
            {
                return;
            }

            var dockState = new DockAbilityState(
                runtime.CastDuration,
                runtime.CastDuration,
                runtime.CooldownDuration,
                runtime.CooldownDuration);
            _lastPublishedStates[abilityId] = dockState;
            abilityStateHub.SetAbilityState(abilityId, dockState);
        }

        private static bool ApproximatelyEqual(DockAbilityState left, DockAbilityState right)
        {
            return Mathf.Approximately(left.CastDuration, right.CastDuration) &&
                   Mathf.Approximately(left.CastRemaining, right.CastRemaining) &&
                   Mathf.Approximately(left.CooldownDuration, right.CooldownDuration) &&
                   Mathf.Approximately(left.CooldownRemaining, right.CooldownRemaining);
        }

        private readonly struct AbilityRuntimeState
        {
            public readonly float CastDuration;
            public readonly float CastEndTime;
            public readonly float CooldownDuration;
            public readonly float CooldownEndTime;

            public AbilityRuntimeState(float castDuration, float castEndTime, float cooldownDuration, float cooldownEndTime)
            {
                CastDuration = castDuration;
                CastEndTime = castEndTime;
                CooldownDuration = cooldownDuration;
                CooldownEndTime = cooldownEndTime;
            }
        }
    }
}
