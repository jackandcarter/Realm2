import { randomUUID } from 'crypto';
import { db } from './database';
import { JsonValue } from '../types/characterCustomization';
import { recordVersionConflict } from '../observability/metrics';

export interface CharacterProgressionState {
  level: number;
  xp: number;
  version: number;
  updatedAt: string;
}

export interface ClassUnlockRecord {
  classId: string;
  unlocked: boolean;
  unlockedAt: string | null;
}

export interface ClassUnlockCollection {
  version: number;
  updatedAt: string;
  unlocks: ClassUnlockRecord[];
}

export interface InventoryItemRecord {
  itemId: string;
  quantity: number;
  metadataJson?: string;
}

export interface InventoryCollection {
  version: number;
  updatedAt: string;
  items: InventoryItemRecord[];
}

export interface QuestStateRecord {
  questId: string;
  status: string;
  progressJson?: string;
  updatedAt: string;
}

export interface QuestStateCollection {
  version: number;
  updatedAt: string;
  quests: QuestStateRecord[];
}

export interface CharacterProgressionSnapshot {
  progression: CharacterProgressionState;
  classUnlocks: ClassUnlockCollection;
  inventory: InventoryCollection;
  quests: QuestStateCollection;
}

export interface ClassUnlockInput {
  classId: string;
  unlocked: boolean;
  unlockedAt?: string | null;
}

export interface InventoryItemInput {
  itemId: string;
  quantity: number;
  metadata?: JsonValue | undefined;
}

export interface QuestStateInput {
  questId: string;
  status: string;
  progress?: JsonValue | undefined;
}

export class VersionConflictError extends Error {
  constructor(
    public readonly entity: 'progression' | 'classUnlocks' | 'inventory' | 'quests',
    public readonly expected: number,
    public readonly actual: number
  ) {
    super(`${entity} version conflict`);
    this.name = 'VersionConflictError';
  }
}

function ensureProgressionRow(characterId: string): void {
  const now = new Date().toISOString();
  const stmt = db.prepare(
    `INSERT INTO character_progression (character_id, level, xp, version, updated_at)
     VALUES (@characterId, 1, 0, 0, @now)
     ON CONFLICT(character_id) DO NOTHING`
  );
  stmt.run({ characterId, now });
}

function ensureClassUnlockMeta(characterId: string): void {
  const now = new Date().toISOString();
  const stmt = db.prepare(
    `INSERT INTO character_class_unlock_state (character_id, version, updated_at)
     VALUES (@characterId, 0, @now)
     ON CONFLICT(character_id) DO NOTHING`
  );
  stmt.run({ characterId, now });
}

function ensureInventoryMeta(characterId: string): void {
  const now = new Date().toISOString();
  const stmt = db.prepare(
    `INSERT INTO character_inventory_state (character_id, version, updated_at)
     VALUES (@characterId, 0, @now)
     ON CONFLICT(character_id) DO NOTHING`
  );
  stmt.run({ characterId, now });
}

function ensureQuestMeta(characterId: string): void {
  const now = new Date().toISOString();
  const stmt = db.prepare(
    `INSERT INTO character_quest_state_meta (character_id, version, updated_at)
     VALUES (@characterId, 0, @now)
     ON CONFLICT(character_id) DO NOTHING`
  );
  stmt.run({ characterId, now });
}

export function initializeCharacterProgressionState(characterId: string): void {
  ensureProgressionRow(characterId);
  ensureClassUnlockMeta(characterId);
  ensureInventoryMeta(characterId);
  ensureQuestMeta(characterId);
}

function serializeJson(value: JsonValue | undefined): string {
  if (typeof value === 'undefined') {
    return '{}';
  }
  try {
    return JSON.stringify(value);
  } catch (_error) {
    return '{}';
  }
}

export function getCharacterProgressionSnapshot(characterId: string): CharacterProgressionSnapshot {
  ensureProgressionRow(characterId);
  ensureClassUnlockMeta(characterId);
  ensureInventoryMeta(characterId);
  ensureQuestMeta(characterId);

  const progressionRow = db.prepare(
    `SELECT level, xp, version, updated_at as updatedAt
     FROM character_progression
     WHERE character_id = ?`
  ).get(characterId) as CharacterProgressionStateRow;

  const classUnlockRows = db
    .prepare(
      `SELECT class_id as classId, unlocked, unlocked_at as unlockedAt
       FROM character_class_unlocks
       WHERE character_id = ?`
    )
    .all(characterId) as ClassUnlockRow[];

  const classMeta = db
    .prepare(
      `SELECT version, updated_at as updatedAt
       FROM character_class_unlock_state
       WHERE character_id = ?`
    )
    .get(characterId) as VersionRow;

  const inventoryRows = db
    .prepare(
      `SELECT item_id as itemId, quantity, metadata_json as metadataJson
       FROM character_inventory_items
       WHERE character_id = ?`
    )
    .all(characterId) as InventoryRow[];

  const inventoryMeta = db
    .prepare(
      `SELECT version, updated_at as updatedAt
       FROM character_inventory_state
       WHERE character_id = ?`
    )
    .get(characterId) as VersionRow;

  const questRows = db
    .prepare(
      `SELECT quest_id as questId, status, progress_json as progressJson, updated_at as updatedAt
       FROM character_quest_states
       WHERE character_id = ?`
    )
    .all(characterId) as QuestRow[];

  const questMeta = db
    .prepare(
      `SELECT version, updated_at as updatedAt
       FROM character_quest_state_meta
       WHERE character_id = ?`
    )
    .get(characterId) as VersionRow;

  return {
    progression: {
      level: progressionRow.level,
      xp: progressionRow.xp,
      version: progressionRow.version,
      updatedAt: progressionRow.updatedAt,
    },
    classUnlocks: {
      version: classMeta?.version ?? 0,
      updatedAt: classMeta?.updatedAt ?? new Date().toISOString(),
      unlocks: (classUnlockRows ?? []).map((row) => ({
        classId: row.classId,
        unlocked: Boolean(row.unlocked),
        unlockedAt: row.unlockedAt ?? null,
      })),
    },
    inventory: {
      version: inventoryMeta?.version ?? 0,
      updatedAt: inventoryMeta?.updatedAt ?? new Date().toISOString(),
      items: (inventoryRows ?? []).map((row) => ({
        itemId: row.itemId,
        quantity: row.quantity,
        metadataJson: row.metadataJson ?? '{}',
      })),
    },
    quests: {
      version: questMeta?.version ?? 0,
      updatedAt: questMeta?.updatedAt ?? new Date().toISOString(),
      quests: (questRows ?? []).map((row) => ({
        questId: row.questId,
        status: row.status,
        progressJson: row.progressJson ?? '{}',
        updatedAt: row.updatedAt,
      })),
    },
  };
}

export function updateProgressionLevels(
  characterId: string,
  level: number,
  xp: number,
  expectedVersion: number
): CharacterProgressionState {
  ensureProgressionRow(characterId);
  const current = db
    .prepare(
      `SELECT level, xp, version, updated_at as updatedAt
       FROM character_progression
       WHERE character_id = ?`
    )
    .get(characterId) as CharacterProgressionStateRow;

  if (typeof current?.version !== 'number') {
    recordVersionConflict('progression');
    throw new VersionConflictError('progression', expectedVersion, 0);
  }

  if (current.version !== expectedVersion) {
    recordVersionConflict('progression');
    throw new VersionConflictError('progression', expectedVersion, current.version);
  }

  const updatedAt = new Date().toISOString();
  const updateStmt = db.prepare(
    `UPDATE character_progression
     SET level = @level,
         xp = @xp,
         version = version + 1,
         updated_at = @updatedAt
     WHERE character_id = @characterId`
  );
  updateStmt.run({ characterId, level, xp, updatedAt });

  return {
    level,
    xp,
    version: current.version + 1,
    updatedAt,
  };
}

export function replaceClassUnlocks(
  characterId: string,
  unlocks: ClassUnlockInput[],
  expectedVersion: number
): ClassUnlockCollection {
  ensureClassUnlockMeta(characterId);
  const current = db
    .prepare(
      `SELECT version, updated_at as updatedAt
       FROM character_class_unlock_state
       WHERE character_id = ?`
    )
    .get(characterId) as VersionRow;

  const actualVersion = current?.version ?? 0;
  if (actualVersion !== expectedVersion) {
    recordVersionConflict('classUnlocks');
    throw new VersionConflictError('classUnlocks', expectedVersion, actualVersion);
  }

  const now = new Date().toISOString();
  const tx = db.transaction(() => {
    db.prepare(`DELETE FROM character_class_unlocks WHERE character_id = ?`).run(characterId);

    if (unlocks.length > 0) {
      const insertStmt = db.prepare(
        `INSERT INTO character_class_unlocks (id, character_id, class_id, unlocked, unlocked_at)
         VALUES (@id, @characterId, @classId, @unlocked, @unlockedAt)
         ON CONFLICT(character_id, class_id) DO UPDATE SET unlocked = excluded.unlocked, unlocked_at = excluded.unlocked_at`
      );

      for (const unlock of unlocks) {
        insertStmt.run({
          id: randomUUID(),
          characterId,
          classId: unlock.classId,
          unlocked: unlock.unlocked ? 1 : 0,
          unlockedAt: unlock.unlocked ? unlock.unlockedAt ?? now : null,
        });
      }
    }

    db.prepare(
      `UPDATE character_class_unlock_state
       SET version = version + 1,
           updated_at = @updatedAt
       WHERE character_id = @characterId`
    ).run({ characterId, updatedAt: now });
  });

  tx();

  return {
    version: actualVersion + 1,
    updatedAt: now,
    unlocks: unlocks.map((unlock) => ({
      classId: unlock.classId,
      unlocked: unlock.unlocked,
      unlockedAt: unlock.unlocked ? unlock.unlockedAt ?? now : null,
    })),
  };
}

export function replaceInventory(
  characterId: string,
  items: InventoryItemInput[],
  expectedVersion: number
): InventoryCollection {
  ensureInventoryMeta(characterId);
  const current = db
    .prepare(
      `SELECT version, updated_at as updatedAt
       FROM character_inventory_state
       WHERE character_id = ?`
    )
    .get(characterId) as VersionRow;

  const actualVersion = current?.version ?? 0;
  if (actualVersion !== expectedVersion) {
    recordVersionConflict('inventory');
    throw new VersionConflictError('inventory', expectedVersion, actualVersion);
  }

  const now = new Date().toISOString();
  const tx = db.transaction(() => {
    db.prepare(`DELETE FROM character_inventory_items WHERE character_id = ?`).run(characterId);

    if (items.length > 0) {
      const insertStmt = db.prepare(
        `INSERT INTO character_inventory_items (id, character_id, item_id, quantity, metadata_json)
         VALUES (@id, @characterId, @itemId, @quantity, @metadataJson)
         ON CONFLICT(character_id, item_id)
         DO UPDATE SET quantity = excluded.quantity, metadata_json = excluded.metadata_json`
      );

      for (const item of items) {
        insertStmt.run({
          id: randomUUID(),
          characterId,
          itemId: item.itemId,
          quantity: item.quantity,
          metadataJson: serializeJson(item.metadata),
        });
      }
    }

    db.prepare(
      `UPDATE character_inventory_state
       SET version = version + 1,
           updated_at = @updatedAt
       WHERE character_id = @characterId`
    ).run({ characterId, updatedAt: now });
  });

  tx();

  return {
    version: actualVersion + 1,
    updatedAt: now,
    items: items.map((item) => ({
      itemId: item.itemId,
      quantity: item.quantity,
      metadataJson: serializeJson(item.metadata),
    })),
  };
}

export function replaceQuestStates(
  characterId: string,
  quests: QuestStateInput[],
  expectedVersion: number
): QuestStateCollection {
  ensureQuestMeta(characterId);
  const current = db
    .prepare(
      `SELECT version, updated_at as updatedAt
       FROM character_quest_state_meta
       WHERE character_id = ?`
    )
    .get(characterId) as VersionRow;

  const actualVersion = current?.version ?? 0;
  if (actualVersion !== expectedVersion) {
    recordVersionConflict('quests');
    throw new VersionConflictError('quests', expectedVersion, actualVersion);
  }

  const now = new Date().toISOString();
  const tx = db.transaction(() => {
    db.prepare(`DELETE FROM character_quest_states WHERE character_id = ?`).run(characterId);

    if (quests.length > 0) {
      const insertStmt = db.prepare(
        `INSERT INTO character_quest_states (id, character_id, quest_id, status, progress_json, updated_at)
         VALUES (@id, @characterId, @questId, @status, @progressJson, @updatedAt)
         ON CONFLICT(character_id, quest_id)
         DO UPDATE SET status = excluded.status, progress_json = excluded.progress_json, updated_at = excluded.updated_at`
      );

      for (const quest of quests) {
        insertStmt.run({
          id: randomUUID(),
          characterId,
          questId: quest.questId,
          status: quest.status,
          progressJson: serializeJson(quest.progress),
          updatedAt: now,
        });
      }
    }

    db.prepare(
      `UPDATE character_quest_state_meta
       SET version = version + 1,
           updated_at = @updatedAt
       WHERE character_id = @characterId`
    ).run({ characterId, updatedAt: now });
  });

  tx();

  return {
    version: actualVersion + 1,
    updatedAt: now,
    quests: quests.map((quest) => ({
      questId: quest.questId,
      status: quest.status,
      progressJson: serializeJson(quest.progress),
      updatedAt: now,
    })),
  };
}

interface CharacterProgressionStateRow {
  level: number;
  xp: number;
  version: number;
  updatedAt: string;
}

interface ClassUnlockRow {
  classId: string;
  unlocked: number;
  unlockedAt: string | null;
}

interface InventoryRow {
  itemId: string;
  quantity: number;
  metadataJson: string | null;
}

interface QuestRow {
  questId: string;
  status: string;
  progressJson: string | null;
  updatedAt: string;
}

interface VersionRow {
  version: number;
  updatedAt: string;
}
