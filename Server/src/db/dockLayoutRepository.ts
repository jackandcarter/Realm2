import { randomUUID } from 'crypto';
import { db, DbExecutor } from './database';

export interface DockLayoutRecord {
  id: string;
  characterId: string;
  layoutKey: string;
  layoutJson: string;
  updatedAt: string;
}

function mapRow(row: any): DockLayoutRecord {
  return {
    id: row.id,
    characterId: row.character_id,
    layoutKey: row.layout_key,
    layoutJson: row.layout_json,
    updatedAt: row.updated_at,
  };
}

export async function getDockLayout(
  characterId: string,
  layoutKey: string,
  executor: DbExecutor = db
): Promise<DockLayoutRecord | undefined> {
  const rows = await executor.query<any[]>(
    `SELECT id, character_id, layout_key, layout_json, updated_at
     FROM character_dock_layouts
     WHERE character_id = ? AND layout_key = ?`,
    [characterId, layoutKey]
  );
  const row = rows[0];
  return row ? mapRow(row) : undefined;
}

export async function upsertDockLayout(
  characterId: string,
  layoutKey: string,
  layoutJson: string,
  executor: DbExecutor = db
): Promise<DockLayoutRecord> {
  const now = new Date().toISOString();
  const existing = await getDockLayout(characterId, layoutKey, executor);
  if (existing) {
    await executor.execute(
      `UPDATE character_dock_layouts
       SET layout_json = ?, updated_at = ?
       WHERE id = ?`,
      [layoutJson, now, existing.id]
    );
    return { ...existing, layoutJson, updatedAt: now };
  }

  const record: DockLayoutRecord = {
    id: randomUUID(),
    characterId,
    layoutKey,
    layoutJson,
    updatedAt: now,
  };
  await executor.execute(
    `INSERT INTO character_dock_layouts (id, character_id, layout_key, layout_json, updated_at)
     VALUES (?, ?, ?, ?, ?)`,
    [record.id, record.characterId, record.layoutKey, record.layoutJson, record.updatedAt]
  );
  return record;
}
