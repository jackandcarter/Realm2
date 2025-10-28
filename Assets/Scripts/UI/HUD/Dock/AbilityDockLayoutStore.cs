using System;
using System.Collections.Generic;
using UnityEngine;

namespace Client.UI.HUD.Dock
{
    public static class AbilityDockLayoutStore
    {
        private const string PlayerPrefKey = "ui.dock.layouts";

        private static LayoutCache _cache;
        private static bool _loaded;

        [Serializable]
        private class LayoutCache
        {
            public List<ClassLayout> Layouts = new();
        }

        [Serializable]
        private class ClassLayout
        {
            public string ClassId;
            public List<string> AbilityIds = new();
        }

        public static IReadOnlyList<string> GetLayout(string classId, IReadOnlyList<string> defaultOrder)
        {
            EnsureLoaded();

            if (string.IsNullOrWhiteSpace(classId))
            {
                return defaultOrder ?? Array.Empty<string>();
            }

            var normalized = classId.Trim();
            foreach (var layout in _cache.Layouts)
            {
                if (!string.Equals(layout.ClassId, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (layout.AbilityIds == null || layout.AbilityIds.Count == 0)
                {
                    break;
                }

                return new List<string>(layout.AbilityIds);
            }

            return defaultOrder ?? Array.Empty<string>();
        }

        public static void SaveLayout(string classId, IReadOnlyList<string> abilityOrder)
        {
            EnsureLoaded();

            if (string.IsNullOrWhiteSpace(classId))
            {
                return;
            }

            var normalized = classId.Trim();
            var layout = FindOrCreateLayout(normalized);
            layout.AbilityIds = abilityOrder != null
                ? new List<string>(abilityOrder)
                : new List<string>();

            Persist();
        }

        public static void Clear(string classId)
        {
            EnsureLoaded();

            if (string.IsNullOrWhiteSpace(classId))
            {
                return;
            }

            var normalized = classId.Trim();
            _cache.Layouts.RemoveAll(l => string.Equals(l.ClassId, normalized, StringComparison.OrdinalIgnoreCase));
            Persist();
        }

        public static void ClearAll()
        {
            EnsureLoaded();
            _cache.Layouts.Clear();
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
                        if (parsed?.Layouts != null)
                        {
                            _cache.Layouts = parsed.Layouts;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"AbilityDockLayoutStore failed to parse cached layouts: {ex.Message}");
                    }
                }
            }

            _cache.Layouts ??= new List<ClassLayout>();
            _loaded = true;
        }

        private static ClassLayout FindOrCreateLayout(string classId)
        {
            foreach (var layout in _cache.Layouts)
            {
                if (string.Equals(layout.ClassId, classId, StringComparison.OrdinalIgnoreCase))
                {
                    return layout;
                }
            }

            var created = new ClassLayout
            {
                ClassId = classId,
                AbilityIds = new List<string>()
            };
            _cache.Layouts.Add(created);
            return created;
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
                Debug.LogWarning($"AbilityDockLayoutStore failed to persist layouts: {ex.Message}");
            }
        }
    }
}
