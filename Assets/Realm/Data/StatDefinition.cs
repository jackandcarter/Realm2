using System;
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

        public string Guid => guid;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;

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
        }
    }
}
