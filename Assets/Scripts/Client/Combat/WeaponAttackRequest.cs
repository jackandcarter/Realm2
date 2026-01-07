using Realm.Data;

namespace Client.Combat
{
    public readonly struct WeaponAttackRequest
    {
        public readonly WeaponDefinition Weapon;
        public readonly WeaponComboInputType AttackType;
        public readonly float BaseDamage;
        public readonly float DamageMultiplier;
        public readonly float Accuracy;
        public readonly float WindupSeconds;
        public readonly float RecoverySeconds;
        public readonly string SpecialAbilityId;

        public WeaponAttackRequest(
            WeaponDefinition weapon,
            WeaponComboInputType attackType,
            float baseDamage,
            float damageMultiplier,
            float accuracy,
            float windupSeconds,
            float recoverySeconds,
            string specialAbilityId)
        {
            Weapon = weapon;
            AttackType = attackType;
            BaseDamage = baseDamage;
            DamageMultiplier = damageMultiplier;
            Accuracy = accuracy;
            WindupSeconds = windupSeconds;
            RecoverySeconds = recoverySeconds;
            SpecialAbilityId = specialAbilityId;
        }
    }
}
