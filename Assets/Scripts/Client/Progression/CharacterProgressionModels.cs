using System;

namespace Client.Progression
{
    [Serializable]
    public class CharacterProgressionStats
    {
        public int level;
        public int xp;
        public int version;
        public string updatedAt;
    }

    [Serializable]
    public class CharacterClassUnlockEntry
    {
        public string classId;
        public bool unlocked;
        public string unlockedAt;
    }

    [Serializable]
    public class CharacterClassUnlockCollection
    {
        public int version;
        public string updatedAt;
        public CharacterClassUnlockEntry[] unlocks;
    }

    [Serializable]
    public class CharacterInventoryItemEntry
    {
        public string itemId;
        public int quantity;
        public string metadataJson;
    }

    [Serializable]
    public class CharacterInventoryCollection
    {
        public int version;
        public string updatedAt;
        public CharacterInventoryItemEntry[] items;
    }

    [Serializable]
    public class CharacterEquipmentEntry
    {
        public string classId;
        public string slot;
        public string itemId;
        public string metadataJson;
    }

    [Serializable]
    public class CharacterEquipmentCollection
    {
        public int version;
        public string updatedAt;
        public CharacterEquipmentEntry[] items;
    }

    [Serializable]
    public class CharacterQuestStateEntry
    {
        public string questId;
        public string status;
        public string progressJson;
        public string updatedAt;
    }

    [Serializable]
    public class CharacterQuestCollection
    {
        public int version;
        public string updatedAt;
        public CharacterQuestStateEntry[] quests;
    }

    [Serializable]
    public class CharacterProgressionEnvelope
    {
        public CharacterProgressionStats progression;
        public CharacterClassUnlockCollection classUnlocks;
        public CharacterInventoryCollection inventory;
        public CharacterEquipmentCollection equipment;
        public CharacterQuestCollection quests;
    }

    [Serializable]
    internal class CharacterProgressionUpdateRequest
    {
        public CharacterProgressionUpdateLevels progression;
        public CharacterClassUnlockUpdatePayload classUnlocks;
        public CharacterInventoryUpdatePayload inventory;
        public CharacterEquipmentUpdatePayload equipment;
        public CharacterQuestUpdatePayload quests;
    }

    [Serializable]
    internal class CharacterProgressionUpdateLevels
    {
        public int level;
        public int xp;
        public int expectedVersion;
    }

    [Serializable]
    internal class CharacterClassUnlockUpdatePayload
    {
        public int expectedVersion;
        public CharacterClassUnlockUpdateEntry[] unlocks;
    }

    [Serializable]
    internal class CharacterClassUnlockUpdateEntry
    {
        public string classId;
        public bool unlocked;
        public string unlockedAt;
    }

    [Serializable]
    internal class CharacterInventoryUpdatePayload
    {
        public int expectedVersion;
        public CharacterInventoryItemEntry[] items;
    }

    [Serializable]
    internal class CharacterEquipmentUpdatePayload
    {
        public int expectedVersion;
        public CharacterEquipmentEntry[] items;
    }

    [Serializable]
    internal class CharacterQuestUpdatePayload
    {
        public int expectedVersion;
        public CharacterQuestStateEntry[] quests;
    }
}
