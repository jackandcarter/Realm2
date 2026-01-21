using System;
using System.Collections.Generic;
using Client.UI.Dock;

namespace Client.UI.HUD.Dock
{
    public static class DockShortcutLayoutStore
    {
        public static IReadOnlyList<string> GetLayout(IReadOnlyList<string> defaultOrder)
        {
            var fallback = defaultOrder != null ? new List<string>(defaultOrder).ToArray() : Array.Empty<string>();
            return DockLayoutRepository.GetShortcutLayout(null, fallback);
        }

        public static IReadOnlyList<string> GetLayout(string layoutKey, IReadOnlyList<string> defaultOrder)
        {
            var fallback = defaultOrder != null ? new List<string>(defaultOrder).ToArray() : Array.Empty<string>();
            return DockLayoutRepository.GetShortcutLayout(layoutKey, fallback);
        }

        public static void SaveLayout(IReadOnlyList<string> shortcutOrder)
        {
            var order = shortcutOrder != null ? new List<string>(shortcutOrder).ToArray() : Array.Empty<string>();
            DockLayoutRepository.SaveShortcutLayout(null, order);
        }

        public static void SaveLayout(string layoutKey, IReadOnlyList<string> shortcutOrder)
        {
            var order = shortcutOrder != null ? new List<string>(shortcutOrder).ToArray() : Array.Empty<string>();
            DockLayoutRepository.SaveShortcutLayout(layoutKey, order);
        }

        public static void Clear()
        {
            DockLayoutRepository.SaveShortcutLayout(null, Array.Empty<string>());
        }
    }
}
