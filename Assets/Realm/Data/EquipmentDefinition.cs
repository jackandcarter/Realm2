using System.Collections.Generic;
using UnityEngine;

namespace Realm.Data
{
    public abstract class EquipmentDefinition : ConfigurationAsset, IGuidIdentified
    {
        [SerializeField, Tooltip("Stable unique identifier for this equipment. Auto-generated if empty.")]
        private string guid;

        [SerializeField, Tooltip("Human readable name shown in UI.")]
        private string displayName;

        [SerializeField, Tooltip("Description to present in tooltips or codex entries."), TextArea(2, 5)]
        private string description;

        [SerializeField, Tooltip("Optional sprite displayed alongside the equipment.")]
        private Sprite icon;

        [SerializeField, Tooltip("Inventory slot this equipment occupies.")]
        private EquipmentSlot slot = EquipmentSlot.Weapon;

        [SerializeField, Tooltip("Optional class restrictions for equipping this item.")]
        private List<ClassDefinition> requiredClasses = new();

        public string Guid => guid;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public EquipmentSlot Slot => slot;
        public IReadOnlyList<ClassDefinition> RequiredClasses => requiredClasses;

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
