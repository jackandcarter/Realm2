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

        [SerializeField, Tooltip("Icon used in inventory lists and tooltips. Falls back to the main icon if empty.")]
        private Sprite inventoryIcon;

        [SerializeField, Tooltip("Icon used in equipment and ability docks. Falls back to the inventory icon if empty.")]
        private Sprite dockIcon;

        [SerializeField, Tooltip("Inventory slot this equipment occupies.")]
        private EquipmentSlot slot = EquipmentSlot.Weapon;

        [SerializeField, Tooltip("Explicit class ids permitted to equip this item. Leave empty to allow all classes.")]
        private List<string> requiredClassIds = new();

        [SerializeField, Tooltip("Optional class restrictions for equipping this item.")]
        private List<ClassDefinition> requiredClasses = new();

        [SerializeField, Tooltip("Behaviors or effects applied while this equipment is equipped.")]
        private List<EquipmentEquipEffect> equipEffects = new();

        public string Guid => guid;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public Sprite InventoryIcon => inventoryIcon != null ? inventoryIcon : icon;
        public Sprite DockIcon => dockIcon != null ? dockIcon : InventoryIcon;
        public EquipmentSlot Slot => slot;
        public IReadOnlyList<string> RequiredClassIds => requiredClassIds;
        public IReadOnlyList<ClassDefinition> RequiredClasses => requiredClasses;
        public IReadOnlyList<EquipmentEquipEffect> EquipEffects => equipEffects;

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
            NormalizeRequiredClassIds();
        }

        private void NormalizeRequiredClassIds()
        {
            var normalized = new List<string>();

            if (requiredClassIds != null)
            {
                foreach (var entry in requiredClassIds)
                {
                    var trimmed = string.IsNullOrWhiteSpace(entry) ? null : entry.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed) && !ContainsIgnoreCase(normalized, trimmed))
                    {
                        normalized.Add(trimmed);
                    }
                }
            }

            if (requiredClasses != null)
            {
                foreach (var classDefinition in requiredClasses)
                {
                    var classId = classDefinition != null ? classDefinition.ClassId : null;
                    if (!string.IsNullOrWhiteSpace(classId) && !ContainsIgnoreCase(normalized, classId))
                    {
                        normalized.Add(classId);
                    }
                }
            }

            requiredClassIds = normalized;
        }

        private static bool ContainsIgnoreCase(List<string> entries, string value)
        {
            if (entries == null || string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i], value, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
