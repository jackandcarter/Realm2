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

        [Header("Defaults")]
        [SerializeField] private AbilityTargetingConfig defaultTargeting = new AbilityTargetingConfig();
        [SerializeField] private List<AbilityEffectScalingRule> scalingRules = new();

        [Header("Hit Detection")]
        [SerializeField] private bool usePhysicsHitDetection = true;
        [SerializeField] private LayerMask hitboxMask = ~0;
        [SerializeField] private QueryTriggerInteraction hitboxTriggerInteraction = QueryTriggerInteraction.Collide;

        [Header("Server Authority")]
        [SerializeField] private bool deferImpactUntilConfirmed = true;
        [SerializeField] private bool applyPredictedEffects;

        private readonly Dictionary<string, PendingCombatAbility> _pendingAbilities = new();

        public event Action<CombatAbilityRequest, IReadOnlyList<CombatEffectResult>> AbilityPredicted;
        public event Action<CombatAbilityConfirmation, IReadOnlyList<CombatEffectResult>> AbilityConfirmed;

        private void Awake()
        {
            ResolveReferences();
            EnsureDefaultScalingRules();
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
            EnsureDefaultScalingRules();
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
            var targeting = ability != null ? ability.Targeting : defaultTargeting;
            var primaryTarget = targetSelection != null ? targetSelection.PrimaryTarget : null;
            var groundPoint = targetSelection != null && targetSelection.HasGroundTarget
                ? targetSelection.GroundTargetPoint
                : (Vector3?)null;

            var targets = CombatTargetResolver.ResolveTargets(
                targeting,
                caster,
                primaryTarget,
                groundPoint,
                CombatEntityRegistry.All);

            var hitbox = ability != null ? ability.Hitbox : resolution.Hitbox;
            var usePhysicsQuery = usePhysicsHitDetection || (hitbox != null && hitbox.RequiresContact);
            var resolvedTargets = CombatHitboxResolver.ResolveHitTargets(
                hitbox,
                caster,
                targets,
                usePhysicsQuery,
                hitboxMask,
                hitboxTriggerInteraction);

            var predictedResults = BuildEffectResults(ability, resolution.TotalDamage, resolvedTargets);
            var requestId = Guid.NewGuid().ToString("N");
            var request = SendAbilityRequest(requestId, ability, resolvedTargets, groundPoint);

            if (applyPredictedEffects)
            {
                CombatEffectResolver.ApplyResolvedEffects(predictedResults);
            }

            _pendingAbilities[requestId] = new PendingCombatAbility
            {
                Request = request,
                Ability = ability,
                PredictedTargets = resolvedTargets,
                PredictedResults = predictedResults,
                PredictedApplied = applyPredictedEffects,
                BaseDamage = resolution.TotalDamage
            };

            AbilityPredicted?.Invoke(request, predictedResults);
        }

        private CombatAbilityRequest SendAbilityRequest(
            string requestId,
            AbilityDefinition ability,
            IEnumerable<CombatEntity> targets,
            Vector3? groundPoint)
        {
            var targetIds = targets?.Where(target => target != null)
                .Select(target => target.EntityId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList() ?? new List<string>();

            var primaryTargetId = targetSelection != null && targetSelection.PrimaryTarget != null
                ? targetSelection.PrimaryTarget.EntityId
                : targetIds.FirstOrDefault();

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

        private void EnsureDefaultScalingRules()
        {
            if (scalingRules == null)
            {
                scalingRules = new List<AbilityEffectScalingRule>();
            }

            if (scalingRules.Count > 0)
            {
                return;
            }

            scalingRules.Add(new AbilityEffectScalingRule
            {
                EffectType = AbilityEffectType.Damage,
                StatId = "stat.attackPower",
                Ratio = 0.5f
            });

            scalingRules.Add(new AbilityEffectScalingRule
            {
                EffectType = AbilityEffectType.Heal,
                StatId = "stat.magic",
                Ratio = 0.6f
            });
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

            var confirmedTargets = ResolveConfirmedTargets(confirmation, pending);
            var confirmedResults = BuildConfirmedResults(confirmation, pending, confirmedTargets);

            if (!deferImpactUntilConfirmed && pending.PredictedApplied)
            {
                AbilityConfirmed?.Invoke(confirmation, confirmedResults);
                return;
            }

            if (pending.PredictedApplied)
            {
                var reconciledResults = ReconcilePredictedResults(pending, confirmedResults);
                CombatEffectResolver.ApplyResolvedEffects(reconciledResults);
                AbilityConfirmed?.Invoke(confirmation, reconciledResults);
                return;
            }

            CombatEffectResolver.ApplyResolvedEffects(confirmedResults);
            AbilityConfirmed?.Invoke(confirmation, confirmedResults);
        }

        private List<CombatEntity> ResolveConfirmedTargets(
            CombatAbilityConfirmation confirmation,
            PendingCombatAbility pending)
        {
            if (confirmation.events != null && confirmation.events.Count > 0)
            {
                return ResolveTargetsFromEvents(confirmation.events);
            }

            if (confirmation.targetIds != null && confirmation.targetIds.Count > 0)
            {
                var targets = new List<CombatEntity>();
                foreach (var targetId in confirmation.targetIds)
                {
                    if (string.IsNullOrWhiteSpace(targetId))
                    {
                        continue;
                    }

                    var entity = CombatEntityRegistry.All.FirstOrDefault(candidate =>
                        candidate != null && string.Equals(candidate.EntityId, targetId, StringComparison.OrdinalIgnoreCase));
                    if (entity != null)
                    {
                        targets.Add(entity);
                    }
                }

                return targets;
            }

            return pending.PredictedTargets ?? new List<CombatEntity>();
        }

        private List<CombatEffectResult> BuildConfirmedResults(
            CombatAbilityConfirmation confirmation,
            PendingCombatAbility pending,
            IReadOnlyList<CombatEntity> confirmedTargets)
        {
            if (confirmation.events != null && confirmation.events.Count > 0)
            {
                return BuildResultsFromEvents(confirmation.events);
            }

            return BuildEffectResults(pending.Ability, pending.BaseDamage, confirmedTargets);
        }

        private List<CombatEffectResult> BuildEffectResults(
            AbilityDefinition ability,
            float baseDamage,
            IReadOnlyList<CombatEntity> targets)
        {
            if (ability != null && ability.Effects != null && ability.Effects.Count > 0)
            {
                return CombatEffectResolver.ResolveEffects(caster, targets, ability.Effects, scalingRules);
            }

            return BuildBasicDamageResults(baseDamage, targets);
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

        private List<CombatEffectResult> ReconcilePredictedResults(
            PendingCombatAbility pending,
            IReadOnlyList<CombatEffectResult> confirmedResults)
        {
            var reconciled = new List<CombatEffectResult>();
            if (confirmedResults == null)
            {
                return reconciled;
            }

            var predictedLookup = new Dictionary<(string, AbilityEffectType, string), CombatEffectResult>();
            if (pending.PredictedResults != null)
            {
                foreach (var result in pending.PredictedResults)
                {
                    if (result.Target == null)
                    {
                        continue;
                    }

                    var key = (result.Target.EntityId, result.EffectType, result.StateName ?? string.Empty);
                    predictedLookup[key] = result;
                }
            }

            foreach (var confirmed in confirmedResults)
            {
                if (confirmed.Target == null)
                {
                    continue;
                }

                var key = (confirmed.Target.EntityId, confirmed.EffectType, confirmed.StateName ?? string.Empty);
                if (!predictedLookup.TryGetValue(key, out var predicted))
                {
                    reconciled.Add(confirmed);
                    continue;
                }

                if (confirmed.EffectType == AbilityEffectType.Damage || confirmed.EffectType == AbilityEffectType.Heal)
                {
                    var delta = confirmed.Amount - predicted.Amount;
                    if (Mathf.Abs(delta) > 0.01f)
                    {
                        reconciled.Add(new CombatEffectResult
                        {
                            EffectType = delta >= 0f
                                ? confirmed.EffectType
                                : confirmed.EffectType == AbilityEffectType.Damage
                                    ? AbilityEffectType.Heal
                                    : AbilityEffectType.Damage,
                            Target = confirmed.Target,
                            Amount = Mathf.Abs(delta),
                            StateName = confirmed.StateName,
                            DurationSeconds = confirmed.DurationSeconds,
                            CustomSummary = confirmed.CustomSummary
                        });
                    }

                    continue;
                }
            }

            return reconciled;
        }

        private static List<CombatEntity> ResolveTargetsFromEvents(IEnumerable<CombatAbilityEvent> events)
        {
            var targets = new List<CombatEntity>();
            if (events == null)
            {
                return targets;
            }

            foreach (var serverEvent in events)
            {
                if (string.IsNullOrWhiteSpace(serverEvent.targetId))
                {
                    continue;
                }

                var entity = CombatEntityRegistry.All.FirstOrDefault(candidate =>
                    candidate != null && string.Equals(candidate.EntityId, serverEvent.targetId, StringComparison.OrdinalIgnoreCase));
                if (entity != null)
                {
                    targets.Add(entity);
                }
            }

            return targets;
        }

        private static List<CombatEffectResult> BuildBasicDamageResults(
            float damage,
            IReadOnlyList<CombatEntity> targets)
        {
            var results = new List<CombatEffectResult>();
            if (targets == null)
            {
                return results;
            }

            foreach (var target in targets)
            {
                if (target == null)
                {
                    continue;
                }

                results.Add(new CombatEffectResult
                {
                    EffectType = AbilityEffectType.Damage,
                    Target = target,
                    Amount = damage
                });
            }

            return results;
        }

        private sealed class PendingCombatAbility
        {
            public CombatAbilityRequest Request;
            public AbilityDefinition Ability;
            public List<CombatEntity> PredictedTargets;
            public List<CombatEffectResult> PredictedResults;
            public bool PredictedApplied;
            public float BaseDamage;
        }
    }
}
