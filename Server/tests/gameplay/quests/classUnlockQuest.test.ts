process.env.DB_PATH = ':memory:';
process.env.JWT_SECRET = 'test-secret';

import { resetDatabase } from '../../../src/db/database';
import { createUser } from '../../../src/db/userRepository';
import { createCharacter } from '../../../src/db/characterRepository';
import {
  completeRangerAscensionQuest,
  completeTimeMageConvergenceQuest,
  QuestNotification,
} from '../../../src/gameplay/quests';
import { getCharacterProgressionSnapshot } from '../../../src/db/progressionRepository';

const DEFAULT_REALM_ID = 'realm-elysium-nexus';
const RANGER_QUEST_ID = 'quest-ranger-ascension';
const TIME_MAGE_QUEST_ID = 'quest-time-mage-convergence';

function parseProgress(progressJson: string | undefined): any {
  if (!progressJson) {
    return {};
  }
  try {
    return JSON.parse(progressJson);
  } catch (_error) {
    return {};
  }
}

describe('class unlock quest scripts', () => {
  beforeEach(() => {
    resetDatabase();
  });

  it('unlocks the Ranger class when the ascension quest succeeds', () => {
    const user = createUser('felarian-ranger@example.com', 'swift-ranger', 'hash');
    const character = createCharacter({
      userId: user.id,
      realmId: DEFAULT_REALM_ID,
      name: 'Swift Ranger',
      raceId: 'felarian',
    });

    const result = completeRangerAscensionQuest(character.id);

    expect(result.status).toBe('completed');
    expect(result.unlockGranted).toBe(true);
    expect(result.questStateVersion).toBeGreaterThan(0);
    expect(result.classUnlockVersion).toBeGreaterThan(0);

    const snapshot = getCharacterProgressionSnapshot(character.id);
    const rangerUnlock = snapshot.classUnlocks.unlocks.find(
      (entry) => entry.classId.trim().toLowerCase() === 'ranger',
    );
    expect(rangerUnlock?.unlocked).toBe(true);

    const questRecord = snapshot.quests.quests.find((quest) => quest.questId === RANGER_QUEST_ID);
    expect(questRecord).toBeDefined();
    expect(questRecord?.status).toBe('completed');
    const progress = parseProgress(questRecord?.progressJson);
    expect(progress.unlockedClassId).toBe('ranger');
    expect(progress.notifications).toEqual(
      expect.arrayContaining([
        expect.objectContaining<QuestNotification>({
          type: 'success',
          classId: 'ranger',
        }),
      ]),
    );
    expect(progress.journalEntries).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          title: 'Ranger Initiation Complete',
        }),
      ]),
    );
  });

  it('records a graceful completion when the Ranger class was already unlocked', () => {
    const user = createUser('veteran-ranger@example.com', 'veteran-ranger', 'hash');
    const character = createCharacter({
      userId: user.id,
      realmId: DEFAULT_REALM_ID,
      name: 'Veteran Ranger',
      raceId: 'felarian',
    });

    const first = completeRangerAscensionQuest(character.id);
    expect(first.unlockGranted).toBe(true);

    const second = completeRangerAscensionQuest(character.id);
    expect(second.status).toBe('completed');
    expect(second.unlockGranted).toBe(false);
    expect(second.unlockError).toBeUndefined();

    const snapshot = getCharacterProgressionSnapshot(character.id);
    const questRecord = snapshot.quests.quests.find((quest) => quest.questId === RANGER_QUEST_ID);
    const progress = parseProgress(questRecord?.progressJson);
    const infoNotifications = progress.notifications.filter(
      (entry: QuestNotification) => entry.type === 'info' && entry.classId === 'ranger',
    );
    expect(infoNotifications.length).toBeGreaterThan(0);
  });

  it('marks the Ranger quest as failed when the class cannot be unlocked for the race', () => {
    const user = createUser('human-adventurer@example.com', 'human-hero', 'hash');
    const character = createCharacter({
      userId: user.id,
      realmId: DEFAULT_REALM_ID,
      name: 'Human Adventurer',
      raceId: 'human',
    });

    const result = completeRangerAscensionQuest(character.id);

    expect(result.status).toBe('failed');
    expect(result.unlockGranted).toBe(false);
    expect(result.unlockError).toBe('Class ranger cannot be unlocked for race human');

    const snapshot = getCharacterProgressionSnapshot(character.id);
    const questRecord = snapshot.quests.quests.find((quest) => quest.questId === RANGER_QUEST_ID);
    expect(questRecord?.status).toBe('failed');
    const progress = parseProgress(questRecord?.progressJson);
    expect(progress.failure).toEqual(
      expect.objectContaining({ reason: 'Class ranger cannot be unlocked for race human' }),
    );
    expect(progress.notifications).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ type: 'error', classId: 'ranger' }),
      ]),
    );
  });

  it('unlocks the Time Mage class and records the journal feedback', () => {
    const user = createUser('chrononaut@example.com', 'chrono-walker', 'hash');
    const character = createCharacter({
      userId: user.id,
      realmId: DEFAULT_REALM_ID,
      name: 'Chrono Walker',
      raceId: 'human',
    });

    const result = completeTimeMageConvergenceQuest(character.id);

    expect(result.status).toBe('completed');
    expect(result.unlockGranted).toBe(true);

    const snapshot = getCharacterProgressionSnapshot(character.id);
    const unlock = snapshot.classUnlocks.unlocks.find(
      (entry) => entry.classId.trim().toLowerCase() === 'time-mage',
    );
    expect(unlock?.unlocked).toBe(true);

    const questRecord = snapshot.quests.quests.find((quest) => quest.questId === TIME_MAGE_QUEST_ID);
    expect(questRecord?.status).toBe('completed');
    const progress = parseProgress(questRecord?.progressJson);
    expect(progress.notifications).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ type: 'success', classId: 'time-mage' }),
      ]),
    );
    expect(progress.journalEntries).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          title: 'Chronomantic Rite Completed',
        }),
      ]),
    );
  });
});
