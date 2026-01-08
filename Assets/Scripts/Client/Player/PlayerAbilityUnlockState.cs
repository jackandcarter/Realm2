using System;
using System.Collections.Generic;
using System.Text;
using Client.CharacterCreation;
using Client.Progression;
using Realm.Data;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Client.Player
{
    public static class PlayerAbilityUnlockState
    {
        private static bool _initialized;
        private static string _currentCharacterId;
        private static bool _classDefinitionsLoaded;
        private static Dictionary<string, ClassDefinition> _classDefinitions;

        public static event Action AbilityUnlocksChanged;

        public static bool IsAbilityUnlocked(string classId, string abilityId)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(classId) || string.IsNullOrWhiteSpace(abilityId))
            {
                return false;
            }

            var normalizedClassId = classId.Trim();
            var normalizedAbilityId = abilityId.Trim();
            var level = GetCharacterLevel(_currentCharacterId);
            var unlocked = false;

            if (ClassAbilityCatalog.TryGetAbilityUnlockLevel(normalizedClassId, normalizedAbilityId, out var unlockLevel))
            {
                unlocked = level >= unlockLevel;
            }

            if (unlocked)
            {
                return true;
            }

            if (TryGetAbilityUnlock(normalizedClassId, normalizedAbilityId, out var unlock))
            {
                return IsUnlockConditionMet(unlock, level);
            }

            return false;
        }

        private static int GetCharacterLevel(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return 0;
            }

            if (CharacterProgressionCache.TryGet(characterId, out var snapshot) && snapshot?.progression != null)
            {
                return Math.Max(1, snapshot.progression.level);
            }

            return 1;
        }

        private static bool IsUnlockConditionMet(ClassAbilityUnlock unlock, int level)
        {
            if (unlock == null)
            {
                return false;
            }

            return unlock.ConditionType switch
            {
                AbilityUnlockConditionType.Level => level >= Math.Max(1, unlock.RequiredLevel),
                AbilityUnlockConditionType.Quest => IsQuestCompleted(_currentCharacterId, unlock.QuestId),
                AbilityUnlockConditionType.Item => HasEncounterReward(_currentCharacterId, unlock.ItemId),
                _ => false
            };
        }

        private static bool IsQuestCompleted(string characterId, string questId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(questId))
            {
                return false;
            }

            if (!CharacterProgressionCache.TryGet(characterId, out var snapshot) || snapshot?.quests?.quests == null)
            {
                return false;
            }

            foreach (var quest in snapshot.quests.quests)
            {
                if (quest == null || string.IsNullOrWhiteSpace(quest.questId))
                {
                    continue;
                }

                if (string.Equals(quest.questId.Trim(), questId.Trim(), StringComparison.OrdinalIgnoreCase)
                    && string.Equals(quest.status, "completed", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasEncounterReward(string characterId, string itemId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            if (!CharacterProgressionCache.TryGet(characterId, out var snapshot) || snapshot?.inventory?.items == null)
            {
                return false;
            }

            foreach (var item in snapshot.inventory.items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.itemId))
                {
                    continue;
                }

                if (string.Equals(item.itemId.Trim(), itemId.Trim(), StringComparison.OrdinalIgnoreCase)
                    && item.quantity > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _currentCharacterId = SessionManager.SelectedCharacterId;

            SessionManager.SelectedCharacterChanged += OnSelectedCharacterChanged;
            SessionManager.SessionCleared += OnSessionCleared;
            CharacterProgressionCache.ProgressionSnapshotChanged += OnProgressionSnapshotChanged;
        }

        private static bool TryGetAbilityUnlock(string classId, string abilityId, out ClassAbilityUnlock unlock)
        {
            unlock = null;
            if (string.IsNullOrWhiteSpace(classId) || string.IsNullOrWhiteSpace(abilityId))
            {
                return false;
            }

            if (!TryGetClassDefinition(classId, out var definition) || definition?.AbilityUnlocks == null)
            {
                return false;
            }

            foreach (var entry in definition.AbilityUnlocks)
            {
                if (entry?.Ability == null || string.IsNullOrWhiteSpace(entry.Ability.Guid))
                {
                    continue;
                }

                if (string.Equals(entry.Ability.Guid.Trim(), abilityId.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    unlock = entry;
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetClassDefinition(string classId, out ClassDefinition definition)
        {
            definition = null;
            if (string.IsNullOrWhiteSpace(classId))
            {
                return false;
            }

            EnsureClassDefinitionsLoaded();
            if (_classDefinitions == null || _classDefinitions.Count == 0)
            {
                return false;
            }

            var key = NormalizeClassKey(classId);
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return _classDefinitions.TryGetValue(key, out definition);
        }

        private static void EnsureClassDefinitionsLoaded()
        {
            if (_classDefinitionsLoaded)
            {
                return;
            }

            _classDefinitionsLoaded = true;
            _classDefinitions = new Dictionary<string, ClassDefinition>();

            foreach (var definition in LoadClassDefinitions())
            {
                if (definition == null)
                {
                    continue;
                }

                AddClassDefinitionKey(definition, definition.ClassId);
                AddClassDefinitionKey(definition, definition.DisplayName);
                AddClassDefinitionKey(definition, definition.name);
            }
        }

        private static IEnumerable<ClassDefinition> LoadClassDefinitions()
        {
            var definitions = new List<ClassDefinition>();
#if UNITY_EDITOR
            var guids = AssetDatabase.FindAssets("t:ClassDefinition");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ClassDefinition>(path);
                if (asset != null)
                {
                    definitions.Add(asset);
                }
            }
#endif
            var resourceDefinitions = Resources.LoadAll<ClassDefinition>(string.Empty);
            if (resourceDefinitions != null && resourceDefinitions.Length > 0)
            {
                definitions.AddRange(resourceDefinitions);
            }

            return definitions;
        }

        private static void AddClassDefinitionKey(ClassDefinition definition, string keySource)
        {
            if (definition == null)
            {
                return;
            }

            var key = NormalizeClassKey(keySource);
            if (string.IsNullOrWhiteSpace(key) || _classDefinitions.ContainsKey(key))
            {
                return;
            }

            _classDefinitions[key] = definition;
        }

        private static string NormalizeClassKey(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return null;
            }

            var builder = new StringBuilder(source.Length);
            foreach (var character in source.Trim())
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                }
            }

            return builder.Length == 0 ? null : builder.ToString();
        }

        private static void OnSelectedCharacterChanged(string characterId)
        {
            EnsureInitialized();
            _currentCharacterId = string.IsNullOrWhiteSpace(characterId) ? null : characterId;
            AbilityUnlocksChanged?.Invoke();
        }

        private static void OnSessionCleared()
        {
            EnsureInitialized();
            _currentCharacterId = null;
            AbilityUnlocksChanged?.Invoke();
        }

        private static void OnProgressionSnapshotChanged(string characterId)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(_currentCharacterId))
            {
                return;
            }

            if (string.Equals(_currentCharacterId, characterId, StringComparison.Ordinal))
            {
                AbilityUnlocksChanged?.Invoke();
            }
        }
    }
}
