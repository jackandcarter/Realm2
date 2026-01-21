import { randomUUID } from 'crypto';
import { db } from './database';
import { recordVersionConflict } from '../observability/metrics';

export interface MapPinStateRecord {
  pinId: string;
  unlocked: boolean;
  updatedAt: string;
}

export interface MapPinStateCollection {
  version: number;
  updatedAt: string;
  pins: MapPinStateRecord[];
}

export interface MapPinStateInput {
  pinId: string;
  unlocked: boolean;
}

export class MapPinVersionConflictError extends Error {
  constructor(public readonly expected: number, public readonly actual: number) {
    super('mapPins version conflict');
    this.name = 'MapPinVersionConflictError';
  }
}

async function ensureMapPinMeta(characterId: string): Promise<void> {
  const now = new Date().toISOString();
  await db.execute(
    `INSERT INTO character_map_pin_state_meta (character_id, version, updated_at)
     VALUES (?, 0, ?)
     ON DUPLICATE KEY UPDATE updated_at = updated_at`,
    [characterId, now]
  );
}

export async function getMapPinSnapshot(characterId: string): Promise<MapPinStateCollection> {
  await ensureMapPinMeta(characterId);

  const pinRows = await db.query<any[]>(
    `SELECT pin_id as pinId, unlocked, updated_at as updatedAt
     FROM character_map_pin_states
     WHERE character_id = ?
     ORDER BY updated_at DESC`,
    [characterId]
  );

  const metaRows = await db.query<any[]>(
    `SELECT version, updated_at as updatedAt
     FROM character_map_pin_state_meta
     WHERE character_id = ?`,
    [characterId]
  );
  const meta = metaRows[0];

  return {
    version: meta?.version ?? 0,
    updatedAt: meta?.updatedAt ?? new Date().toISOString(),
    pins: (pinRows ?? []).map((row) => ({
      pinId: row.pinId,
      unlocked: Boolean(row.unlocked),
      updatedAt: row.updatedAt,
    })),
  };
}

export async function replaceMapPinStates(
  characterId: string,
  pins: MapPinStateInput[],
  expectedVersion: number
): Promise<MapPinStateCollection> {
  await ensureMapPinMeta(characterId);

  return db.withTransaction(async (tx) => {
    const metaRows = await tx.query<any[]>(
      `SELECT version
       FROM character_map_pin_state_meta
       WHERE character_id = ?
       FOR UPDATE`,
      [characterId]
    );
    const actualVersion = metaRows[0]?.version ?? 0;
    if (actualVersion !== expectedVersion) {
      recordVersionConflict('mapPins');
      throw new MapPinVersionConflictError(expectedVersion, actualVersion);
    }

    await tx.execute(`DELETE FROM character_map_pin_states WHERE character_id = ?`, [
      characterId,
    ]);

    const now = new Date().toISOString();
    for (const pin of pins) {
      await tx.execute(
        `INSERT INTO character_map_pin_states (id, character_id, pin_id, unlocked, updated_at)
         VALUES (?, ?, ?, ?, ?)`,
        [randomUUID(), characterId, pin.pinId, pin.unlocked ? 1 : 0, now]
      );
    }

    await tx.execute(
      `UPDATE character_map_pin_state_meta
       SET version = version + 1, updated_at = ?
       WHERE character_id = ?`,
      [now, characterId]
    );

    const snapshot = await tx.query<any[]>(
      `SELECT version, updated_at as updatedAt
       FROM character_map_pin_state_meta
       WHERE character_id = ?`,
      [characterId]
    );
    const meta = snapshot[0];

    return {
      version: meta?.version ?? actualVersion + 1,
      updatedAt: meta?.updatedAt ?? now,
      pins: pins.map((pin) => ({
        pinId: pin.pinId,
        unlocked: Boolean(pin.unlocked),
        updatedAt: now,
      })),
    };
  });
}
