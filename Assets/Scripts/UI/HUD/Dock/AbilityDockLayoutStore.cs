using System;
using System.Collections.Generic;
using Client.UI.Dock;

namespace Client.UI.HUD.Dock
{
    public static class AbilityDockLayoutStore
    {
        public static IReadOnlyList<string> GetLayout(string classId, IReadOnlyList<string> defaultOrder)
        {
            var fallback = defaultOrder != null ? new List<string>(defaultOrder).ToArray() : Array.Empty<string>();
            return DockLayoutRepository.GetAbilityLayout(classId, fallback);
        }

        public static void SaveLayout(string classId, IReadOnlyList<string> abilityOrder)
        {
            var order = abilityOrder != null ? new List<string>(abilityOrder).ToArray() : Array.Empty<string>();
            DockLayoutRepository.SaveAbilityLayout(classId, order);
        }

        public static void Clear(string classId)
        {
            DockLayoutRepository.SaveAbilityLayout(classId, Array.Empty<string>());
        }

        public static void ClearAll()
        {
            DockLayoutRepository.ClearCache();
        }
    }
}
