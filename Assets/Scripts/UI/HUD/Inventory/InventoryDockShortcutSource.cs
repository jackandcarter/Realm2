using Client.UI.HUD.Dock;
using UnityEngine;

namespace Client.UI.HUD.Inventory
{
    [DisallowMultipleComponent]
    public class InventoryDockShortcutSource : MonoBehaviour, IDockShortcutSource
    {
        [SerializeField] private string shortcutId = "inventory";
        [SerializeField] private string displayName = "Inventory";
        [SerializeField] private Sprite icon;
        [SerializeField] private InventoryPanelController panelController;

        private DockShortcutEntry _entry;

        public DockShortcutEntry ShortcutEntry
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_entry.ShortcutId))
                {
                    _entry = new DockShortcutEntry(
                        shortcutId,
                        displayName,
                        icon,
                        new DockShortcutActionMetadata("inventory.toggle", shortcutId));
                }

                return _entry;
            }
        }

        private void Awake()
        {
            if (panelController == null)
            {
                panelController = GetComponentInChildren<InventoryPanelController>(true);
            }
        }

        public void ActivateDockShortcut()
        {
            if (panelController == null)
            {
                return;
            }

            panelController.ToggleOpen();
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(shortcutId))
            {
                shortcutId = "inventory";
            }

            shortcutId = shortcutId.Trim();
            displayName = string.IsNullOrWhiteSpace(displayName) ? "Inventory" : displayName.Trim();
            _entry = new DockShortcutEntry(
                shortcutId,
                displayName,
                icon,
                new DockShortcutActionMetadata("inventory.toggle", shortcutId));
        }
    }
}
