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
        private static readonly Dictionary<string, LayoutCache> LayoutCaches = new(StringComparer.OrdinalIgnoreCase);

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

        public static IReadOnlyList<string> GetLayout(string layoutKey, IReadOnlyList<string> defaultOrder)
        {
            if (string.IsNullOrWhiteSpace(layoutKey))
            {
                return GetLayout(defaultOrder);
            }

            var cache = EnsureLoaded(layoutKey);
            if (cache.ShortcutIds == null || cache.ShortcutIds.Count == 0)
            {
                return defaultOrder ?? Array.Empty<string>();
            }

            return new List<string>(cache.ShortcutIds);
        }

        public static void SaveLayout(IReadOnlyList<string> shortcutOrder)
        {
            EnsureLoaded();

            _cache.ShortcutIds = shortcutOrder != null
                ? new List<string>(shortcutOrder)
                : new List<string>();

            Persist();
        }

        public static void SaveLayout(string layoutKey, IReadOnlyList<string> shortcutOrder)
        {
            if (string.IsNullOrWhiteSpace(layoutKey))
            {
                SaveLayout(shortcutOrder);
                return;
            }

            var cache = EnsureLoaded(layoutKey);
            cache.ShortcutIds = shortcutOrder != null
                ? new List<string>(shortcutOrder)
                : new List<string>();

            Persist(layoutKey, cache);
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

        private static LayoutCache EnsureLoaded(string layoutKey)
        {
            if (LayoutCaches.TryGetValue(layoutKey, out var cached))
            {
                return cached;
            }

            var key = $"{PlayerPrefKey}-{layoutKey}";
            var cache = new LayoutCache();
            if (PlayerPrefs.HasKey(key))
            {
                var raw = PlayerPrefs.GetString(key, string.Empty);
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    try
                    {
                        var parsed = JsonUtility.FromJson<LayoutCache>(raw);
                        if (parsed?.ShortcutIds != null)
                        {
                            cache.ShortcutIds = parsed.ShortcutIds;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"DockShortcutLayoutStore failed to parse cached shortcuts ({layoutKey}): {ex.Message}");
                    }
                }
            }

            cache.ShortcutIds ??= new List<string>();
            LayoutCaches[layoutKey] = cache;
            return cache;
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

        private static void Persist(string layoutKey, LayoutCache cache)
        {
            if (cache == null || string.IsNullOrWhiteSpace(layoutKey))
            {
                return;
            }

            try
            {
                var json = JsonUtility.ToJson(cache);
                PlayerPrefs.SetString($"{PlayerPrefKey}-{layoutKey}", json);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"DockShortcutLayoutStore failed to persist shortcuts ({layoutKey}): {ex.Message}");
            }
        }
    }
}
