using System;
using System.Collections.Generic;
using UnityEngine;

namespace Realm.Data
{
    [CreateAssetMenu(menuName = "Realm/Stats/Stat Profile", fileName = "StatProfile")]
    public class StatProfileDefinition : ConfigurationAsset, IGuidIdentified
    {
        [SerializeField, Tooltip("Stable unique identifier for this stat profile. Auto-generated if empty.")]
        private string guid;

        [SerializeField]
        private string displayName;

        [SerializeField, TextArea(2, 5)]
        private string description;

        [SerializeField, Tooltip("Collection of stat curves describing how the class progresses with this profile.")]
        private List<ClassStatCurve> statCurves = new();

        public string Guid => guid;
        public string DisplayName => displayName;
        public string Description => description;
        public IReadOnlyList<ClassStatCurve> StatCurves => statCurves;

        public ClassStatCurve FindCurve(StatDefinition stat)
        {
            if (statCurves == null || stat == null)
            {
                return null;
            }

            for (var i = 0; i < statCurves.Count; i++)
            {
                var curve = statCurves[i];
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

            RemoveNullCurves(statCurves);
#if UNITY_EDITOR
            SynchronizeCurves(statCurves);
#endif
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
                var entry = curves[i];
                if (entry == null || entry.Stat == null || !seen.Add(entry.Stat))
                {
                    curves.RemoveAt(i);
                }
            }
        }

#if UNITY_EDITOR
        private static void SynchronizeCurves(List<ClassStatCurve> curves)
        {
            if (curves == null)
            {
                return;
            }

            foreach (var curve in curves)
            {
                curve?.EnsureEditorConsistency();
            }
        }
#endif
    }
}
