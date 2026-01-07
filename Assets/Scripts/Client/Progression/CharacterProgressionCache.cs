using System;
using System.Collections.Generic;

namespace Client.Progression
{
    public static class CharacterProgressionCache
    {
        private static readonly Dictionary<string, CharacterProgressionEnvelope> Snapshots =
            new Dictionary<string, CharacterProgressionEnvelope>(StringComparer.OrdinalIgnoreCase);

        public static event Action<string> ProgressionSnapshotChanged;

        static CharacterProgressionCache()
        {
            SessionManager.SessionCleared += Clear;
        }

        public static void Clear()
        {
            Snapshots.Clear();
            ProgressionSnapshotChanged?.Invoke(null);
        }

        public static void Store(string characterId, CharacterProgressionEnvelope snapshot)
        {
            if (string.IsNullOrWhiteSpace(characterId) || snapshot == null)
            {
                return;
            }

            Snapshots[characterId] = Clone(snapshot);
            ProgressionSnapshotChanged?.Invoke(characterId);
        }

        public static bool TryGet(string characterId, out CharacterProgressionEnvelope snapshot)
        {
            snapshot = null;
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return false;
            }

            if (!Snapshots.TryGetValue(characterId, out var stored) || stored == null)
            {
                return false;
            }

            snapshot = Clone(stored);
            return true;
        }

        public static int GetClassUnlockVersion(string characterId)
        {
            if (!TryGet(characterId, out var snapshot) || snapshot.classUnlocks == null)
            {
                return 0;
            }

            return snapshot.classUnlocks.version;
        }

        public static CharacterClassUnlockEntry[] GetClassUnlocks(string characterId)
        {
            if (!TryGet(characterId, out var snapshot) || snapshot.classUnlocks?.unlocks == null)
            {
                return Array.Empty<CharacterClassUnlockEntry>();
            }

            return CloneClassUnlocks(snapshot.classUnlocks.unlocks);
        }

        public static void UpdateClassUnlocks(string characterId, CharacterClassUnlockCollection collection)
        {
            if (string.IsNullOrWhiteSpace(characterId) || collection == null)
            {
                return;
            }

            if (!Snapshots.TryGetValue(characterId, out var snapshot) || snapshot == null)
            {
                snapshot = new CharacterProgressionEnvelope();
            }

            snapshot.classUnlocks = new CharacterClassUnlockCollection
            {
                version = collection.version,
                updatedAt = collection.updatedAt,
                unlocks = CloneClassUnlocks(collection.unlocks)
            };

            Snapshots[characterId] = snapshot;
        }

        private static CharacterProgressionEnvelope Clone(CharacterProgressionEnvelope source)
        {
            if (source == null)
            {
                return null;
            }

            return new CharacterProgressionEnvelope
            {
                progression = CloneStats(source.progression),
                classUnlocks = source.classUnlocks == null
                    ? null
                    : new CharacterClassUnlockCollection
                    {
                        version = source.classUnlocks.version,
                        updatedAt = source.classUnlocks.updatedAt,
                        unlocks = CloneClassUnlocks(source.classUnlocks.unlocks)
                    },
                inventory = source.inventory == null
                    ? null
                    : new CharacterInventoryCollection
                    {
                        version = source.inventory.version,
                        updatedAt = source.inventory.updatedAt,
                        items = CloneInventory(source.inventory.items)
                    },
                quests = source.quests == null
                    ? null
                    : new CharacterQuestCollection
                    {
                        version = source.quests.version,
                        updatedAt = source.quests.updatedAt,
                        quests = CloneQuestStates(source.quests.quests)
                    }
            };
        }

        private static CharacterProgressionStats CloneStats(CharacterProgressionStats stats)
        {
            if (stats == null)
            {
                return null;
            }

            return new CharacterProgressionStats
            {
                level = stats.level,
                xp = stats.xp,
                version = stats.version,
                updatedAt = stats.updatedAt
            };
        }

        private static CharacterClassUnlockEntry[] CloneClassUnlocks(CharacterClassUnlockEntry[] unlocks)
        {
            if (unlocks == null || unlocks.Length == 0)
            {
                return Array.Empty<CharacterClassUnlockEntry>();
            }

            var clone = new CharacterClassUnlockEntry[unlocks.Length];
            for (var i = 0; i < unlocks.Length; i++)
            {
                var entry = unlocks[i];
                clone[i] = entry == null
                    ? null
                    : new CharacterClassUnlockEntry
                    {
                        classId = entry.classId,
                        unlocked = entry.unlocked,
                        unlockedAt = entry.unlockedAt
                    };
            }

            return clone;
        }

        private static CharacterInventoryItemEntry[] CloneInventory(CharacterInventoryItemEntry[] items)
        {
            if (items == null || items.Length == 0)
            {
                return Array.Empty<CharacterInventoryItemEntry>();
            }

            var clone = new CharacterInventoryItemEntry[items.Length];
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                clone[i] = item == null
                    ? null
                    : new CharacterInventoryItemEntry
                    {
                        itemId = item.itemId,
                        quantity = item.quantity,
                        metadataJson = item.metadataJson
                    };
            }

            return clone;
        }

        private static CharacterQuestStateEntry[] CloneQuestStates(CharacterQuestStateEntry[] quests)
        {
            if (quests == null || quests.Length == 0)
            {
                return Array.Empty<CharacterQuestStateEntry>();
            }

            var clone = new CharacterQuestStateEntry[quests.Length];
            for (var i = 0; i < quests.Length; i++)
            {
                var quest = quests[i];
                clone[i] = quest == null
                    ? null
                    : new CharacterQuestStateEntry
                    {
                        questId = quest.questId,
                        status = quest.status,
                        progressJson = quest.progressJson,
                        updatedAt = quest.updatedAt
                    };
            }

            return clone;
        }
    }
}
