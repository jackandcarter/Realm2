using System;
using System.Collections.Generic;

namespace Client.Quests
{
    public enum QuestObjectiveType
    {
        CollectItems,
        TalkToNpc,
        InteractObject
    }

    [Serializable]
    public class QuestItemRequirement
    {
        public string ItemId;
        public int Quantity;
    }

    [Serializable]
    public class QuestDefinition
    {
        public string QuestId;
        public string Title;
        public string Description;
        public QuestObjectiveType ObjectiveType;
        public QuestItemRequirement[] ItemRequirements;
        public string UnlockClassId;
    }

    public static class QuestCatalog
    {
        private static readonly QuestDefinition[] Quests =
        {
            new QuestDefinition
            {
                QuestId = "quest-builder-arkitect",
                Title = "The Arkitect's Request",
                Description = "Gather the requested building materials to earn the Builder mantle.",
                ObjectiveType = QuestObjectiveType.CollectItems,
                ItemRequirements = new[]
                {
                    new QuestItemRequirement { ItemId = "resource.wood", Quantity = 20 },
                    new QuestItemRequirement { ItemId = "resource.ore", Quantity = 12 },
                    new QuestItemRequirement { ItemId = "resource.wood-beam-small", Quantity = 4 }
                },
                UnlockClassId = "builder"
            }
        };

        private static readonly Dictionary<string, QuestDefinition> Lookup;

        static QuestCatalog()
        {
            Lookup = new Dictionary<string, QuestDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var quest in Quests)
            {
                if (quest == null || string.IsNullOrWhiteSpace(quest.QuestId))
                {
                    continue;
                }

                if (!Lookup.ContainsKey(quest.QuestId))
                {
                    Lookup[quest.QuestId] = quest;
                }
            }
        }

        public static IReadOnlyList<QuestDefinition> GetAllQuests()
        {
            return Quests;
        }

        public static bool TryGetQuest(string questId, out QuestDefinition quest)
        {
            if (string.IsNullOrWhiteSpace(questId))
            {
                quest = null;
                return false;
            }

            return Lookup.TryGetValue(questId.Trim(), out quest);
        }
    }
}
