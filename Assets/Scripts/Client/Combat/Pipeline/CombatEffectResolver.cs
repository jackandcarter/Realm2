using System.Collections.Generic;
using Client.Combat;
using Realm.Abilities;
using UnityEngine;

namespace Client.Combat.Pipeline
{
    public struct CombatEffectResult
    {
        public AbilityEffectType EffectType;
        public CombatEntity Target;
        public float Amount;
        public string StateName;
    }

    [System.Serializable]
    public struct AbilityEffectScalingRule
    {
        public AbilityEffectType EffectType;
        public string StatId;
        public float Ratio;
    }

    public static class CombatEffectResolver
    {
        public static List<CombatEffectResult> ApplyEffects(
            CombatEntity caster,
            IEnumerable<CombatEntity> targets,
            IEnumerable<AbilityEffect> effects,
            IReadOnlyList<AbilityEffectScalingRule> scalingRules)
        {
            var results = new List<CombatEffectResult>();
            if (effects == null)
            {
                return results;
            }

            foreach (var effect in effects)
            {
                if (effect == null)
                {
                    continue;
                }

                foreach (var target in targets)
                {
                    if (target == null)
                    {
                        continue;
                    }

                    var amount = ResolveEffectAmount(effect, caster, scalingRules);
                    ApplyEffectToTarget(effect, target, amount);
                    results.Add(new CombatEffectResult
                    {
                        EffectType = effect.EffectType,
                        Target = target,
                        Amount = amount,
                        StateName = effect.StateName
                    });
                }
            }

            return results;
        }

        private static float ResolveEffectAmount(
            AbilityEffect effect,
            CombatEntity caster,
            IReadOnlyList<AbilityEffectScalingRule> scalingRules)
        {
            var baseValue = Mathf.Max(0f, effect.Magnitude);
            if (!effect.ScalingWithPower || caster == null)
            {
                return baseValue;
            }

            var (statId, ratio) = ResolveScalingRule(effect.EffectType, scalingRules);
            if (string.IsNullOrWhiteSpace(statId))
            {
                return baseValue;
            }

            var statValue = caster.GetStatOrDefault(statId, 0f);
            return baseValue + (statValue * ratio);
        }

        private static (string statId, float ratio) ResolveScalingRule(
            AbilityEffectType effectType,
            IReadOnlyList<AbilityEffectScalingRule> scalingRules)
        {
            if (scalingRules != null)
            {
                foreach (var rule in scalingRules)
                {
                    if (rule.EffectType == effectType && !string.IsNullOrWhiteSpace(rule.StatId))
                    {
                        return (rule.StatId.Trim(), rule.Ratio);
                    }
                }
            }

            return (string.Empty, 0f);
        }

        private static void ApplyEffectToTarget(AbilityEffect effect, CombatEntity target, float amount)
        {
            switch (effect.EffectType)
            {
                case AbilityEffectType.Damage:
                    target.ApplyDamage(amount);
                    break;
                case AbilityEffectType.Heal:
                    target.ApplyHeal(amount);
                    break;
            }
        }
    }
}
