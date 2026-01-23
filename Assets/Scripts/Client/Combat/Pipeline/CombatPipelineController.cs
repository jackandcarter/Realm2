using System;
using System.Collections.Generic;
using System.Linq;
using Client.Combat;
using Realm.Abilities;
using UnityEngine;

namespace Client.Combat.Pipeline
{
    [DisallowMultipleComponent]
    public class CombatPipelineController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CombatManager combatManager;
        [SerializeField] private CombatEntity caster;
        [SerializeField] private CombatTargetSelection targetSelection;
        [SerializeField] private CombatServerBridge serverBridge;

        private readonly Dictionary<string, PendingCombatAbility> _pendingAbilities = new();

        public event Action<CombatAbilityRequest, IReadOnlyList<CombatEffectResult>> AbilityPredicted;
        public event Action<CombatAbilityConfirmation, IReadOnlyList<CombatEffectResult>> AbilityConfirmed;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            BindCombatManager();
            BindServerBridge();
        }

        private void OnDisable()
        {
            UnbindCombatManager();
            UnbindServerBridge();
        }

        private void OnValidate()
        {
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

            if (caster == null)
            {
#if UNITY_2023_1_OR_NEWER
                caster = UnityEngine.Object.FindFirstObjectByType<CombatEntity>(FindObjectsInactive.Include);
#else
                caster = FindObjectOfType<CombatEntity>(true);
#endif
            }

            if (targetSelection == null)
            {
#if UNITY_2023_1_OR_NEWER
                targetSelection = UnityEngine.Object.FindFirstObjectByType<CombatTargetSelection>(FindObjectsInactive.Include);
#else
                targetSelection = FindObjectOfType<CombatTargetSelection>(true);
#endif
            }

            if (serverBridge == null)
            {
#if UNITY_2023_1_OR_NEWER
                serverBridge = UnityEngine.Object.FindFirstObjectByType<CombatServerBridge>(FindObjectsInactive.Include);
#else
                serverBridge = FindObjectOfType<CombatServerBridge>(true);
#endif
            }
        }

        private void BindCombatManager()
        {
            if (combatManager == null)
            {
                return;
            }

            combatManager.AttackResolved -= HandleAttackResolved;
            combatManager.AttackResolved += HandleAttackResolved;
        }

        private void UnbindCombatManager()
        {
            if (combatManager == null)
            {
                return;
            }

            combatManager.AttackResolved -= HandleAttackResolved;
        }

        private void BindServerBridge()
        {
            if (serverBridge == null)
            {
                return;
            }

            serverBridge.AbilityConfirmed -= HandleAbilityConfirmed;
            serverBridge.AbilityConfirmed += HandleAbilityConfirmed;
        }

        private void UnbindServerBridge()
        {
            if (serverBridge == null)
            {
                return;
            }

            serverBridge.AbilityConfirmed -= HandleAbilityConfirmed;
        }

        private void HandleAttackResolved(WeaponAttackResolution resolution)
        {
            if (caster == null)
            {
                return;
            }

            var ability = resolution.SpecialAbility;
            var primaryTarget = targetSelection != null ? targetSelection.PrimaryTarget : null;
            var groundPoint = targetSelection != null && targetSelection.HasGroundTarget
                ? targetSelection.GroundTargetPoint
                : (Vector3?)null;

            var requestId = Guid.NewGuid().ToString("N");
            var request = SendAbilityRequest(requestId, ability, primaryTarget, groundPoint);

            _pendingAbilities[requestId] = new PendingCombatAbility
            {
                Request = request
            };

            AbilityPredicted?.Invoke(request, Array.Empty<CombatEffectResult>());
        }

        private CombatAbilityRequest SendAbilityRequest(
            string requestId,
            AbilityDefinition ability,
            CombatEntity primaryTarget,
            Vector3? groundPoint)
        {
            var targetIds = new List<string>();
            var primaryTargetId = primaryTarget != null ? primaryTarget.EntityId : null;
            if (!string.IsNullOrWhiteSpace(primaryTargetId))
            {
                targetIds.Add(primaryTargetId);
            }

            var request = new CombatAbilityRequest
            {
                requestId = requestId,
                abilityId = ability != null ? ability.Guid : "basic-attack",
                casterId = caster != null ? caster.EntityId : string.Empty,
                primaryTargetId = primaryTargetId,
                targetIds = targetIds,
                targetPoint = groundPoint ?? caster.Position,
                clientTime = Time.time
            };

            if (serverBridge != null)
            {
                serverBridge.RequestAbilityExecution(request);
            }

            return request;
        }

        private void HandleAbilityConfirmed(CombatAbilityConfirmation confirmation)
        {
            if (caster == null)
            {
                return;
            }

            if (!_pendingAbilities.TryGetValue(confirmation.requestId ?? string.Empty, out var pending))
            {
                return;
            }

            _pendingAbilities.Remove(confirmation.requestId ?? string.Empty);

            var confirmedResults = BuildResultsFromEvents(confirmation.events);

            if (confirmedResults.Count > 0)
            {
                CombatEffectResolver.ApplyResolvedEffects(confirmedResults);
            }

            AbilityConfirmed?.Invoke(confirmation, confirmedResults);
        }

        private List<CombatEffectResult> BuildResultsFromEvents(IReadOnlyList<CombatAbilityEvent> events)
        {
            var results = new List<CombatEffectResult>();
            if (events == null)
            {
                return results;
            }

            foreach (var serverEvent in events)
            {
                if (string.IsNullOrWhiteSpace(serverEvent.targetId))
                {
                    continue;
                }

                var target = CombatEntityRegistry.All.FirstOrDefault(entity =>
                    entity != null && string.Equals(entity.EntityId, serverEvent.targetId, StringComparison.OrdinalIgnoreCase));
                if (target == null)
                {
                    continue;
                }

                var effectType = ResolveEffectType(serverEvent.kind);
                var stateName = serverEvent.stateId;

                results.Add(new CombatEffectResult
                {
                    EffectType = effectType,
                    Target = target,
                    Amount = serverEvent.amount,
                    StateName = stateName,
                    DurationSeconds = serverEvent.durationSeconds
                });
            }

            return results;
        }

        private AbilityEffectType ResolveEffectType(string serverKind)
        {
            if (string.IsNullOrWhiteSpace(serverKind))
            {
                return AbilityEffectType.Custom;
            }

            switch (serverKind.Trim().ToLowerInvariant())
            {
                case "damage":
                    return AbilityEffectType.Damage;
                case "heal":
                    return AbilityEffectType.Heal;
                case "stateapplied":
                    return AbilityEffectType.StateChange;
                default:
                    return AbilityEffectType.Custom;
            }
        }

        private sealed class PendingCombatAbility
        {
            public CombatAbilityRequest Request;
        }
    }
}
