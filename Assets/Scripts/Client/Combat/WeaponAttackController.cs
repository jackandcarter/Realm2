using System;
using Client.Player;
using Realm.Data;
using UnityEngine;

namespace Client.Combat
{
    [DisallowMultipleComponent]
    public class WeaponAttackController : MonoBehaviour
    {
        [SerializeField] private WeaponComboTracker comboTracker;

        public event Action<WeaponAttackRequest> AttackRequested;
        public event Action<WeaponAttackRequest> SpecialRequested;

        private void Awake()
        {
            ResolveComboTracker();
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
            AttackRequested?.Invoke(request);
        }

        public void HandleSpecial()
        {
            var weapon = PlayerEquipmentStateManager.GetEquippedItem(EquipmentSlot.Weapon) as WeaponDefinition;
            if (weapon == null)
            {
                return;
            }

            var abilityId = comboTracker != null
                ? comboTracker.CurrentSpecialAttackAbilityId
                : string.Empty;

            var request = new WeaponAttackRequest(
                weapon,
                WeaponComboInputType.Special,
                weapon.BaseDamage,
                weapon.HeavyAttack.DamageMultiplier,
                weapon.HeavyAttack.Accuracy,
                weapon.HeavyAttack.WindupSeconds,
                weapon.HeavyAttack.RecoverySeconds,
                abilityId);
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
    }
}
