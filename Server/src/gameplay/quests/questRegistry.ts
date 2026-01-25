import { HttpError } from '../../utils/errors';
import type { QuestCompletionResult } from './classUnlockQuest';
import { completeBuilderArkitectQuest } from './builderQuest';
import { completeRangerAscensionQuest } from './rangerQuest';
import { completeTimeMageConvergenceQuest } from './timeMageQuest';

export type QuestCompletionHandler = (characterId: string) => Promise<QuestCompletionResult>;

const questHandlers: Record<string, QuestCompletionHandler> = {
  'quest-builder-arkitect': completeBuilderArkitectQuest,
  'quest-ranger-ascension': completeRangerAscensionQuest,
  'quest-ranger-trial': completeRangerAscensionQuest,
  'quest-time-mage-convergence': completeTimeMageConvergenceQuest,
};

export function resolveQuestCompletionHandler(questId: string): QuestCompletionHandler {
  const key = questId.trim().toLowerCase();
  const handler = questHandlers[key];
  if (!handler) {
    throw new HttpError(404, `Quest handler not found for ${questId}`);
  }
  return handler;
}
