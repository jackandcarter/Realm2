using UnityEngine;

namespace Client.Combat
{
    public static class PhysicalDamageCalculator
    {
        private const float PowerToBonusPercent = 0.01f;
        private const float LightAttackMultiplier = 0.9f;
        private const float MediumAttackMultiplier = 1f;
        private const float HeavyAttackMultiplier = 1.1f;

        public static PhysicalDamageResult Calculate(WeaponAttackRequest request, CombatStats stats)
        {
            var baseDamage = Mathf.Max(0f, request.BaseDamage);
            var multiplier = Mathf.Max(0f, request.DamageMultiplier) * ResolveAttackTypeMultiplier(request.AttackType);
            var statBonus = baseDamage * stats.PhysicalPower * PowerToBonusPercent;
            var totalDamage = Mathf.Max(0f, (baseDamage + statBonus) * multiplier);

            return new PhysicalDamageResult(baseDamage, multiplier, statBonus, totalDamage);
        }

        private static float ResolveAttackTypeMultiplier(WeaponComboInputType attackType)
        {
            return attackType switch
            {
                WeaponComboInputType.Light => LightAttackMultiplier,
                WeaponComboInputType.Medium => MediumAttackMultiplier,
                WeaponComboInputType.Heavy => HeavyAttackMultiplier,
                _ => MediumAttackMultiplier
            };
        }
    }
}
