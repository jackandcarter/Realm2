using System;
using System.Collections.Generic;
using Client.Inventory;
using Client.Player;
using Client.Progression;
using Realm.UI.Tooltips;
using UnityEngine;
using UnityEngine.UI;

namespace Client.UI.HUD.Inventory
{
    [DisallowMultipleComponent]
    public class InventoryPanelController : MonoBehaviour
    {
        [Header("Inventory Slots")]
        [SerializeField] private InventorySlotView[] slots;
        [SerializeField] private Transform slotRoot;
        [SerializeField] private Sprite fallbackIcon;

        [Header("Tooltip")]
        [SerializeField] private CombatTooltipBindings tooltipBindings;

        [Header("Panel")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private bool startOpen;

        private bool _isOpen;

        private void Awake()
        {
            ResolveSlots();
            ResolveTooltipBindings();
            SetOpen(startOpen, true);
        }

        private void OnEnable()
        {
            PlayerInventoryStateManager.InventoryChanged += OnInventoryChanged;
            Refresh();
        }

        private void OnDisable()
        {
            PlayerInventoryStateManager.InventoryChanged -= OnInventoryChanged;
        }

        public void ToggleOpen()
        {
            SetOpen(!_isOpen, false);
        }

        public void SetOpen(bool open)
        {
            SetOpen(open, false);
        }

        private void SetOpen(bool open, bool force)
        {
            if (!force && _isOpen == open)
            {
                return;
            }

            _isOpen = open;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = open ? 1f : 0f;
                canvasGroup.interactable = open;
                canvasGroup.blocksRaycasts = open;
            }
            else
            {
                gameObject.SetActive(open);
            }

            if (open)
            {
                Refresh();
            }
        }

        private void OnInventoryChanged(string characterId, CharacterInventoryItemEntry[] items)
        {
            if (!string.IsNullOrWhiteSpace(characterId) && _isOpen)
            {
                Refresh(items);
            }
        }

        private void Refresh()
        {
            Refresh(PlayerInventoryStateManager.GetCurrentInventory());
        }

        private void Refresh(CharacterInventoryItemEntry[] items)
        {
            ResolveSlots();

            if (slots == null || slots.Length == 0)
            {
                return;
            }

            var normalized = NormalizeItems(items);
            for (var i = 0; i < slots.Length; i++)
            {
                if (i >= normalized.Count)
                {
                    slots[i].Clear(tooltipBindings);
                    continue;
                }

                var entry = normalized[i];
                var icon = entry.Icon != null ? entry.Icon : fallbackIcon;
                slots[i].SetItem(icon, entry.Quantity, entry.Definition, tooltipBindings);
            }
        }

        private List<InventoryDisplayEntry> NormalizeItems(CharacterInventoryItemEntry[] items)
        {
            var results = new List<InventoryDisplayEntry>();
            if (items == null || items.Length == 0)
            {
                return results;
            }

            foreach (var item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.itemId) || item.quantity <= 0)
                {
                    continue;
                }

                if (ItemCatalog.TryGetItem(item.itemId, out var presentation))
                {
                    results.Add(new InventoryDisplayEntry(item.quantity, presentation.Definition, presentation.Icon));
                }
                else
                {
                    results.Add(new InventoryDisplayEntry(item.quantity, null, fallbackIcon));
                }
            }

            return results;
        }

        private void ResolveSlots()
        {
            if (slots != null && slots.Length > 0)
            {
                return;
            }

            if (slotRoot == null)
            {
                slotRoot = transform;
            }

            slots = slotRoot.GetComponentsInChildren<InventorySlotView>(true);
        }

        private void ResolveTooltipBindings()
        {
            if (tooltipBindings != null)
            {
                return;
            }

            tooltipBindings = FindFirstObjectByType<CombatTooltipBindings>(FindObjectsInactive.Include);
        }

        private readonly struct InventoryDisplayEntry
        {
            public InventoryDisplayEntry(int quantity, UnityEngine.Object definition, Sprite icon)
            {
                Quantity = quantity;
                Definition = definition;
                Icon = icon;
            }

            public int Quantity { get; }
            public UnityEngine.Object Definition { get; }
            public Sprite Icon { get; }
        }
    }
}
