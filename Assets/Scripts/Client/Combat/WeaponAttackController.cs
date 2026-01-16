using System;
using Client.Combat.Runtime;
using Client.Player;
using Realm.Combat.Data;
using Realm.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Client.Combat
{
    [DisallowMultipleComponent]
    public class WeaponAttackController : MonoBehaviour
    {
        [SerializeField] private WeaponComboTracker comboTracker;
        [SerializeField] private CombatStateMachine combatStateMachine;
        [SerializeField] private AnimationCombatDriver animationCombatDriver;

        public event Action<WeaponAttackRequest> AttackRequested;
        public event Action<WeaponAttackRequest> SpecialRequested;

        private void Awake()
        {
            ResolveComboTracker();
            ResolveCombatStateMachine();
            ResolveAnimationCombatDriver();
        }

        private void OnEnable()
        {
            ResolveCombatStateMachine();
            if (combatStateMachine != null)
            {
                combatStateMachine.ComboStepStarted -= HandleComboStepStarted;
                combatStateMachine.ComboStepStarted += HandleComboStepStarted;
            }
        }

        private void OnDisable()
        {
            if (combatStateMachine != null)
            {
                combatStateMachine.ComboStepStarted -= HandleComboStepStarted;
            }
        }

        public void HandleAttack(WeaponComboInputType inputType)
        {
            var weapon = PlayerEquipmentStateManager.GetEquippedItem(EquipmentSlot.Weapon) as WeaponDefinition;
            if (weapon == null)
            {
                return;
            }

            var profile = ResolveProfile(weapon, inputType);
            var request = new WeaponAttackRequest(
                weapon,
                inputType,
                weapon.BaseDamage,
                profile.DamageMultiplier,
                profile.Accuracy,
                profile.WindupSeconds,
                profile.RecoverySeconds,
                string.Empty);

            if (combatStateMachine != null)
            {
                combatStateMachine.SetCombatDefinition(weapon.CombatDefinition);
                combatStateMachine.SetSpecialDefinition(weapon.SpecialDefinition);
                if (weapon.CombatDefinition != null)
                {
                    var result = combatStateMachine.HandleComboInput(
                        MapComboInput(inputType),
                        profile.RecoverySeconds,
                        out var step);
                    if (result == ActionHandleResult.Rejected)
                    {
                        return;
                    }

                    return;
                }

                combatStateMachine.RegisterComboInput(MapComboInput(inputType));
            }

            AttackRequested?.Invoke(request);
        }

        public void HandleSpecial()
        {
            HandleSpecial(string.Empty);
        }

        public void HandleSpecial(string abilityId)
        {
            var weapon = PlayerEquipmentStateManager.GetEquippedItem(EquipmentSlot.Weapon) as WeaponDefinition;
            if (weapon == null)
            {
                return;
            }

            if (combatStateMachine != null && weapon.SpecialDefinition != null)
            {
                combatStateMachine.SetSpecialDefinition(weapon.SpecialDefinition);
                if (!combatStateMachine.TryConsumeSpecialReady())
                {
                    return;
                }
            }

            var resolvedAbilityId = ResolveSpecialAbilityId(abilityId, weapon);

            var request = new WeaponAttackRequest(
                weapon,
                WeaponComboInputType.Special,
                weapon.BaseDamage,
                weapon.HeavyAttack.DamageMultiplier,
                weapon.HeavyAttack.Accuracy,
                weapon.HeavyAttack.WindupSeconds,
                weapon.HeavyAttack.RecoverySeconds,
                resolvedAbilityId);
            SpecialRequested?.Invoke(request);
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

        private void ResolveCombatStateMachine()
        {
            if (combatStateMachine != null)
            {
                return;
            }

#if UNITY_2023_1_OR_NEWER
            combatStateMachine = Object.FindFirstObjectByType<CombatStateMachine>(FindObjectsInactive.Include);
#else
            combatStateMachine = FindObjectOfType<CombatStateMachine>(true);
#endif
        }

        private void ResolveAnimationCombatDriver()
        {
            if (animationCombatDriver != null)
            {
                return;
            }

#if UNITY_2023_1_OR_NEWER
            animationCombatDriver = Object.FindFirstObjectByType<AnimationCombatDriver>(FindObjectsInactive.Include);
#else
            animationCombatDriver = FindObjectOfType<AnimationCombatDriver>(true);
#endif
        }

        private static WeaponAttackProfile ResolveProfile(WeaponDefinition weapon, WeaponComboInputType inputType)
        {
            return inputType switch
            {
                WeaponComboInputType.Light => weapon.LightAttack,
                WeaponComboInputType.Medium => weapon.MediumAttack,
                WeaponComboInputType.Heavy => weapon.HeavyAttack,
                _ => weapon.MediumAttack
            };
        }

        private static ComboInputType MapComboInput(WeaponComboInputType inputType)
        {
            return inputType switch
            {
                WeaponComboInputType.Light => ComboInputType.Light,
                WeaponComboInputType.Medium => ComboInputType.Medium,
                WeaponComboInputType.Heavy => ComboInputType.Heavy,
                _ => ComboInputType.Medium
            };
        }

        private string ResolveSpecialAbilityId(string abilityId, WeaponDefinition weapon)
        {
            if (!string.IsNullOrWhiteSpace(abilityId))
            {
                return abilityId;
            }

            if (combatStateMachine != null && !string.IsNullOrWhiteSpace(combatStateMachine.CurrentSpecialAbilityId))
            {
                return combatStateMachine.CurrentSpecialAbilityId;
            }

            if (comboTracker != null && !string.IsNullOrWhiteSpace(comboTracker.CurrentSpecialAttackAbilityId))
            {
                return comboTracker.CurrentSpecialAttackAbilityId;
            }

            if (weapon.SpecialDefinition != null && weapon.SpecialDefinition.Action != null)
            {
                var ability = weapon.SpecialDefinition.Action.AbilityReference;
                if (ability != null && !string.IsNullOrWhiteSpace(ability.Guid))
                {
                    return ability.Guid;
                }

                if (!string.IsNullOrWhiteSpace(weapon.SpecialDefinition.Action.SpecialId))
                {
                    return weapon.SpecialDefinition.Action.SpecialId;
                }
            }

            return weapon.SpecialAttack != null ? weapon.SpecialAttack.Guid : string.Empty;
        }

        private void HandleComboStepStarted(ComboStepContext context)
        {
            var weapon = PlayerEquipmentStateManager.GetEquippedItem(EquipmentSlot.Weapon) as WeaponDefinition;
            if (weapon == null)
            {
                return;
            }

            var inputType = MapWeaponInput(context.Input);
            var profile = ResolveProfile(weapon, inputType);
            var request = new WeaponAttackRequest(
                weapon,
                inputType,
                weapon.BaseDamage,
                profile.DamageMultiplier,
                profile.Accuracy,
                profile.WindupSeconds,
                profile.RecoverySeconds,
                string.Empty);

            animationCombatDriver?.BeginComboStep(context.Step);
            AttackRequested?.Invoke(request);
        }

        private static WeaponComboInputType MapWeaponInput(ComboInputType inputType)
        {
            return inputType switch
            {
                ComboInputType.Light => WeaponComboInputType.Light,
                ComboInputType.Medium => WeaponComboInputType.Medium,
                ComboInputType.Heavy => WeaponComboInputType.Heavy,
                _ => WeaponComboInputType.Medium
            };
        }
    }
}
