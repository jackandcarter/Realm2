import { consumeInventoryItems, getCharacterProgressionSnapshot } from '../../db/progressionRepository';
import { HttpError } from '../../utils/errors';
import { ClassUnlockQuestOptions, QuestCompletionResult, completeClassUnlockQuest } from './classUnlockQuest';

const BUILDER_QUEST_OPTIONS: ClassUnlockQuestOptions = {
  questId: 'quest-builder-arkitect',
  classId: 'builder',
  classDisplayName: 'Builder',
  successNotificationMessage: 'You delivered the materials and earned the Builder mantle.',
  successJournalEntry: {
    title: 'Builder Ascension',
    body: 'With the requested materials delivered, the Builder has accepted you as an apprentice.',
  },
};

const BUILDER_MATERIAL_REQUIREMENTS = [
  { itemId: 'resource.wood', quantity: 20 },
  { itemId: 'resource.ore', quantity: 12 },
  { itemId: 'resource.wood-beam-small', quantity: 4 },
];

export async function completeBuilderArkitectQuest(
  characterId: string,
): Promise<QuestCompletionResult> {
  const snapshot = await getCharacterProgressionSnapshot(characterId);
  const inventoryMap = new Map(
    snapshot.inventory.items.map((item) => [item.itemId.trim().toLowerCase(), item.quantity]),
  );

  const missing: string[] = [];
  for (const requirement of BUILDER_MATERIAL_REQUIREMENTS) {
    const itemId = requirement.itemId.trim().toLowerCase();
    const quantity = requirement.quantity;
    const available = inventoryMap.get(itemId) ?? 0;
    if (available < quantity) {
      missing.push(`${requirement.itemId} (${available}/${quantity})`);
    }
  }

  if (missing.length > 0) {
    throw new HttpError(
      400,
      `Missing required materials to complete ${BUILDER_QUEST_OPTIONS.questId}: ${missing.join(', ')}`,
    );
  }

  await consumeInventoryItems(characterId, BUILDER_MATERIAL_REQUIREMENTS);
  return completeClassUnlockQuest(characterId, BUILDER_QUEST_OPTIONS);
}

export function getBuilderQuestMaterialRequirements() {
  return BUILDER_MATERIAL_REQUIREMENTS.map((entry) => ({ ...entry }));
}
