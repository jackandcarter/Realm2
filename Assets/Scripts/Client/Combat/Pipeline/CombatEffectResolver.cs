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
        public float DurationSeconds;
        public string CustomSummary;
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
            var results = ResolveEffects(caster, targets, effects, scalingRules);
            ApplyResolvedEffects(results);
            return results;
        }

        public static List<CombatEffectResult> ResolveEffects(
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
                    var stateName = string.IsNullOrWhiteSpace(effect.StateName) ? effect.Name : effect.StateName;
                    var summary = string.IsNullOrWhiteSpace(effect.CustomSummary) ? effect.Name : effect.CustomSummary;
                    results.Add(new CombatEffectResult
                    {
                        EffectType = effect.EffectType,
                        Target = target,
                        Amount = amount,
                        StateName = stateName,
                        DurationSeconds = effect.DurationSeconds,
                        CustomSummary = summary
                    });
                }
            }

            return results;
        }

        public static void ApplyResolvedEffects(IEnumerable<CombatEffectResult> results)
        {
            if (results == null)
            {
                return;
            }

            foreach (var result in results)
            {
                ApplyResolvedEffect(result);
            }
        }

        private static void ApplyResolvedEffect(CombatEffectResult result)
        {
            if (result.Target == null)
            {
                return;
            }

            switch (result.EffectType)
            {
                case AbilityEffectType.Damage:
                    result.Target.ApplyDamage(result.Amount);
                    break;
                case AbilityEffectType.Heal:
                    result.Target.ApplyHeal(result.Amount);
                    break;
                case AbilityEffectType.Buff:
                    result.Target.ApplyBuff(result.StateName, result.DurationSeconds, result.Amount);
                    break;
                case AbilityEffectType.Debuff:
                    result.Target.ApplyDebuff(result.StateName, result.DurationSeconds, result.Amount);
                    break;
                case AbilityEffectType.StateChange:
                    result.Target.ApplyStateChange(result.StateName, result.DurationSeconds);
                    break;
                case AbilityEffectType.Custom:
                    result.Target.ApplyCustomEffect(result.CustomSummary, result.Amount);
                    break;
            }
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

    }
}
