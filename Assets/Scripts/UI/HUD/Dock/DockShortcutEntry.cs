using System;
using UnityEngine;

namespace Client.UI.HUD.Dock
{
    [Serializable]
    public struct DockShortcutActionMetadata
    {
        public string ActionId;
        public string ActionPayload;

        public DockShortcutActionMetadata(string actionId, string actionPayload)
        {
            ActionId = actionId;
            ActionPayload = actionPayload;
        }
    }

    [Serializable]
    public struct DockShortcutEntry
    {
        public string ShortcutId;
        public string DisplayName;
        public Sprite Icon;
        public DockShortcutActionMetadata ActionMetadata;

        public DockShortcutEntry(string shortcutId, string displayName, Sprite icon, DockShortcutActionMetadata actionMetadata)
        {
            ShortcutId = shortcutId;
            DisplayName = displayName;
            Icon = icon;
            ActionMetadata = actionMetadata;
        }
    }
}
