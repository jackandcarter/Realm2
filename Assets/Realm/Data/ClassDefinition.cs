using System;
using System.Collections.Generic;
using UnityEngine;

namespace Realm.Data
{
    [CreateAssetMenu(menuName = "Realm/Classes/Class Definition", fileName = "ClassDefinition")]
    public class ClassDefinition : ScriptableObject, IGuidIdentified
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

        [SerializeField, Tooltip("Base stat values evaluated by level for this class.")]
        private List<ClassStatCurve> baseStatCurves = new();

        [SerializeField, Tooltip("Growth modifiers that apply after the base stat curve is evaluated.")]
        private List<ClassStatCurve> growthModifiers = new();

        [SerializeField, Tooltip("Abilities available to the class at creation.")]
        private List<AbilityDefinition> startingAbilities = new();

        [SerializeField, Tooltip("Abilities learned as the class progresses.")]
        private List<AbilityDefinition> unlockableAbilities = new();

        public string Guid => guid;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public IReadOnlyList<StatCategory> StatCategories => statCategories;
        public IReadOnlyList<ClassStatCurve> BaseStatCurves => baseStatCurves;
        public IReadOnlyList<ClassStatCurve> GrowthModifiers => growthModifiers;
        public IReadOnlyList<AbilityDefinition> StartingAbilities => startingAbilities;
        public IReadOnlyList<AbilityDefinition> UnlockableAbilities => unlockableAbilities;

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

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(guid))
            {
                guid = Guid.NewGuid().ToString("N");
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }

            displayName = string.IsNullOrWhiteSpace(displayName) ? name : displayName.Trim();
            description = description?.Trim();

            RemoveNullsAndDuplicates(statCategories);
            RemoveNullCurves(baseStatCurves);
            RemoveNullCurves(growthModifiers);
            RemoveNullsAndDuplicates(startingAbilities);
            RemoveNullsAndDuplicates(unlockableAbilities);

#if UNITY_EDITOR
            SynchronizeCurveData(baseStatCurves);
            SynchronizeCurveData(growthModifiers);
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
