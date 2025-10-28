using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Client.CharacterCreation
{
    [CreateAssetMenu(menuName = "Realm/Class Ability Progression", fileName = "ClassAbilityProgression")]
    public class ClassAbilityProgression : ScriptableObject
    {
        [SerializeField]
        private List<ClassAbilityTrack> classAbilityTracks = new();

        private Dictionary<string, List<ClassAbilityLevelEntry>> _cachedLevels;
        private bool _isDirty = true;

        private void OnEnable()
        {
            _isDirty = true;
        }

        private void OnValidate()
        {
            _isDirty = true;
        }

        private void EnsureCache()
        {
            if (!_isDirty && _cachedLevels != null)
            {
                return;
            }

            _cachedLevels = new Dictionary<string, List<ClassAbilityLevelEntry>>(StringComparer.OrdinalIgnoreCase);
            if (classAbilityTracks != null)
            {
                foreach (var track in classAbilityTracks)
                {
                    if (track == null || string.IsNullOrWhiteSpace(track.ClassId))
                    {
                        continue;
                    }

                    if (!_cachedLevels.TryGetValue(track.ClassId, out var levelList))
                    {
                        levelList = new List<ClassAbilityLevelEntry>();
                        _cachedLevels[track.ClassId] = levelList;
                    }

                    if (track.Levels == null)
                    {
                        continue;
                    }

                    foreach (var levelEntry in track.Levels)
                    {
                        if (levelEntry == null)
                        {
                            continue;
                        }

                        var clone = levelEntry.Clone();
                        levelList.RemoveAll(l => l.Level == clone.Level);
                        levelList.Add(clone);
                    }
                }
            }

            foreach (var list in _cachedLevels.Values)
            {
                list.Sort((a, b) => a.Level.CompareTo(b.Level));
            }

            _isDirty = false;
        }

        public IReadOnlyList<ClassAbilityLevelEntry> GetProgression(string classId)
        {
            EnsureCache();
            if (string.IsNullOrWhiteSpace(classId) || _cachedLevels == null)
            {
                return Array.Empty<ClassAbilityLevelEntry>();
            }

            if (!_cachedLevels.TryGetValue(classId, out var levels) || levels == null || levels.Count == 0)
            {
                return Array.Empty<ClassAbilityLevelEntry>();
            }

            return levels.Select(l => l.Clone()).ToList();
        }

        public IReadOnlyList<ClassAbilityDefinition> GetAbilitiesUnlockedAtLevel(string classId, int level)
        {
            EnsureCache();
            if (string.IsNullOrWhiteSpace(classId) || _cachedLevels == null)
            {
                return Array.Empty<ClassAbilityDefinition>();
            }

            if (!_cachedLevels.TryGetValue(classId, out var levels) || levels == null || levels.Count == 0)
            {
                return Array.Empty<ClassAbilityDefinition>();
            }

            var entry = levels.FirstOrDefault(l => l.Level == level);
            if (entry == null || entry.Abilities == null || entry.Abilities.Count == 0)
            {
                return Array.Empty<ClassAbilityDefinition>();
            }

            return entry.Abilities.Select(a => a.Clone()).ToList();
        }

        public IReadOnlyList<string> GetTrackedClassIds()
        {
            EnsureCache();
            if (_cachedLevels == null || _cachedLevels.Count == 0)
            {
                return Array.Empty<string>();
            }

            return _cachedLevels.Keys.ToList();
        }

        [Serializable]
        public class ClassAbilityTrack
        {
            public string ClassId;
            public List<ClassAbilityLevelEntry> Levels = new();
        }

        [Serializable]
        public class ClassAbilityLevelEntry
        {
            public int Level;
            public List<ClassAbilityDefinition> Abilities = new();

            public ClassAbilityLevelEntry Clone()
            {
                var clone = new ClassAbilityLevelEntry
                {
                    Level = Level,
                    Abilities = new List<ClassAbilityDefinition>()
                };

                if (Abilities != null)
                {
                    foreach (var ability in Abilities)
                    {
                        if (ability == null)
                        {
                            continue;
                        }

                        clone.Abilities.Add(ability.Clone());
                    }
                }

                return clone;
            }
        }

        [Serializable]
        public class ClassAbilityDefinition
        {
            public string AbilityId;
            public string DisplayName;
            [TextArea]
            public string Description;
            [TextArea]
            public string Tooltip;

            public ClassAbilityDefinition Clone()
            {
                return new ClassAbilityDefinition
                {
                    AbilityId = AbilityId,
                    DisplayName = DisplayName,
                    Description = Description,
                    Tooltip = Tooltip
                };
            }
        }
    }
}
