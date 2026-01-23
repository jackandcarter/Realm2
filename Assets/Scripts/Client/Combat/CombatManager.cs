using System;
using System.Collections.Generic;
using Realm.Abilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Client.Combat
{
    [DisallowMultipleComponent]
    public class CombatManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WeaponAttackController attackController;
        [SerializeField] private WeaponComboTracker comboTracker;

        [Header("Ability Resolution")]
        [SerializeField] private List<AbilityDefinition> abilityDefinitions = new();

        [Header("Fallback Hitbox")]
        [SerializeField] private AbilityHitboxConfig defaultWeaponHitbox = new AbilityHitboxConfig();

        [Header("Combat Stats")]
        [SerializeField] private MonoBehaviour statsProvider;

        private readonly Dictionary<string, AbilityDefinition> _abilityLookup =
            new Dictionary<string, AbilityDefinition>(StringComparer.OrdinalIgnoreCase);
        private ICombatStatsProvider _statsProvider;

        public event Action<WeaponAttackResolution> AttackResolved;
        public event Action<WeaponAttackResolution, AbilityDefinition> SpecialAttackResolved;

        public bool TryGetAbilityDefinition(string abilityId, out AbilityDefinition ability)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                ability = null;
                return false;
            }

            if (_abilityLookup.TryGetValue(abilityId.Trim(), out ability))
            {
                return true;
            }

            return AbilityRegistry.TryGetAbility(abilityId.Trim(), out ability);
        }

        private void Awake()
        {
            ResolveAttackController();
            ResolveComboTracker();
            ResolveStatsProvider();
            BuildAbilityLookup();
        }

        private void OnEnable()
        {
            ResolveAttackController();
            ResolveComboTracker();
            ResolveStatsProvider();
            BindAttackController();
        }

        private void OnDisable()
        {
            UnbindAttackController();
        }

        private void OnValidate()
        {
            if (defaultWeaponHitbox == null)
            {
                defaultWeaponHitbox = new AbilityHitboxConfig();
            }

            if (statsProvider != null && statsProvider is not ICombatStatsProvider)
            {
                statsProvider = null;
            }

            BuildAbilityLookup();
        }

        private void ResolveAttackController()
        {
            if (attackController != null)
            {
                return;
            }

#if UNITY_2023_1_OR_NEWER
            attackController = Object.FindFirstObjectByType<WeaponAttackController>(FindObjectsInactive.Include);
#else
            attackController = FindObjectOfType<WeaponAttackController>(true);
#endif
        }

        private void ResolveComboTracker()
        {
            if (comboTracker != null)
            {
                return;
            }

#if UNITY_2023_1_OR_NEWER
            comboTracker = Object.FindFirstObjectByType<WeaponComboTracker>(FindObjectsInactive.Include);
#else
            comboTracker = FindObjectOfType<WeaponComboTracker>(true);
#endif
        }

        private void ResolveStatsProvider()
        {
            if (statsProvider != null && statsProvider is ICombatStatsProvider provider)
            {
                _statsProvider = provider;
                return;
            }

            _statsProvider = FindStatsProviderInScene();
        }

        private static ICombatStatsProvider FindStatsProviderInScene()
        {
#if UNITY_2023_1_OR_NEWER
            var behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
#endif

            foreach (var behaviour in behaviours)
            {
                if (behaviour is ICombatStatsProvider provider)
                {
                    return provider;
                }
            }

            return null;
        }

        private void BindAttackController()
        {
            if (attackController == null)
            {
                return;
            }

            attackController.AttackRequested -= HandleAttackRequested;
            attackController.AttackRequested += HandleAttackRequested;
            attackController.SpecialRequested -= HandleSpecialRequested;
            attackController.SpecialRequested += HandleSpecialRequested;
        }

        private void UnbindAttackController()
        {
            if (attackController == null)
            {
                return;
            }

            attackController.AttackRequested -= HandleAttackRequested;
            attackController.SpecialRequested -= HandleSpecialRequested;
        }

        private void HandleAttackRequested(WeaponAttackRequest request)
        {
            var resolution = BuildResolution(request, null);
            AttackResolved?.Invoke(resolution);
        }

        private void HandleSpecialRequested(WeaponAttackRequest request)
        {
            var ability = ResolveSpecialAbility(request);
            var resolution = BuildResolution(request, ability);
            AttackResolved?.Invoke(resolution);

            if (ability != null)
            {
                ExecuteSpecialAbility(resolution, ability);
            }

            SpecialAttackResolved?.Invoke(resolution, ability);
        }

        private WeaponAttackResolution BuildResolution(WeaponAttackRequest request, AbilityDefinition ability)
        {
            var hitbox = ability != null ? ability.Hitbox : null;

            return new WeaponAttackResolution(
                request,
                0f,
                0f,
                CloneHitbox(hitbox),
                ability,
                CombatStats.Default,
                new PhysicalDamageResult(0f, 0f, 0f, 0f));
        }

        private AbilityDefinition ResolveSpecialAbility(WeaponAttackRequest request)
        {
            var abilityId = request.SpecialAbilityId;
            if (string.IsNullOrWhiteSpace(abilityId) && comboTracker != null)
            {
                abilityId = comboTracker.CurrentSpecialAttackAbilityId;
            }

            if (!string.IsNullOrWhiteSpace(abilityId) && _abilityLookup.TryGetValue(abilityId, out var ability))
            {
                return ability;
            }

            return request.Weapon != null ? request.Weapon.SpecialAttack : null;
        }

        private void ExecuteSpecialAbility(WeaponAttackResolution resolution, AbilityDefinition ability)
        {
            if (ability == null)
            {
                return;
            }

            Debug.Log(
                $"Queued special ability '{ability.AbilityName}' for server resolution.",
                this);
        }

        private void BuildAbilityLookup()
        {
            _abilityLookup.Clear();
            if (abilityDefinitions == null)
            {
                return;
            }

            foreach (var ability in abilityDefinitions)
            {
                if (ability == null || string.IsNullOrWhiteSpace(ability.Guid))
                {
                    continue;
                }

                _abilityLookup[ability.Guid] = ability;
            }

            AbilityRegistry.RegisterAbilities(abilityDefinitions);
        }

        private static AbilityHitboxConfig CloneHitbox(AbilityHitboxConfig source)
        {
            if (source == null)
            {
                return new AbilityHitboxConfig();
            }

            return new AbilityHitboxConfig
            {
                Shape = source.Shape,
                Size = source.Size,
                Radius = source.Radius,
                Length = source.Length,
                Offset = source.Offset,
                UseCasterFacing = source.UseCasterFacing,
                ActiveSeconds = source.ActiveSeconds,
                RequiresContact = source.RequiresContact
            };
        }
    }
}
