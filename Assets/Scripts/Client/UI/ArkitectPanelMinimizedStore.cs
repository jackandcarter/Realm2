using System;
using System.Collections.Generic;
using UnityEngine;

namespace Client.UI
{
    public static class ArkitectPanelMinimizedStore
    {
        private const string PlayerPrefKey = "arkitect-minimized-panels";

        private static MinimizedCache _cache;
        private static bool _loaded;

        [Serializable]
        private class MinimizedCache
        {
            public List<string> PanelIds = new();
        }

        public static IReadOnlyList<string> GetMinimizedPanels()
        {
            EnsureLoaded();

            if (_cache.PanelIds == null || _cache.PanelIds.Count == 0)
            {
                return Array.Empty<string>();
            }

            return new List<string>(_cache.PanelIds);
        }

        public static void SaveMinimizedPanels(IEnumerable<string> panelIds)
        {
            EnsureLoaded();
            _cache.PanelIds = panelIds != null ? new List<string>(panelIds) : new List<string>();
            Persist();
        }

        private static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            _cache = new MinimizedCache();
            if (PlayerPrefs.HasKey(PlayerPrefKey))
            {
                var raw = PlayerPrefs.GetString(PlayerPrefKey, string.Empty);
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    try
                    {
                        var parsed = JsonUtility.FromJson<MinimizedCache>(raw);
                        if (parsed?.PanelIds != null)
                        {
                            _cache.PanelIds = parsed.PanelIds;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"ArkitectPanelMinimizedStore failed to parse cached panels: {ex.Message}");
                    }
                }
            }

            _cache.PanelIds ??= new List<string>();
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
                Debug.LogWarning($"ArkitectPanelMinimizedStore failed to persist panels: {ex.Message}");
            }
        }
    }
}
