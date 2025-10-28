import { ClassUnlockQuestOptions, QuestCompletionResult, completeClassUnlockQuest } from './classUnlockQuest';

const TIME_MAGE_QUEST_OPTIONS: ClassUnlockQuestOptions = {
  questId: 'quest-time-mage-convergence',
  classId: 'time-mage',
  classDisplayName: 'Time Mage',
  successNotificationMessage: 'Temporal currents align! You unlocked the Time Mage class.',
  alreadyUnlockedNotificationMessage: 'Time Mage teachings were already within your grasp.',
  failureNotificationMessage: 'Temporal rift remains sealed: {error}.',
  successJournalEntry: {
    title: 'Chronomantic Rite Completed',
    body: 'Your mastery over time has been acknowledged. The Time Mage class is now yours.',
  },
  failureJournalEntry: {
    title: 'Chronomantic Rite Failed',
    body: 'The ritual to unlock Time Mage collapsed: {error}.',
  },
};

export function completeTimeMageConvergenceQuest(characterId: string): QuestCompletionResult {
  return completeClassUnlockQuest(characterId, TIME_MAGE_QUEST_OPTIONS);
}
