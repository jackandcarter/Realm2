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

        private void Awake()
        {
            ResolveReferences();
            EnsureDefaultScalingRules();
        }

        private void OnEnable()
        {
            ResolveReferences();
            BindCombatManager();
        }

        private void OnDisable()
        {
            UnbindCombatManager();
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

            var hitTargets = CombatHitboxResolver.FilterTargets(
                ability != null ? ability.Hitbox : resolution.Hitbox,
                caster,
                targets);

            if (ability != null && ability.Effects != null && ability.Effects.Count > 0)
            {
                CombatEffectResolver.ApplyEffects(caster, hitTargets, ability.Effects, scalingRules);
            }
            else
            {
                ApplyBasicDamage(resolution.TotalDamage, hitTargets);
            }

            SendAbilityRequest(ability, hitTargets, groundPoint);
        }

        private void ApplyBasicDamage(float damage, IEnumerable<CombatEntity> targets)
        {
            foreach (var target in targets)
            {
                if (target == null)
                {
                    continue;
                }

                target.ApplyDamage(damage);
            }
        }

        private void SendAbilityRequest(
            AbilityDefinition ability,
            IEnumerable<CombatEntity> targets,
            Vector3? groundPoint)
        {
            if (serverBridge == null)
            {
                return;
            }

            var targetIds = targets?.Where(target => target != null)
                .Select(target => target.EntityId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList() ?? new List<string>();

            var request = new CombatAbilityRequest
            {
                AbilityId = ability != null ? ability.Guid : "basic-attack",
                CasterId = caster != null ? caster.EntityId : string.Empty,
                TargetIds = targetIds,
                TargetPoint = groundPoint ?? caster.Position,
                ClientTime = Time.time
            };

            serverBridge.RequestAbilityExecution(request);
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
    }
}
