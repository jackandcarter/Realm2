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

        public StatDefinition Stat => stat;
        public AnimationCurve BaseValues => baseValues;
        public AnimationCurve GrowthValues => growthValues;

        public float EvaluateBaseValue(float level)
        {
            return baseValues != null ? baseValues.Evaluate(level) : 0f;
        }

        public float EvaluateGrowthValue(float level)
        {
            return growthValues != null ? growthValues.Evaluate(level) : 0f;
        }

        public float EvaluateTotalValue(float level)
        {
            return EvaluateBaseValue(level) + EvaluateGrowthValue(level);
        }
    }
}
