using System;
using Realm.Abilities;
using UnityEngine;

namespace Realm.Data
{
    [CreateAssetMenu(menuName = "Realm/Equipment/Weapon", fileName = "WeaponDefinition")]
    public class WeaponDefinition : EquipmentDefinition
    {
        [SerializeField, Tooltip("Weapon type this item belongs to.")]
        private WeaponTypeDefinition weaponType;

        [Header("Combat")]
        [SerializeField, Tooltip("Base damage before attack multipliers are applied.")]
        private float baseDamage = 10f;

        [SerializeField] private WeaponAttackProfile lightAttack = WeaponAttackProfile.DefaultLight;
        [SerializeField] private WeaponAttackProfile mediumAttack = WeaponAttackProfile.DefaultMedium;
        [SerializeField] private WeaponAttackProfile heavyAttack = WeaponAttackProfile.DefaultHeavy;

        [SerializeField, Tooltip("Optional special attack ability tied to this weapon.")]
        private AbilityDefinition specialAttack;

        public WeaponTypeDefinition WeaponType => weaponType;
        public float BaseDamage => baseDamage;
        public WeaponAttackProfile LightAttack => lightAttack;
        public WeaponAttackProfile MediumAttack => mediumAttack;
        public WeaponAttackProfile HeavyAttack => heavyAttack;
        public AbilityDefinition SpecialAttack => specialAttack;

        public void ApplySeed(WeaponSeedData seed)
        {
            ApplySeed(seed as EquipmentSeedData);
            if (seed == null)
            {
                return;
            }

            baseDamage = seed.BaseDamage;
            lightAttack = seed.LightAttack;
            mediumAttack = seed.MediumAttack;
            heavyAttack = seed.HeavyAttack;
            specialAttack = seed.SpecialAttack;
        }
    }

    [Serializable]
    public struct WeaponAttackProfile
    {
        public static readonly WeaponAttackProfile DefaultLight = new WeaponAttackProfile(0.8f, 0.75f, 0.2f, 0.25f);
        public static readonly WeaponAttackProfile DefaultMedium = new WeaponAttackProfile(1f, 0.85f, 0.3f, 0.35f);
        public static readonly WeaponAttackProfile DefaultHeavy = new WeaponAttackProfile(1.35f, 0.95f, 0.45f, 0.5f);

        [SerializeField] private float damageMultiplier;
        [SerializeField] private float accuracy;
        [SerializeField] private float windupSeconds;
        [SerializeField] private float recoverySeconds;

        public float DamageMultiplier => damageMultiplier;
        public float Accuracy => accuracy;
        public float WindupSeconds => windupSeconds;
        public float RecoverySeconds => recoverySeconds;

        public WeaponAttackProfile(float damageMultiplier, float accuracy, float windupSeconds, float recoverySeconds)
        {
            this.damageMultiplier = damageMultiplier;
            this.accuracy = accuracy;
            this.windupSeconds = windupSeconds;
            this.recoverySeconds = recoverySeconds;
        }
    }
}
