import { ClassUnlockQuestOptions, QuestCompletionResult, completeClassUnlockQuest } from './classUnlockQuest';

const RANGER_QUEST_OPTIONS: ClassUnlockQuestOptions = {
  questId: 'quest-ranger-ascension',
  classId: 'ranger',
  classDisplayName: 'Ranger',
  successNotificationMessage: 'Ranger class unlocked! The Wardens of the Wild welcome you.',
  alreadyUnlockedNotificationMessage: 'Ranger class was already available to you.',
  failureNotificationMessage: 'Ranger conclave refuses your petition: {error}.',
  successJournalEntry: {
    title: 'Ranger Initiation Complete',
    body: 'The wilds recognize your mastery. You unlocked the Ranger class.',
  },
  failureJournalEntry: {
    title: 'Ranger Trial Failed',
    body: 'Your attempt to earn the Ranger mantle failed: {error}.',
  },
};

export async function completeRangerAscensionQuest(
  characterId: string
): Promise<QuestCompletionResult> {
  return completeClassUnlockQuest(characterId, RANGER_QUEST_OPTIONS);
}
