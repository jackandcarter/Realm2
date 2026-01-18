using System;
using System.Collections.Generic;
using Realm.Data;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Client.Inventory
{
    public readonly struct InventoryItemPresentation
    {
        public InventoryItemPresentation(UnityObject definition, string itemId, string displayName, string description, Sprite icon)
        {
            Definition = definition;
            ItemId = itemId;
            DisplayName = displayName;
            Description = description;
            Icon = icon;
        }

        public UnityObject Definition { get; }
        public string ItemId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public Sprite Icon { get; }
    }

    public static class ItemCatalog
    {
        private const string ItemResourcePath = "Items";
        private static bool _loaded;
        private static readonly Dictionary<string, InventoryItemPresentation> Lookup =
            new(StringComparer.OrdinalIgnoreCase);

        public static bool TryGetItem(string itemId, out InventoryItemPresentation presentation)
        {
            EnsureLoaded();

            if (string.IsNullOrWhiteSpace(itemId))
            {
                presentation = default;
                return false;
            }

            return Lookup.TryGetValue(itemId.Trim(), out presentation);
        }

        private static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            Lookup.Clear();

            var items = Resources.LoadAll<ItemDefinition>(ItemResourcePath);
            if (items == null || items.Length == 0)
            {
                return;
            }

            foreach (var item in items)
            {
                if (item == null)
                {
                    continue;
                }

                var itemId = item.ItemId;
                if (string.IsNullOrWhiteSpace(itemId) || Lookup.ContainsKey(itemId))
                {
                    continue;
                }

                Lookup[itemId] = new InventoryItemPresentation(
                    item,
                    itemId,
                    item.DisplayName,
                    item.Description,
                    item.InventoryIcon);
            }
        }
    }
}
