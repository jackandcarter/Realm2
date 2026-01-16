using System;
using System.Collections.Generic;
using UnityEngine;

namespace Realm.Data
{
    [CreateAssetMenu(menuName = "Realm/Stats/Stat Definition", fileName = "StatDefinition")]
    public class StatDefinition : ConfigurationAsset, IGuidIdentified
    {
        [SerializeField, Tooltip("Stable unique identifier for this stat. Auto-generated if empty.")]
        private string guid;

        [SerializeField, Tooltip("Human readable name shown in UI.")]
        private string displayName;

        [SerializeField, Tooltip("Description to present in tooltips or codex entries."), TextArea(2, 5)]
        private string description;

        [SerializeField, Tooltip("Optional sprite displayed alongside the stat.")]
        private Sprite icon;

        [SerializeField, Tooltip("Derived stat ratios that combine other stats into this stat.")]
        private List<StatRatioDefinition> ratios = new();

        public string Guid => guid;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public IReadOnlyList<StatRatioDefinition> Ratios => ratios;

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

            if (ratios == null)
            {
                ratios = new List<StatRatioDefinition>();
            }

            for (var i = ratios.Count - 1; i >= 0; i--)
            {
                var ratio = ratios[i];
                if (ratio == null || ratio.SourceStat == null)
                {
                    ratios.RemoveAt(i);
                }
            }
        }
    }

    [Serializable]
    public class StatRatioDefinition
    {
        [SerializeField] private StatDefinition sourceStat;
        [SerializeField] private float ratio = 1f;

        public StatDefinition SourceStat => sourceStat;
        public float Ratio => ratio;
    }
}
