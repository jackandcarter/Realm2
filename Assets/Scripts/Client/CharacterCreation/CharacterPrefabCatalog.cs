using System;
using System.Collections.Generic;
using UnityEngine;

namespace Client.CharacterCreation
{
    [CreateAssetMenu(menuName = "Character Creation/Character Prefab Catalog", fileName = "CharacterPrefabCatalog")]
    public class CharacterPrefabCatalog : ScriptableObject
    {
        [SerializeField] private GameObject fallbackPrefab;
        [SerializeField] private List<CharacterPrefabEntry> entries = new();

        public GameObject ResolvePrefab(string raceId, string classId)
        {
            var normalizedRace = string.IsNullOrWhiteSpace(raceId) ? null : raceId.Trim();
            var normalizedClass = string.IsNullOrWhiteSpace(classId) ? null : classId.Trim();

            CharacterPrefabEntry defaultEntry = null;

            foreach (var entry in entries)
            {
                if (entry == null)
                {
                    continue;
                }

                if (entry.IsDefault)
                {
                    defaultEntry ??= entry;
                }

                if (entry.Matches(normalizedRace, normalizedClass))
                {
                    return entry.Prefab;
                }
            }

            foreach (var entry in entries)
            {
                if (entry == null)
                {
                    continue;
                }

                if (entry.Matches(normalizedRace, null) || entry.Matches(null, normalizedClass))
                {
                    return entry.Prefab;
                }
            }

            return defaultEntry?.Prefab ?? fallbackPrefab;
        }
    }

    [Serializable]
    public class CharacterPrefabEntry
    {
        [SerializeField] private string raceId;
        [SerializeField] private string classId;
        [SerializeField] private GameObject prefab;
        [SerializeField] private bool isDefault;

        public string RaceId => raceId;
        public string ClassId => classId;
        public GameObject Prefab => prefab;
        public bool IsDefault => isDefault;

        public bool Matches(string race, string classIdCandidate)
        {
            var raceMatches = string.IsNullOrWhiteSpace(raceId) || string.Equals(raceId.Trim(), race, StringComparison.OrdinalIgnoreCase);
            var classMatches = string.IsNullOrWhiteSpace(classId) || string.Equals(classId.Trim(), classIdCandidate, StringComparison.OrdinalIgnoreCase);
            return raceMatches && classMatches && prefab != null;
        }
    }
}
