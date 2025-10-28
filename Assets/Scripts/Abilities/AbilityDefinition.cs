using System;
using System.Collections.Generic;
using UnityEngine;

namespace Realm.Abilities
{
    public enum AbilityTargetingMode
    {
        Self,
        Ally,
        Enemy,
        Area,
        Global
    }

    public enum AbilityAreaShape
    {
        None,
        Circle,
        Cone,
        Line
    }

    public enum AbilityResourceType
    {
        None,
        Mana,
        Energy,
        Stamina,
        Coolant,
        Custom
    }

    public enum AbilityEffectType
    {
        Damage,
        Heal,
        Buff,
        Debuff,
        StateChange,
        Custom
    }

    [Serializable]
    public class AbilityTargetingConfig
    {
        public AbilityTargetingMode Mode = AbilityTargetingMode.Enemy;
        public AbilityAreaShape AreaShape = AbilityAreaShape.None;
        public float AreaSize = 0f;
        public int MaxTargets = 1;
        public bool RequiresPrimaryTarget = true;
        public bool CanAffectCaster;
    }

    [Serializable]
    public class AbilityResourceConfig
    {
        public AbilityResourceType ResourceType = AbilityResourceType.Mana;
        public float Cost = 10f;
        public bool PercentageCost;
        public float CooldownSeconds = 10f;
        public float GlobalCooldownSeconds = 1.5f;
    }

    [Serializable]
    public class AbilityExecutionCondition
    {
        public string Label;
        public string Description;
        public bool RequiresLoS;
        public bool RequiresGroundTarget;
        public bool OnlyWhileMoving;
        public bool OnlyWhileStationary;
        public bool RequiresBuffActive;
        public string RequiredBuffName;
        public float HealthThreshold = 1f;
        public bool RequiresComboWindow;
    }

    [Serializable]
    public class AbilityEffect
    {
        public string Name = "New Effect";
        public AbilityEffectType EffectType = AbilityEffectType.Damage;
        public float Magnitude = 10f;
        public float DurationSeconds = 0f;
        public float TickInterval = 1f;
        public bool ScalingWithPower = true;
        public string StateName;
        public string CustomSummary;
        public int Priority;
    }

    [CreateAssetMenu(menuName = "Realm/Abilities/Ability Definition", fileName = "NewAbilityDefinition")]
    public class AbilityDefinition : ScriptableObject
    {
        public string AbilityName = "New Ability";
        [TextArea] public string Description;
        public Sprite Icon;

        public AbilityTargetingConfig Targeting = new AbilityTargetingConfig();
        public AbilityResourceConfig Resource = new AbilityResourceConfig();
        public AbilityExecutionCondition Execution = new AbilityExecutionCondition();
        public List<AbilityEffect> Effects = new List<AbilityEffect>();

        public string BuildSummary()
        {
            var segments = new List<string>();

            if (!string.IsNullOrWhiteSpace(Description))
            {
                segments.Add(Description.Trim());
            }

            segments.Add(DescribeTargeting());

            if (Resource != null && Resource.ResourceType != AbilityResourceType.None)
            {
                var cost = Resource.PercentageCost ? $"{Resource.Cost}%" : Resource.Cost.ToString("0.#");
                segments.Add($"Costs {cost} {Resource.ResourceType} with {Resource.CooldownSeconds:0.#}s cooldown.");
            }
            else
            {
                var cooldown = Resource != null ? Resource.CooldownSeconds : 0f;
                segments.Add($"Cooldown: {cooldown:0.#}s.");
            }

            if (Effects != null)
            {
                foreach (var effect in Effects)
                {
                    segments.Add(DescribeEffect(effect));
                }
            }

            return string.Join(" \u2022 ", segments);
        }

        private string DescribeTargeting()
        {
            var targeting = Targeting ?? new AbilityTargetingConfig();

            if (targeting.Mode == AbilityTargetingMode.Area)
            {
                var shape = targeting.AreaShape.ToString();
                var size = targeting.AreaSize <= 0 ? "unspecified size" : $"{targeting.AreaSize:0.#}m";
                return $"Targets up to {targeting.MaxTargets} in a {shape} ({size}).";
            }

            if (targeting.Mode == AbilityTargetingMode.Global)
            {
                return $"Global effect hitting up to {targeting.MaxTargets}.";
            }

            var target = targeting.Mode.ToString().ToLowerInvariant();
            return targeting.MaxTargets <= 1
                ? $"Single-target ({target})."
                : $"Hits up to {targeting.MaxTargets} {target} targets.";
        }

        private string DescribeEffect(AbilityEffect effect)
        {
            if (!string.IsNullOrWhiteSpace(effect.CustomSummary))
            {
                return effect.CustomSummary;
            }

            var magnitude = effect.Magnitude.ToString("0.#");
            var duration = effect.DurationSeconds > 0 ? $" over {effect.DurationSeconds:0.#}s" : string.Empty;
            var scaling = effect.ScalingWithPower ? " (scales with power)" : string.Empty;

            switch (effect.EffectType)
            {
                case AbilityEffectType.Damage:
                    return $"Deals {magnitude}{duration} damage{scaling}.";
                case AbilityEffectType.Heal:
                    return $"Heals {magnitude}{duration}{scaling}.";
                case AbilityEffectType.Buff:
                    return $"Applies buff '{effect.StateName}' for {effect.DurationSeconds:0.#}s.";
                case AbilityEffectType.Debuff:
                    return $"Inflicts debuff '{effect.StateName}' for {effect.DurationSeconds:0.#}s.";
                case AbilityEffectType.StateChange:
                    return $"Changes state to '{effect.StateName}'{duration}.";
                default:
                    return $"{effect.Name}: {magnitude}{duration}.";
            }
        }
    }
}
