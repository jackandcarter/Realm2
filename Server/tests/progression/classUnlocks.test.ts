process.env.DB_NAME = process.env.DB_NAME ?? 'realm2_test';
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
  beforeEach(async () => {
    await resetDatabase();
  });

  it('allows unlocking classes permitted for the character race', async () => {
    const user = await createUser('ranger@example.com', 'felarian-ranger', 'hash');
    const character = await createCharacter({
      userId: user.id,
      realmId: DEFAULT_REALM_ID,
      name: 'Swift Ranger',
      raceId: 'felarian',
    });

    const result = await replaceClassUnlocks(
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

    const snapshot = await getCharacterProgressionSnapshot(character.id);
    expect(snapshot.classUnlocks.unlocks).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          classId: 'ranger',
          unlocked: true,
        }),
      ]),
    );
  });

  it('rejects unlocking classes forbidden for the character race', async () => {
    const user = await createUser('human@example.com', 'human-player', 'hash');
    const character = await createCharacter({
      userId: user.id,
      realmId: DEFAULT_REALM_ID,
      name: 'Curious Human',
      raceId: 'human',
    });

    await expect(
      replaceClassUnlocks(
        character.id,
        [
          {
            classId: 'necromancer',
            unlocked: true,
          },
        ],
        0,
      )
    ).rejects.toThrow(ForbiddenClassUnlockError);

    await expect(
      replaceClassUnlocks(
        character.id,
        [
          {
            classId: 'necromancer',
            unlocked: true,
          },
        ],
        0,
      )
    ).rejects.toThrow('Class necromancer cannot be unlocked for race human');
  });

  it('allows locked states to be recorded for forbidden classes', async () => {
    const user = await createUser('revenant@example.com', 'revenant-player', 'hash');
    const character = await createCharacter({
      userId: user.id,
      realmId: DEFAULT_REALM_ID,
      name: 'Wary Revenant',
      raceId: 'human',
    });

    const result = await replaceClassUnlocks(
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
