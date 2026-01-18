using UnityEngine;

namespace Realm.Data
{
    [CreateAssetMenu(menuName = "Realm/Items/Item Definition", fileName = "ItemDefinition")]
    public class ItemDefinition : ConfigurationAsset, IGuidIdentified
    {
        [SerializeField, Tooltip("Stable unique id used by inventory records.")]
        private string itemId;

        [SerializeField, Tooltip("Human readable name shown in UI.")]
        private string displayName;

        [SerializeField, Tooltip("Description to present in tooltips or codex entries."), TextArea(2, 5)]
        private string description;

        [SerializeField, Tooltip("Icon used in inventory lists and tooltips.")]
        private Sprite inventoryIcon;

        public string Guid => ItemId;
        public string ItemId => string.IsNullOrWhiteSpace(itemId) ? name : itemId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description;
        public Sprite InventoryIcon => inventoryIcon;

        protected override void OnValidate()
        {
            base.OnValidate();

            if (string.IsNullOrWhiteSpace(itemId))
            {
                itemId = name;
            }

            itemId = itemId.Trim();
            displayName = string.IsNullOrWhiteSpace(displayName) ? name : displayName.Trim();
            description = description?.Trim();
        }
    }
}
