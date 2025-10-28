using System;
using System.Collections.Generic;
using UnityEngine;

namespace Realm.Data
{
    [CreateAssetMenu(menuName = "Realm/Stats/Stat Category", fileName = "StatCategory")]
    public class StatCategory : ScriptableObject, IGuidIdentified
    {
        [SerializeField, Tooltip("Stable unique identifier for this category. Auto-generated if empty.")]
        private string guid;

        [SerializeField]
        private string displayName;

        [SerializeField, TextArea(2, 5)]
        private string description;

        [SerializeField, Tooltip("Optional color accent for UI highlighting.")]
        private Color accentColor = Color.white;

        [SerializeField, Tooltip("Collection of stats that belong to this category.")]
        private List<StatDefinition> stats = new();

        public string Guid => guid;
        public string DisplayName => displayName;
        public string Description => description;
        public Color AccentColor => accentColor;
        public IReadOnlyList<StatDefinition> Stats => stats;

        public bool ContainsStat(StatDefinition stat)
        {
            return stat != null && stats.Contains(stat);
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
            RemoveNullAndDuplicateStats();
        }

        private void RemoveNullAndDuplicateStats()
        {
            if (stats == null)
            {
                stats = new List<StatDefinition>();
                return;
            }

            var seen = new HashSet<StatDefinition>();
            for (var i = stats.Count - 1; i >= 0; i--)
            {
                var entry = stats[i];
                if (entry == null || !seen.Add(entry))
                {
                    stats.RemoveAt(i);
                }
            }
        }
    }
}
