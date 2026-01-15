using UnityEngine;

namespace Client.Combat
{
    public static class PhysicalDamageCalculator
    {
        private const float PowerToBonusPercent = 0.01f;

        public static PhysicalDamageResult Calculate(WeaponAttackRequest request, CombatStats stats)
        {
            var baseDamage = Mathf.Max(0f, request.BaseDamage);
            var multiplier = Mathf.Max(0f, request.DamageMultiplier);
            var statBonus = baseDamage * stats.PhysicalPower * PowerToBonusPercent;
            var totalDamage = Mathf.Max(0f, (baseDamage + statBonus) * multiplier);

            return new PhysicalDamageResult(baseDamage, multiplier, statBonus, totalDamage);
        }
    }
}
