process.env.DB_PATH = ':memory:';
process.env.JWT_SECRET = 'test-secret';

import { resetDatabase } from '../src/db/database';
import { createUser } from '../src/db/userRepository';
import { createMembership } from '../src/db/realmMembershipRepository';
import { upsertChunk } from '../src/db/chunkRepository';
import { recordChunkChange } from '../src/services/chunkService';
import { applyResourceAdjustments, listWalletEntries } from '../src/db/resourceWalletRepository';
import { HttpError } from '../src/utils/errors';

const REALM_ID = 'realm-elysium-nexus';
const CHUNK_ID = 'chunk-test-hub';

describe('chunk build transactions', () => {
  beforeEach(() => {
    resetDatabase();
    upsertChunk({
      id: CHUNK_ID,
      realmId: REALM_ID,
      chunkX: 0,
      chunkZ: 0,
      payloadJson: '{}',
      isDeleted: false,
    });
  });

  it('deducts resources and records a change for authorized builders', () => {
    const builder = createUser('builder@example.com', 'builder', 'hash');
    createMembership(builder.id, REALM_ID, 'builder');
    applyResourceAdjustments(REALM_ID, builder.id, [{ resourceType: 'stone', delta: 25 }]);

    const change = recordChunkChange(
      builder.id,
      REALM_ID,
      CHUNK_ID,
      'structure:build',
      undefined,
      [
        {
          structureType: 'watchtower',
          data: { tier: 1 },
        },
      ],
      undefined,
      [{ resourceType: 'stone', quantity: 5 }]
    );

    expect(change.structures).toHaveLength(1);
    expect(change.resources).toEqual([{ resourceType: 'stone', quantity: 5 }]);

    const wallet = listWalletEntries(REALM_ID, builder.id);
    const stoneBalance = wallet.find((entry) => entry.resourceType === 'stone');
    expect(stoneBalance?.quantity).toBe(20);
  });

  it('blocks non-builders from modifying structures', () => {
    const player = createUser('player@example.com', 'player', 'hash');
    createMembership(player.id, REALM_ID, 'player');

    expect(() =>
      recordChunkChange(
        player.id,
        REALM_ID,
        CHUNK_ID,
        'structure:build',
        undefined,
        [
          {
            structureType: 'forge',
          },
        ],
        undefined,
        [{ resourceType: 'iron', quantity: 3 }]
      )
    ).toThrow(HttpError);
  });

  it('enforces plot ownership rules for players', () => {
    const builder = createUser('architect@example.com', 'architect', 'hash');
    createMembership(builder.id, REALM_ID, 'builder');
    const owner = createUser('owner@example.com', 'owner', 'hash');
    createMembership(owner.id, REALM_ID, 'player');
    const intruder = createUser('intruder@example.com', 'intruder', 'hash');
    createMembership(intruder.id, REALM_ID, 'player');

    recordChunkChange(
      builder.id,
      REALM_ID,
      CHUNK_ID,
      'plot:create',
      undefined,
      undefined,
      [
        {
          plotIdentifier: 'plot-a',
          ownerUserId: owner.id,
        },
      ]
    );

    expect(() =>
      recordChunkChange(
        intruder.id,
        REALM_ID,
        CHUNK_ID,
        'plot:claim',
        undefined,
        undefined,
        [
          {
            plotIdentifier: 'plot-a',
            ownerUserId: intruder.id,
          },
        ]
      )
    ).toThrow(HttpError);
  });

  it('rejects mutations when the user lacks sufficient resources', () => {
    const builder = createUser('artisan@example.com', 'artisan', 'hash');
    createMembership(builder.id, REALM_ID, 'builder');
    applyResourceAdjustments(REALM_ID, builder.id, [{ resourceType: 'lumber', delta: 2 }]);

    try {
      recordChunkChange(
        builder.id,
        REALM_ID,
        CHUNK_ID,
        'structure:upgrade',
        undefined,
        [
          {
            structureType: 'barracks',
            data: { tier: 2 },
          },
        ],
        undefined,
        [{ resourceType: 'lumber', quantity: 10 }]
      );
      fail('Expected insufficient resource error');
    } catch (error) {
      expect(error).toBeInstanceOf(HttpError);
      const httpError = error as HttpError;
      expect(httpError.status).toBe(409);
    }

    const wallet = listWalletEntries(REALM_ID, builder.id);
    const lumber = wallet.find((entry) => entry.resourceType === 'lumber');
    expect(lumber?.quantity).toBe(2);
  });
});
