import { randomUUID } from 'crypto';
import { db, DbExecutor } from './database';
import { logger } from '../observability/logger';
import { JsonValue } from '../types/characterCustomization';

interface BuildStateRow {
  id: string;
  realm_id: string;
  character_id: string;
  plots_json: string;
  constructions_json: string;
  updated_at: string;
}

export interface BuildStateSnapshot {
  id: string;
  realmId: string;
  characterId: string;
  plots: JsonValue[];
  constructions: JsonValue[];
  updatedAt: string;
}

function parseJsonList(raw: string | null | undefined, label: string): JsonValue[] {
  if (!raw) {
    return [];
  }

  try {
    const parsed = JSON.parse(raw);
    if (Array.isArray(parsed)) {
      return parsed as JsonValue[];
    }
  } catch (error) {
    logger.warn({ err: error, payload: raw, label }, 'Failed to parse build state payload');
  }

  return [];
}

function mapRow(row: BuildStateRow): BuildStateSnapshot {
  return {
    id: row.id,
    realmId: row.realm_id,
    characterId: row.character_id,
    plots: parseJsonList(row.plots_json, 'plots'),
    constructions: parseJsonList(row.constructions_json, 'constructions'),
    updatedAt: row.updated_at,
  };
}

export async function getBuildState(
  characterId: string,
  realmId: string,
  executor: DbExecutor = db
): Promise<BuildStateSnapshot | undefined> {
  const rows = await executor.query<BuildStateRow[]>(
    `SELECT id, realm_id, character_id, plots_json, constructions_json, updated_at
     FROM character_build_states
     WHERE character_id = ? AND realm_id = ?
     LIMIT 1`,
    [characterId, realmId]
  );

  return rows[0] ? mapRow(rows[0]) : undefined;
}

export async function upsertBuildState(
  characterId: string,
  realmId: string,
  plots: JsonValue[],
  constructions: JsonValue[],
  executor: DbExecutor = db
): Promise<BuildStateSnapshot> {
  const existing = await getBuildState(characterId, realmId, executor);
  const now = new Date().toISOString();
  const payloads = {
    plotsJson: JSON.stringify(plots ?? []),
    constructionsJson: JSON.stringify(constructions ?? []),
  };

  if (existing) {
    await executor.execute(
      `UPDATE character_build_states
       SET plots_json = ?, constructions_json = ?, updated_at = ?
       WHERE id = ?`,
      [payloads.plotsJson, payloads.constructionsJson, now, existing.id]
    );

    return {
      ...existing,
      plots: plots ?? [],
      constructions: constructions ?? [],
      updatedAt: now,
    };
  }

  const snapshot: BuildStateSnapshot = {
    id: randomUUID(),
    realmId,
    characterId,
    plots: plots ?? [],
    constructions: constructions ?? [],
    updatedAt: now,
  };

  await executor.execute(
    `INSERT INTO character_build_states
       (id, realm_id, character_id, plots_json, constructions_json, updated_at)
     VALUES
       (?, ?, ?, ?, ?, ?)`,
    [
      snapshot.id,
      snapshot.realmId,
      snapshot.characterId,
      payloads.plotsJson,
      payloads.constructionsJson,
      snapshot.updatedAt,
    ]
  );

  return snapshot;
}
