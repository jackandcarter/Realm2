using System;
using System.Collections.Generic;
using Realm.Abilities;
using UnityEngine;

namespace Realm.Data
{
    [CreateAssetMenu(menuName = "Realm/Classes/Class Definition", fileName = "ClassDefinition")]
    public class ClassDefinition : ConfigurationAsset, IGuidIdentified
    {
        [SerializeField, Tooltip("Stable unique identifier for this class. Auto-generated if empty.")]
        private string guid;

        [SerializeField]
        private string displayName;

        [SerializeField, TextArea(2, 5)]
        private string description;

        [SerializeField, Tooltip("Optional icon or portrait displayed in selection menus.")]
        private Sprite icon;

        [SerializeField, Tooltip("Categories that help describe this class' focus areas.")]
        private List<StatCategory> statCategories = new();

        [SerializeField, Tooltip("Defines the stat profile used to evaluate JRPG style curves and formulas for this class.")]
        private StatProfileDefinition statProfile;

        [SerializeField, Tooltip("Base stat values evaluated by level for this class.")]
        private List<ClassStatCurve> baseStatCurves = new();

        [SerializeField, Tooltip("Growth modifiers that apply after the base stat curve is evaluated.")]
        private List<ClassStatCurve> growthModifiers = new();

        [SerializeField, Tooltip("Armor categories the class may equip.")]
        private List<ArmorType> allowedArmorTypes = new();

        [SerializeField, Tooltip("Weapon categories the class may equip.")]
        private List<WeaponType> allowedWeaponTypes = new();

        [SerializeField, Tooltip("Ability unlocks describing how the class gains access to abilities.")]
        private List<ClassAbilityUnlock> abilityUnlocks = new();

        public string Guid => guid;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public IReadOnlyList<StatCategory> StatCategories => statCategories;
        public StatProfileDefinition StatProfile => statProfile;
        public IReadOnlyList<ClassStatCurve> BaseStatCurves => baseStatCurves;
        public IReadOnlyList<ClassStatCurve> GrowthModifiers => growthModifiers;
        public IReadOnlyList<ArmorType> AllowedArmorTypes => allowedArmorTypes;
        public IReadOnlyList<WeaponType> AllowedWeaponTypes => allowedWeaponTypes;
        public IReadOnlyList<ClassAbilityUnlock> AbilityUnlocks => abilityUnlocks;

        public ClassStatCurve FindBaseCurve(StatDefinition stat)
        {
            return FindCurve(baseStatCurves, stat);
        }

        public ClassStatCurve FindGrowthCurve(StatDefinition stat)
        {
            return FindCurve(growthModifiers, stat);
        }

        private static ClassStatCurve FindCurve(List<ClassStatCurve> curves, StatDefinition stat)
        {
            if (curves == null || stat == null)
            {
                return null;
            }

            for (var i = 0; i < curves.Count; i++)
            {
                var curve = curves[i];
                if (curve != null && curve.Stat == stat)
                {
                    return curve;
                }
            }

            return null;
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            if (string.IsNullOrWhiteSpace(guid))
            {
                guid = System.Guid.NewGuid().ToString("N");
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }

            displayName = string.IsNullOrWhiteSpace(displayName) ? name : displayName.Trim();
            description = description?.Trim();

            RemoveNullsAndDuplicates(statCategories);
            RemoveNullCurves(baseStatCurves);
            RemoveNullCurves(growthModifiers);
            RemoveDuplicateValues(allowedArmorTypes);
            RemoveDuplicateValues(allowedWeaponTypes);
            NormalizeAbilityUnlocks(abilityUnlocks);

#if UNITY_EDITOR
            SynchronizeCurveData(baseStatCurves);
            SynchronizeCurveData(growthModifiers);
            SynchronizeAbilityUnlocks();
#endif
        }

        private static void RemoveNullsAndDuplicates<T>(List<T> items) where T : UnityEngine.Object
        {
            if (items == null)
            {
                return;
            }

            var seen = new HashSet<T>();
            for (var i = items.Count - 1; i >= 0; i--)
            {
                var entry = items[i];
                if (entry == null || !seen.Add(entry))
                {
                    items.RemoveAt(i);
                }
            }
        }

        private static void RemoveDuplicateValues<T>(List<T> items)
        {
            if (items == null)
            {
                return;
            }

            var seen = new HashSet<T>();
            for (var i = items.Count - 1; i >= 0; i--)
            {
                if (!seen.Add(items[i]))
                {
                    items.RemoveAt(i);
                }
            }
        }

#if UNITY_EDITOR
        private static void SynchronizeCurveData(List<ClassStatCurve> curves)
        {
            if (curves == null)
            {
                return;
            }

            for (var i = 0; i < curves.Count; i++)
            {
                curves[i]?.EnsureEditorConsistency();
            }
        }

        private void SynchronizeAbilityUnlocks()
        {
            if (abilityUnlocks == null)
            {
                return;
            }

            foreach (var unlock in abilityUnlocks)
            {
                unlock?.EnsureEditorConsistency();
            }
        }
#endif

        private static void RemoveNullCurves(List<ClassStatCurve> curves)
        {
            if (curves == null)
            {
                return;
            }

            var seen = new HashSet<StatDefinition>();
            for (var i = curves.Count - 1; i >= 0; i--)
            {
                var curve = curves[i];
                if (curve == null || curve.Stat == null || !seen.Add(curve.Stat))
                {
                    curves.RemoveAt(i);
                }
            }
        }

        private static void NormalizeAbilityUnlocks(List<ClassAbilityUnlock> unlocks)
        {
            if (unlocks == null)
            {
                return;
            }

            var seen = new HashSet<AbilityDefinition>();
            for (var i = unlocks.Count - 1; i >= 0; i--)
            {
                var unlock = unlocks[i];
                if (unlock == null || unlock.Ability == null)
                {
                    unlocks.RemoveAt(i);
                    continue;
                }

                if (!seen.Add(unlock.Ability))
                {
                    unlocks.RemoveAt(i);
                    continue;
                }

                unlock.ClampEditorValues();
            }
        }
    }

    [Serializable]
    public class ClassAbilityUnlock
    {
        [SerializeField]
        private AbilityDefinition ability;

        [SerializeField]
        private AbilityUnlockConditionType conditionType = AbilityUnlockConditionType.Level;

        [SerializeField]
        private int requiredLevel = 1;

        [SerializeField]
        private string questId;

        [SerializeField]
        private string itemId;

        [SerializeField, TextArea(1, 3)]
        private string notes;

        public AbilityDefinition Ability => ability;
        public AbilityUnlockConditionType ConditionType => conditionType;
        public int RequiredLevel => requiredLevel;
        public string QuestId => questId;
        public string ItemId => itemId;
        public string Notes => notes;

        public string DescribeCondition()
        {
            return conditionType switch
            {
                AbilityUnlockConditionType.Level => requiredLevel <= 1
                    ? "Unlocked at start"
                    : $"Unlocks at level {requiredLevel}",
                AbilityUnlockConditionType.Quest => string.IsNullOrWhiteSpace(questId)
                    ? "Quest unlock (unspecified)"
                    : $"Quest: {questId}",
                AbilityUnlockConditionType.Item => string.IsNullOrWhiteSpace(itemId)
                    ? "Item unlock (unspecified)"
                    : $"Item: {itemId}",
                _ => "Unlock condition unspecified"
            };
        }

#if UNITY_EDITOR
        internal void EnsureEditorConsistency()
        {
            ClampEditorValues();
        }
#endif

        internal void ClampEditorValues()
        {
            if (conditionType == AbilityUnlockConditionType.Level)
            {
                requiredLevel = Mathf.Max(1, requiredLevel);
            }
            else
            {
                requiredLevel = Mathf.Max(0, requiredLevel);
            }

            questId = questId?.Trim();
            itemId = itemId?.Trim();
            notes = notes?.Trim();
        }
    }

    public enum AbilityUnlockConditionType
    {
        Level,
        Quest,
        Item
    }

    [Serializable]
    public class ClassStatCurve
    {
        [SerializeField]
        private StatDefinition stat;

        [SerializeField]
        private AnimationCurve baseValues = AnimationCurve.Linear(1f, 0f, 100f, 0f);

        [SerializeField]
        private AnimationCurve growthValues = AnimationCurve.Linear(1f, 0f, 100f, 0f);

        [SerializeField]
        private AnimationCurve softCapCurve = new AnimationCurve();

        [SerializeField]
        private Vector2 jitterVariance = Vector2.zero;

        [SerializeField]
        private JrpgFormulaTemplate formulaTemplate = JrpgFormulaTemplate.Linear;

        [SerializeField]
        private List<FormulaCoefficient> formulaCoefficients = new();

        public StatDefinition Stat => stat;
        public AnimationCurve BaseValues => baseValues;
        public AnimationCurve GrowthValues => growthValues;
        public AnimationCurve SoftCapCurve => softCapCurve;
        public Vector2 JitterVariance => jitterVariance;
        public JrpgFormulaTemplate FormulaTemplate => formulaTemplate;
        public IReadOnlyList<FormulaCoefficient> FormulaCoefficients => formulaCoefficients;

        public float EvaluateBaseValue(float level)
        {
            return baseValues != null ? baseValues.Evaluate(level) : 0f;
        }

        public float EvaluateGrowthValue(float level)
        {
            return growthValues != null ? growthValues.Evaluate(level) : 0f;
        }

        public float EvaluateSoftCap(float level)
        {
            if (softCapCurve == null || softCapCurve.length == 0)
            {
                return float.PositiveInfinity;
            }

            return softCapCurve.Evaluate(level);
        }

        public float EvaluateTotalValue(float level)
        {
            var total = EvaluateBaseValue(level) + EvaluateGrowthValue(level);
            if (softCapCurve != null && softCapCurve.length > 0)
            {
                var cap = softCapCurve.Evaluate(level);
                if (cap > 0f)
                {
                    total = Mathf.Min(total, cap);
                }
            }

            return total;
        }

        public float EvaluateJitteredValue(float level, float normalizedRandom)
        {
            var total = EvaluateTotalValue(level);
            var min = Mathf.Min(jitterVariance.x, jitterVariance.y);
            var max = Mathf.Max(jitterVariance.x, jitterVariance.y);

            if (Mathf.Approximately(min, 0f) && Mathf.Approximately(max, 0f))
            {
                return total;
            }

            var jitter = Mathf.Lerp(min, max, Mathf.Clamp01(normalizedRandom));
            return total + jitter;
        }

#if UNITY_EDITOR
        internal void EnsureEditorConsistency()
        {
            if (formulaCoefficients == null)
            {
                formulaCoefficients = new List<FormulaCoefficient>();
            }

            JrpgFormulaTemplateLibrary.SynchronizeCoefficients(formulaTemplate, formulaCoefficients);

            if (jitterVariance.x > jitterVariance.y)
            {
                var min = jitterVariance.y;
                var max = jitterVariance.x;
                jitterVariance = new Vector2(min, max);
            }
        }
#endif
    }

    public enum JrpgFormulaTemplate
    {
        Linear,
        Quadratic,
        Exponential,
        AttackDefenseRatio,
        MagicSpiritRatio,
        SpeedWeightRatio
    }

    [Serializable]
    public class FormulaCoefficient
    {
        [SerializeField]
        private string key;

        [SerializeField]
        private float value;

        public string Key => key;

        public float Value
        {
            get => value;
            set => this.value = value;
        }

#if UNITY_EDITOR
        internal void SetKey(string newKey)
        {
            key = newKey;
        }
#endif
    }

    public static class JrpgFormulaTemplateLibrary
    {
        private static readonly Dictionary<JrpgFormulaTemplate, string[]> TemplateCoefficients = new()
        {
            { JrpgFormulaTemplate.Linear, new[] { "Slope", "Intercept" } },
            { JrpgFormulaTemplate.Quadratic, new[] { "A", "B", "C" } },
            { JrpgFormulaTemplate.Exponential, new[] { "Base", "Exponent", "Scale" } },
            { JrpgFormulaTemplate.AttackDefenseRatio, new[] { "AttackWeight", "DefenseWeight", "BaseMultiplier" } },
            { JrpgFormulaTemplate.MagicSpiritRatio, new[] { "MagicWeight", "SpiritWeight", "BaseMultiplier" } },
            { JrpgFormulaTemplate.SpeedWeightRatio, new[] { "SpeedWeight", "WeightPenalty", "Minimum" } }
        };

        private static readonly Dictionary<JrpgFormulaTemplate, string> TemplateDescriptions = new()
        {
            { JrpgFormulaTemplate.Linear, "Linear progression with slope and intercept." },
            { JrpgFormulaTemplate.Quadratic, "Quadratic curve for accelerated early or late growth." },
            { JrpgFormulaTemplate.Exponential, "Exponential scaling useful for late game stats." },
            { JrpgFormulaTemplate.AttackDefenseRatio, "Classic ATK vs DEF ratio balancing physical damage." },
            { JrpgFormulaTemplate.MagicSpiritRatio, "MAG vs SPR ratio used for spell potency." },
            { JrpgFormulaTemplate.SpeedWeightRatio, "SPD vs weight to drive turn order adjustments." }
        };

        public static IReadOnlyList<string> GetCoefficientKeys(JrpgFormulaTemplate template)
        {
            return TemplateCoefficients.TryGetValue(template, out var keys) ? keys : Array.Empty<string>();
        }

        public static string GetTemplateDescription(JrpgFormulaTemplate template)
        {
            return TemplateDescriptions.TryGetValue(template, out var description) ? description : string.Empty;
        }

#if UNITY_EDITOR
        internal static void SynchronizeCoefficients(JrpgFormulaTemplate template, List<FormulaCoefficient> coefficients)
        {
            if (coefficients == null)
            {
                return;
            }

            if (!TemplateCoefficients.TryGetValue(template, out var expected) || expected == null)
            {
                coefficients.Clear();
                return;
            }

            var updated = new List<FormulaCoefficient>(expected.Length);
            foreach (var key in expected)
            {
                FormulaCoefficient match = null;
                for (var i = 0; i < coefficients.Count; i++)
                {
                    var candidate = coefficients[i];
                    if (candidate != null && candidate.Key == key)
                    {
                        match = candidate;
                        break;
                    }
                }

                if (match == null)
                {
                    match = new FormulaCoefficient();
                }

                match.SetKey(key);
                updated.Add(match);
            }

            coefficients.Clear();
            coefficients.AddRange(updated);
        }
#endif
    }
}
