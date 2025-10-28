process.env.DB_PATH = ':memory:';
process.env.JWT_SECRET = 'test-secret';

import { resetDatabase } from '../../src/db/database';
import { createUser } from '../../src/db/userRepository';
import { createCharacter } from '../../src/db/characterRepository';
import {
  ForbiddenClassUnlockError,
  replaceClassUnlocks,
  getCharacterProgressionSnapshot,
} from '../../src/db/progressionRepository';

const DEFAULT_REALM_ID = 'realm-elysium-nexus';

describe('class unlock rules', () => {
  beforeEach(() => {
    resetDatabase();
  });

  it('allows unlocking classes permitted for the character race', () => {
    const user = createUser('ranger@example.com', 'felarian-ranger', 'hash');
    const character = createCharacter({
      userId: user.id,
      realmId: DEFAULT_REALM_ID,
      name: 'Swift Ranger',
      raceId: 'felarian',
    });

    const result = replaceClassUnlocks(
      character.id,
      [
        {
          classId: 'ranger',
          unlocked: true,
        },
      ],
      0,
    );

    expect(result.version).toBe(1);
    expect(result.unlocks).toHaveLength(1);
    expect(result.unlocks[0]).toMatchObject({
      classId: 'ranger',
      unlocked: true,
    });
    expect(result.unlocks[0].unlockedAt).toEqual(expect.any(String));

    const snapshot = getCharacterProgressionSnapshot(character.id);
    expect(snapshot.classUnlocks.unlocks).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          classId: 'ranger',
          unlocked: true,
        }),
      ]),
    );
  });

  it('rejects unlocking classes forbidden for the character race', () => {
    const user = createUser('human@example.com', 'human-player', 'hash');
    const character = createCharacter({
      userId: user.id,
      realmId: DEFAULT_REALM_ID,
      name: 'Curious Human',
      raceId: 'human',
    });

    expect(() =>
      replaceClassUnlocks(
        character.id,
        [
          {
            classId: 'necromancer',
            unlocked: true,
          },
        ],
        0,
      ),
    ).toThrow(ForbiddenClassUnlockError);

    expect(() =>
      replaceClassUnlocks(
        character.id,
        [
          {
            classId: 'necromancer',
            unlocked: true,
          },
        ],
        0,
      ),
    ).toThrow('Class necromancer cannot be unlocked for race human');
  });

  it('allows locked states to be recorded for forbidden classes', () => {
    const user = createUser('revenant@example.com', 'revenant-player', 'hash');
    const character = createCharacter({
      userId: user.id,
      realmId: DEFAULT_REALM_ID,
      name: 'Wary Revenant',
      raceId: 'human',
    });

    const result = replaceClassUnlocks(
      character.id,
      [
        {
          classId: 'necromancer',
          unlocked: false,
        },
      ],
      0,
    );

    expect(result.version).toBe(1);
    expect(result.unlocks[0]).toMatchObject({
      classId: 'necromancer',
      unlocked: false,
      unlockedAt: null,
    });
  });
});
