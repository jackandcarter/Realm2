process.env.DB_NAME = process.env.DB_NAME ?? 'realm2_test';
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
  beforeEach(async () => {
    await resetDatabase();
    await upsertChunk({
      id: CHUNK_ID,
      realmId: REALM_ID,
      chunkX: 0,
      chunkZ: 0,
      payloadJson: '{}',
      isDeleted: false,
    });
  });

  it('deducts resources and records a change for authorized builders', async () => {
    const builder = await createUser('builder@example.com', 'builder', 'hash');
    await createMembership(builder.id, REALM_ID, 'builder');
    await applyResourceAdjustments(REALM_ID, builder.id, [{ resourceType: 'stone', delta: 25 }]);

    const change = await recordChunkChange(
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

    const wallet = await listWalletEntries(REALM_ID, builder.id);
    const stoneBalance = wallet.find((entry) => entry.resourceType === 'stone');
    expect(stoneBalance?.quantity).toBe(20);
  });

  it('blocks non-builders from modifying structures', async () => {
    const player = await createUser('player@example.com', 'player', 'hash');
    await createMembership(player.id, REALM_ID, 'player');

    await expect(
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
    ).rejects.toThrow(HttpError);
  });

  it('enforces plot ownership rules for players', async () => {
    const builder = await createUser('architect@example.com', 'architect', 'hash');
    await createMembership(builder.id, REALM_ID, 'builder');
    const owner = await createUser('owner@example.com', 'owner', 'hash');
    await createMembership(owner.id, REALM_ID, 'player');
    const intruder = await createUser('intruder@example.com', 'intruder', 'hash');
    await createMembership(intruder.id, REALM_ID, 'player');

    await recordChunkChange(
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

    await expect(
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
    ).rejects.toThrow(HttpError);
  });

  it('rejects mutations when the user lacks sufficient resources', async () => {
    const builder = await createUser('artisan@example.com', 'artisan', 'hash');
    await createMembership(builder.id, REALM_ID, 'builder');
    await applyResourceAdjustments(REALM_ID, builder.id, [{ resourceType: 'lumber', delta: 2 }]);

    try {
      await recordChunkChange(
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

    const wallet = await listWalletEntries(REALM_ID, builder.id);
    const lumber = wallet.find((entry) => entry.resourceType === 'lumber');
    expect(lumber?.quantity).toBe(2);
  });

  it('blocks edits to immutable base terrain payloads', async () => {
    const builder = await createUser('terrain-builder@example.com', 'builder', 'hash');
    await createMembership(builder.id, REALM_ID, 'builder');
    await upsertChunk({
      id: 'chunk-base',
      realmId: REALM_ID,
      chunkX: 1,
      chunkZ: 2,
      payloadJson: JSON.stringify({ terrainLayer: 'base', payloadVersion: 1 }),
      isDeleted: false,
    });

    await expect(
      recordChunkChange(
        builder.id,
        REALM_ID,
        'chunk-base',
        'terrain:update',
        {
          payload: { terrainLayer: 'base', payloadVersion: 2 },
        }
      )
    ).rejects.toThrow(HttpError);
  });
});
