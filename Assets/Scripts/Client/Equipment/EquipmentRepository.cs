using System;
using System.Collections;
using System.Collections.Generic;
using Client.Progression;
using UnityEngine;

namespace Client.Equipment
{
    public static class EquipmentRepository
    {
        private static readonly Dictionary<string, CharacterEquipmentEntry[]> EquipmentByCharacter =
            new(StringComparer.OrdinalIgnoreCase);

        private static CharacterProgressionClient _progressionClient;

        public static event Action<string, CharacterEquipmentEntry[]> EquipmentChanged;

        public static void SetProgressionClient(CharacterProgressionClient client)
        {
            _progressionClient = client;
        }

        public static CharacterEquipmentEntry[] GetEquipmentEntries(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || !EquipmentByCharacter.TryGetValue(characterId, out var items))
            {
                return Array.Empty<CharacterEquipmentEntry>();
            }

            return CloneEntries(items);
        }

        public static CharacterEquipmentEntry[] GetEquipmentForClass(string characterId, string classId)
        {
            if (string.IsNullOrWhiteSpace(classId))
            {
                return Array.Empty<CharacterEquipmentEntry>();
            }

            var entries = GetEquipmentEntries(characterId);
            if (entries.Length == 0)
            {
                return entries;
            }

            var normalized = classId.Trim();
            var results = new List<CharacterEquipmentEntry>();
            foreach (var entry in entries)
            {
                if (entry == null)
                {
                    continue;
                }

                if (string.Equals(entry.classId, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(CloneEntry(entry));
                }
            }

            return results.ToArray();
        }

        public static IEnumerator ReplaceEquipmentAsync(
            string characterId,
            CharacterEquipmentEntry[] items,
            Action<bool> onSuccess,
            Action<ApiError> onError)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                onSuccess?.Invoke(false);
                yield break;
            }

            var sanitized = Sanitize(items);

            if (_progressionClient == null)
            {
                UnityEngine.Debug.LogWarning(
                    "Equipment updates are server-authoritative. Configure a progression client before requesting updates.");
                onSuccess?.Invoke(false);
                yield break;
            }

            var expectedVersion = CharacterProgressionCache.GetEquipmentVersion(characterId);

            CharacterProgressionEnvelope response = null;
            ApiError error = null;

            yield return _progressionClient.UpdateEquipment(
                characterId,
                sanitized,
                expectedVersion,
                payload => response = payload,
                apiError => error = apiError);

            if (error != null)
            {
                onError?.Invoke(error);
                yield break;
            }

            if (response != null)
            {
                ApplySnapshot(characterId, response);
            }

            onSuccess?.Invoke(true);
        }

        public static IEnumerator ReplaceClassEquipmentAsync(
            string characterId,
            string classId,
            CharacterEquipmentEntry[] classEntries,
            Action<bool> onSuccess,
            Action<ApiError> onError)
        {
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(classId))
            {
                onSuccess?.Invoke(false);
                yield break;
            }

            var sanitized = Sanitize(classEntries, classId.Trim());
            var existing = GetEquipmentEntries(characterId);
            var merged = MergeClassEntries(existing, classId.Trim(), sanitized);
            yield return ReplaceEquipmentAsync(characterId, merged, onSuccess, onError);
        }

        public static void ApplySnapshot(string characterId, CharacterProgressionEnvelope snapshot)
        {
            if (string.IsNullOrWhiteSpace(characterId) || snapshot == null)
            {
                return;
            }

            CharacterProgressionCache.Store(characterId, snapshot);
            ApplyEquipmentState(characterId, snapshot.equipment);
        }

        public static void ApplyEquipmentState(string characterId, CharacterEquipmentCollection equipment)
        {
            if (string.IsNullOrWhiteSpace(characterId) || equipment?.items == null)
            {
                return;
            }

            UpdateLocalEquipment(characterId, equipment.items);
        }

        private static void UpdateLocalEquipment(string characterId, CharacterEquipmentEntry[] items)
        {
            EquipmentByCharacter[characterId] = CloneEntries(items);
            EquipmentChanged?.Invoke(characterId, CloneEntries(items));
        }

        private static CharacterEquipmentEntry[] MergeClassEntries(
            CharacterEquipmentEntry[] existing,
            string classId,
            CharacterEquipmentEntry[] classEntries)
        {
            var merged = new List<CharacterEquipmentEntry>();
            if (existing != null)
            {
                foreach (var entry in existing)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    if (!string.Equals(entry.classId, classId, StringComparison.OrdinalIgnoreCase))
                    {
                        merged.Add(CloneEntry(entry));
                    }
                }
            }

            if (classEntries != null)
            {
                foreach (var entry in classEntries)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    merged.Add(CloneEntry(entry));
                }
            }

            return merged.ToArray();
        }

        private static CharacterEquipmentEntry[] Sanitize(CharacterEquipmentEntry[] items, string forceClassId = null)
        {
            if (items == null || items.Length == 0)
            {
                return Array.Empty<CharacterEquipmentEntry>();
            }

            var sanitized = new List<CharacterEquipmentEntry>();
            foreach (var item in items)
            {
                var classId = forceClassId ?? item?.classId;
                if (string.IsNullOrWhiteSpace(classId) ||
                    string.IsNullOrWhiteSpace(item?.slot) ||
                    string.IsNullOrWhiteSpace(item?.itemId))
                {
                    continue;
                }

                sanitized.Add(new CharacterEquipmentEntry
                {
                    classId = classId.Trim(),
                    slot = item.slot.Trim(),
                    itemId = item.itemId.Trim(),
                    metadataJson = string.IsNullOrWhiteSpace(item.metadataJson) ? "{}" : item.metadataJson.Trim()
                });
            }

            return sanitized.ToArray();
        }

        private static CharacterEquipmentEntry[] CloneEntries(CharacterEquipmentEntry[] items)
        {
            if (items == null || items.Length == 0)
            {
                return Array.Empty<CharacterEquipmentEntry>();
            }

            var clone = new CharacterEquipmentEntry[items.Length];
            for (var i = 0; i < items.Length; i++)
            {
                clone[i] = CloneEntry(items[i]);
            }

            return clone;
        }

        private static CharacterEquipmentEntry CloneEntry(CharacterEquipmentEntry entry)
        {
            return entry == null
                ? null
                : new CharacterEquipmentEntry
                {
                    classId = entry.classId,
                    slot = entry.slot,
                    itemId = entry.itemId,
                    metadataJson = entry.metadataJson
                };
        }
    }
}
