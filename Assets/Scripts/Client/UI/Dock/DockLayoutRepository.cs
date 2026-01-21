using System;
using System.Collections.Generic;
using Client;
using Client.Progression;
using UnityEngine;

namespace Client.UI.Dock
{
    public static class DockLayoutRepository
    {
        private const string ShortcutLayoutKey = "dock-shortcuts";
        private const string AbilityLayoutPrefix = "ability-dock";

        private static readonly Dictionary<string, string[]> Cache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> PendingLoads = new(StringComparer.OrdinalIgnoreCase);
        private static DockLayoutClient _client;

        public static void SetClient(DockLayoutClient client)
        {
            _client = client;
            SessionManager.SelectedCharacterChanged -= HandleCharacterChanged;
            SessionManager.SelectedCharacterChanged += HandleCharacterChanged;
            SessionManager.SessionCleared -= HandleSessionCleared;
            SessionManager.SessionCleared += HandleSessionCleared;
        }

        public static string[] GetShortcutLayout(string layoutKey, string[] fallback)
        {
            var key = string.IsNullOrWhiteSpace(layoutKey)
                ? ShortcutLayoutKey
                : $"{ShortcutLayoutKey}:{layoutKey.Trim()}";
            return GetLayoutInternal(key, fallback ?? Array.Empty<string>());
        }

        public static string[] GetAbilityLayout(string classId, string[] fallback)
        {
            var key = string.IsNullOrWhiteSpace(classId)
                ? AbilityLayoutPrefix
                : $"{AbilityLayoutPrefix}:{classId.Trim()}";
            return GetLayoutInternal(key, fallback ?? Array.Empty<string>());
        }

        public static void SaveShortcutLayout(string layoutKey, string[] order)
        {
            var key = string.IsNullOrWhiteSpace(layoutKey)
                ? ShortcutLayoutKey
                : $"{ShortcutLayoutKey}:{layoutKey.Trim()}";
            SaveLayoutInternal(key, order ?? Array.Empty<string>());
        }

        public static void SaveAbilityLayout(string classId, string[] order)
        {
            if (string.IsNullOrWhiteSpace(classId))
            {
                return;
            }

            var key = $"{AbilityLayoutPrefix}:{classId.Trim()}";
            SaveLayoutInternal(key, order ?? Array.Empty<string>());
        }

        public static void ClearCache()
        {
            Cache.Clear();
            PendingLoads.Clear();
        }

        private static string[] GetLayoutInternal(string layoutKey, string[] fallback)
        {
            if (string.IsNullOrWhiteSpace(layoutKey))
            {
                return fallback;
            }

            if (Cache.TryGetValue(layoutKey, out var cached) && cached != null)
            {
                return cached;
            }

            RequestLayout(layoutKey);
            return fallback;
        }

        private static void SaveLayoutInternal(string layoutKey, string[] order)
        {
            if (string.IsNullOrWhiteSpace(layoutKey))
            {
                return;
            }

            Cache[layoutKey] = order ?? Array.Empty<string>();
            PersistLayout(layoutKey, order ?? Array.Empty<string>());
        }

        private static void RequestLayout(string layoutKey)
        {
            if (_client == null || PendingLoads.Contains(layoutKey))
            {
                return;
            }

            var characterId = SessionManager.SelectedCharacterId;
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return;
            }

            PendingLoads.Add(layoutKey);
            ProgressionCoroutineRunner.Run(
                _client.GetLayout(
                    characterId,
                    layoutKey,
                    snapshot =>
                    {
                        PendingLoads.Remove(layoutKey);
                        if (snapshot?.order != null)
                        {
                            Cache[layoutKey] = snapshot.order;
                        }
                    },
                    error =>
                    {
                        PendingLoads.Remove(layoutKey);
                        if (error != null)
                        {
                            Debug.LogWarning($"Failed to load dock layout {layoutKey}: {error.Message}");
                        }
                    }
                )
            );
        }

        private static void PersistLayout(string layoutKey, string[] order)
        {
            if (_client == null)
            {
                return;
            }

            var characterId = SessionManager.SelectedCharacterId;
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return;
            }

            var payload = new DockLayoutUpdateRequest
            {
                order = order
            };

            ProgressionCoroutineRunner.Run(
                _client.SaveLayout(
                    characterId,
                    layoutKey,
                    payload,
                    snapshot =>
                    {
                        if (snapshot?.order != null)
                        {
                            Cache[layoutKey] = snapshot.order;
                        }
                    },
                    error =>
                    {
                        if (error != null)
                        {
                            Debug.LogWarning($"Failed to save dock layout {layoutKey}: {error.Message}");
                        }
                    }
                )
            );
        }

        private static void HandleCharacterChanged(string characterId)
        {
            ClearCache();
        }

        private static void HandleSessionCleared()
        {
            ClearCache();
        }
    }
}
