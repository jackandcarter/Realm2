using System;
using System.Collections.Generic;
using UnityEngine;

namespace Client.UI.HUD.Dock
{
    public static class DockShortcutLayoutStore
    {
        private const string PlayerPrefKey = "dock-shortcuts";

        private static LayoutCache _cache;
        private static bool _loaded;

        [Serializable]
        private class LayoutCache
        {
            public List<string> ShortcutIds = new();
        }

        public static IReadOnlyList<string> GetLayout(IReadOnlyList<string> defaultOrder)
        {
            EnsureLoaded();

            if (_cache.ShortcutIds == null || _cache.ShortcutIds.Count == 0)
            {
                return defaultOrder ?? Array.Empty<string>();
            }

            return new List<string>(_cache.ShortcutIds);
        }

        public static void SaveLayout(IReadOnlyList<string> shortcutOrder)
        {
            EnsureLoaded();

            _cache.ShortcutIds = shortcutOrder != null
                ? new List<string>(shortcutOrder)
                : new List<string>();

            Persist();
        }

        public static void Clear()
        {
            EnsureLoaded();
            _cache.ShortcutIds.Clear();
            PlayerPrefs.DeleteKey(PlayerPrefKey);
            PlayerPrefs.Save();
        }

        private static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            _cache = new LayoutCache();
            if (PlayerPrefs.HasKey(PlayerPrefKey))
            {
                var raw = PlayerPrefs.GetString(PlayerPrefKey, string.Empty);
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    try
                    {
                        var parsed = JsonUtility.FromJson<LayoutCache>(raw);
                        if (parsed?.ShortcutIds != null)
                        {
                            _cache.ShortcutIds = parsed.ShortcutIds;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"DockShortcutLayoutStore failed to parse cached shortcuts: {ex.Message}");
                    }
                }
            }

            _cache.ShortcutIds ??= new List<string>();
            _loaded = true;
        }

        private static void Persist()
        {
            if (_cache == null)
            {
                return;
            }

            try
            {
                var json = JsonUtility.ToJson(_cache);
                PlayerPrefs.SetString(PlayerPrefKey, json);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"DockShortcutLayoutStore failed to persist shortcuts: {ex.Message}");
            }
        }
    }
}
