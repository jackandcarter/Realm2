using System;
using System.Collections.Generic;
using Realm.Abilities;
using UnityEngine;

namespace Realm.Combat.Data
{
    public enum ComboInputType
    {
        Light,
        Medium,
        Heavy
    }

    public enum HitShapeType
    {
        Arc,
        Cone,
        Box,
        Sphere
    }

    public enum SpecialRuleType
    {
        SequenceMatch,
        HitCount,
        FinisherReached,
        TimeInCombat,
        MeterFill
    }

    public enum DamageType
    {
        Physical,
        Magical,
        Element
    }

    public enum StatusEffectType
    {
        Buff,
        Debuff,
        CrowdControl
    }

    public enum StatusRefreshRule
    {
        RefreshDuration,
        AddStacks,
        Ignore
    }

    public enum StatusDispelType
    {
        None,
        Magic,
        Curse,
        Poison,
        Disease
    }

    public enum AbilityTargetingType
    {
        Self,
        AllyEntity,
        EnemyEntity,
        GroundPoint,
        ConeFromCaster,
        NoTargetForward
    }

    public enum AbilityCastModel
    {
        Instant,
        CastTime,
        Channel,
        Charged
    }

    [CreateAssetMenu(menuName = "Realm/Combat/Weapon Combat Definition", fileName = "WeaponCombatDefinition")]
    public class WeaponCombatDefinition : ScriptableObject
    {
        [SerializeField] private ComboGraphDefinition comboGraph = new();

        public ComboGraphDefinition ComboGraph => comboGraph;
    }

    [Serializable]
    public class ComboGraphDefinition
    {
        public List<ComboStartNodeDefinition> StartNodes = new();
        public List<ComboNodeDefinition> Nodes = new();
        public List<ComboEdgeDefinition> Edges = new();
    }

    [Serializable]
    public class ComboStartNodeDefinition
    {
        public ComboInputType Input;
        public string NodeId;
    }

    [Serializable]
    public class ComboNodeDefinition
    {
        public string NodeId;
        public ComboStepDefinition Step = new();
    }

    [Serializable]
    public class ComboEdgeDefinition
    {
        public string FromNodeId;
        public ComboInputType Input;
        public string ToNodeId;
        public ComboEdgeConditionDefinition Conditions = new();
    }

    [Serializable]
    public class ComboEdgeConditionDefinition
    {
        public bool HitConfirmedRequired;
        public float StaminaMin;
    }

    [Serializable]
    public class ComboStepDefinition
    {
        public string StepId;
        public string AnimationKey;
        public EffectListDefinition EffectList;
        public DamageEffectDefinition DamageEffect;
        public List<HitShapeDefinition> HitShapes = new();
        public float ContinueWindowStartNormalized;
        public float ContinueWindowEndNormalized;
        public float CancelIntoAbilityStartNormalized;
        public float CancelIntoAbilityEndNormalized;
        public float CancelIntoDodgeStartNormalized;
        public float CancelIntoDodgeEndNormalized;
        public ComboStepMovementBehavior MovementBehavior = new();
        public List<string> Tags = new();
    }

    [Serializable]
    public class ComboStepMovementBehavior
    {
        public float LungeDistance;
        public bool RootDuringSwing;
    }

    [Serializable]
    public class HitShapeDefinition
    {
        public HitShapeType Type;
        public float Range;
        public float Radius;
        public float Width;
        public float Angle;
        public Vector3 Offset;
        public bool RequiresLoS;
        public int MaxTargets;
        public float HitMomentNormalized;
    }

    [CreateAssetMenu(menuName = "Realm/Combat/Weapon Special Definition", fileName = "WeaponSpecialDefinition")]
    public class WeaponSpecialDefinition : ScriptableObject
    {
        [SerializeField] private SpecialRuleDefinition rule = new();
        [SerializeField] private SpecialActionDefinition action = new();

        public SpecialRuleDefinition Rule => rule;
        public SpecialActionDefinition Action => action;
    }

    [Serializable]
    public class SpecialRuleDefinition
    {
        public SpecialRuleType RuleType;
        public List<ComboInputType> SequenceMatch = new();
        public int HitCount;
        public float HitWindowSeconds;
        public string FinisherTag;
        public float TimeInCombatSeconds;
        public float MeterFill;
        public float ExpiresAfterSeconds;
    }

    [Serializable]
    public class SpecialActionDefinition
    {
        public string SpecialId;
        public AbilityDefinition AbilityReference;
        public EffectListDefinition InlineEffectList;
        public float CooldownSeconds;
        public List<ResourceCostDefinition> ResourceCosts = new();
    }

    [Serializable]
    public class ResourceCostDefinition
    {
        public string ResourceTypeId;
        public float Amount;
    }

    [CreateAssetMenu(menuName = "Realm/Combat/Effect List Definition", fileName = "EffectListDefinition")]
    public class EffectListDefinition : ScriptableObject
    {
        [SerializeReference] public List<EffectDefinition> Effects = new();
    }

    [Serializable]
    public abstract class EffectDefinition
    {
    }

    [Serializable]
    public class DamageEffectDefinition : EffectDefinition
    {
        public float BaseValue;
        public string ScalingStatId;
        public float Coefficient;
        public DamageType DamageType;
        public bool CanCrit;
    }

    [Serializable]
    public class HealEffectDefinition : EffectDefinition
    {
        public float BaseValue;
        public string ScalingStatId;
        public float Coefficient;
    }

    [Serializable]
    public class ApplyStatusEffectDefinition : EffectDefinition
    {
        public string StatusId;
        public float DurationSeconds;
        public int Stacks;
        public StatusRefreshRule RefreshRule;
    }

    [Serializable]
    public class ModifyResourceEffectDefinition : EffectDefinition
    {
        public string ResourceTypeId;
        public float Delta;
    }

    [Serializable]
    public class SpawnProjectileEffectDefinition : EffectDefinition
    {
        public string ProjectileDefinitionId;
        public float Speed;
        public float LifetimeSeconds;
        public string HomingRuleId;
    }

    [Serializable]
    public class SpawnZoneEffectDefinition : EffectDefinition
    {
        public string ZoneDefinitionId;
        public float Radius;
        public float DurationSeconds;
        public EffectListDefinition TickEffectList;
    }

    [CreateAssetMenu(menuName = "Realm/Combat/Status Effect Definition", fileName = "StatusEffectDefinition")]
    public class StatusEffectDefinition : ScriptableObject
    {
        [SerializeField] private string statusId;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private StatusEffectType type;
        [SerializeField] private StatusRefreshRule refreshRule = StatusRefreshRule.RefreshDuration;
        [SerializeField] private int maxStacks = 1;
        [SerializeField] private StatusDispelType dispelType = StatusDispelType.None;
        [SerializeField] private string durationModelId;
        [SerializeField] private string stackingRuleId;
        [SerializeField] private List<StatusStatModifierDefinition> modifiers = new();
        [SerializeField] private StatusActionRestrictionDefinition actionRestrictions = new();
        [SerializeField] private StatusPeriodicEffectDefinition periodicEffects = new();

        public string StatusId => statusId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public StatusEffectType Type => type;
        public StatusRefreshRule RefreshRule => refreshRule;
        public int MaxStacks => maxStacks;
        public StatusDispelType DispelType => dispelType;
        public string DurationModelId => durationModelId;
        public string StackingRuleId => stackingRuleId;
        public IReadOnlyList<StatusStatModifierDefinition> Modifiers => modifiers;
        public StatusActionRestrictionDefinition ActionRestrictions => actionRestrictions;
        public StatusPeriodicEffectDefinition PeriodicEffects => periodicEffects;
    }

    [Serializable]
    public class StatusStatModifierDefinition
    {
        public string StatId;
        public float Value;
    }

    [Serializable]
    public class StatusActionRestrictionDefinition
    {
        public bool BlocksAbilities;
        public bool BlocksAllActions;
    }

    [Serializable]
    public class StatusPeriodicEffectDefinition
    {
        public float TickRateSeconds;
        public EffectListDefinition TickEffectList;
    }
}
