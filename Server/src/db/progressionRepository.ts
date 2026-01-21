import { randomUUID } from 'crypto';
import { db } from './database';
import { JsonValue } from '../types/characterCustomization';
import { recordVersionConflict } from '../observability/metrics';
import { isClassAllowedForRace } from '../config/classRules';

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

export interface EquipmentItemRecord {
  classId: string;
  slot: string;
  itemId: string;
  metadataJson?: string;
}

export interface EquipmentCollection {
  version: number;
  updatedAt: string;
  items: EquipmentItemRecord[];
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
  equipment: EquipmentCollection;
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
  metadataJson?: string | undefined;
}

export interface EquipmentItemInput {
  classId: string;
  slot: string;
  itemId: string;
  metadata?: JsonValue | undefined;
  metadataJson?: string | undefined;
}

export interface QuestStateInput {
  questId: string;
  status: string;
  progress?: JsonValue | undefined;
}

export class VersionConflictError extends Error {
  constructor(
    public readonly entity: 'progression' | 'classUnlocks' | 'inventory' | 'equipment' | 'quests',
    public readonly expected: number,
    public readonly actual: number
  ) {
    super(`${entity} version conflict`);
    this.name = 'VersionConflictError';
  }
}

export class ForbiddenClassUnlockError extends Error {
  constructor(
    public readonly characterId: string,
    public readonly raceId: string,
    public readonly classIds: string[],
  ) {
    super(
      classIds.length === 1
        ? `Class ${classIds[0]} cannot be unlocked for race ${raceId}`
        : `Classes ${classIds.join(', ')} cannot be unlocked for race ${raceId}`,
    );
    this.name = 'ForbiddenClassUnlockError';
  }
}

export class ForbiddenClassEquipmentError extends Error {
  constructor(
    public readonly characterId: string,
    public readonly raceId: string,
    public readonly classIds: string[],
  ) {
    super(
      classIds.length === 1
        ? `Equipment for class ${classIds[0]} cannot be stored for race ${raceId}`
        : `Equipment for classes ${classIds.join(', ')} cannot be stored for race ${raceId}`,
    );
    this.name = 'ForbiddenClassEquipmentError';
  }
}

export class InvalidEquipmentCatalogError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'InvalidEquipmentCatalogError';
  }
}

async function ensureProgressionRow(characterId: string): Promise<void> {
  const now = new Date().toISOString();
  await db.execute(
    `INSERT INTO character_progression (character_id, level, xp, version, updated_at)
     VALUES (?, 1, 0, 0, ?)
     ON DUPLICATE KEY UPDATE updated_at = updated_at`,
    [characterId, now]
  );
}

async function ensureClassUnlockMeta(characterId: string): Promise<void> {
  const now = new Date().toISOString();
  await db.execute(
    `INSERT INTO character_class_unlock_state (character_id, version, updated_at)
     VALUES (?, 0, ?)
     ON DUPLICATE KEY UPDATE updated_at = updated_at`,
    [characterId, now]
  );
}

async function ensureInventoryMeta(characterId: string): Promise<void> {
  const now = new Date().toISOString();
  await db.execute(
    `INSERT INTO character_inventory_state (character_id, version, updated_at)
     VALUES (?, 0, ?)
     ON DUPLICATE KEY UPDATE updated_at = updated_at`,
    [characterId, now]
  );
}

async function ensureEquipmentMeta(characterId: string): Promise<void> {
  const now = new Date().toISOString();
  await db.execute(
    `INSERT INTO character_equipment_state (character_id, version, updated_at)
     VALUES (?, 0, ?)
     ON DUPLICATE KEY UPDATE updated_at = updated_at`,
    [characterId, now]
  );
}

async function ensureQuestMeta(characterId: string): Promise<void> {
  const now = new Date().toISOString();
  await db.execute(
    `INSERT INTO character_quest_state_meta (character_id, version, updated_at)
     VALUES (?, 0, ?)
     ON DUPLICATE KEY UPDATE updated_at = updated_at`,
    [characterId, now]
  );
}

export async function initializeCharacterProgressionState(characterId: string): Promise<void> {
  await ensureProgressionRow(characterId);
  await ensureClassUnlockMeta(characterId);
  await ensureInventoryMeta(characterId);
  await ensureEquipmentMeta(characterId);
  await ensureQuestMeta(characterId);
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

interface MetadataInput {
  metadata?: JsonValue | undefined;
  metadataJson?: string | undefined;
}

function normalizeInventoryMetadata(item: MetadataInput): JsonValue | undefined {
  if (typeof item.metadata !== 'undefined') {
    return item.metadata;
  }

  if (typeof item.metadataJson === 'string' && item.metadataJson.trim().length > 0) {
    try {
      return JSON.parse(item.metadataJson);
    } catch (_error) {
      return undefined;
    }
  }

  return undefined;
}

export async function getCharacterProgressionSnapshot(
  characterId: string
): Promise<CharacterProgressionSnapshot> {
  await ensureProgressionRow(characterId);
  await ensureClassUnlockMeta(characterId);
  await ensureInventoryMeta(characterId);
  await ensureEquipmentMeta(characterId);
  await ensureQuestMeta(characterId);

  const progressionRows = await db.query<CharacterProgressionStateRow[]>(
    `SELECT level, xp, version, updated_at as updatedAt
     FROM character_progression
     WHERE character_id = ?`,
    [characterId]
  );
  const progressionRow = progressionRows[0];

  const classUnlockRows = await db.query<ClassUnlockRow[]>(
    `SELECT class_id as classId, unlocked, unlocked_at as unlockedAt
     FROM character_class_unlocks
     WHERE character_id = ?`,
    [characterId]
  );

  const classMetaRows = await db.query<VersionRow[]>(
    `SELECT version, updated_at as updatedAt
     FROM character_class_unlock_state
     WHERE character_id = ?`,
    [characterId]
  );
  const classMeta = classMetaRows[0];

  const inventoryRows = await db.query<InventoryRow[]>(
    `SELECT item_id as itemId, quantity, metadata_json as metadataJson
     FROM character_inventory_items
     WHERE character_id = ?`,
    [characterId]
  );

  const inventoryMetaRows = await db.query<VersionRow[]>(
    `SELECT version, updated_at as updatedAt
     FROM character_inventory_state
     WHERE character_id = ?`,
    [characterId]
  );
  const inventoryMeta = inventoryMetaRows[0];

  const equipmentRows = await db.query<EquipmentRow[]>(
    `SELECT class_id as classId, slot, item_id as itemId, metadata_json as metadataJson
     FROM character_equipment_items
     WHERE character_id = ?`,
    [characterId]
  );

  const equipmentMetaRows = await db.query<VersionRow[]>(
    `SELECT version, updated_at as updatedAt
     FROM character_equipment_state
     WHERE character_id = ?`,
    [characterId]
  );
  const equipmentMeta = equipmentMetaRows[0];

  const questRows = await db.query<QuestRow[]>(
    `SELECT quest_id as questId, status, progress_json as progressJson, updated_at as updatedAt
     FROM character_quest_states
     WHERE character_id = ?`,
    [characterId]
  );

  const questMetaRows = await db.query<VersionRow[]>(
    `SELECT version, updated_at as updatedAt
     FROM character_quest_state_meta
     WHERE character_id = ?`,
    [characterId]
  );
  const questMeta = questMetaRows[0];

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
    equipment: {
      version: equipmentMeta?.version ?? 0,
      updatedAt: equipmentMeta?.updatedAt ?? new Date().toISOString(),
      items: (equipmentRows ?? []).map((row) => ({
        classId: row.classId,
        slot: row.slot,
        itemId: row.itemId,
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

export async function updateProgressionLevels(
  characterId: string,
  level: number,
  xp: number,
  expectedVersion: number
): Promise<CharacterProgressionState> {
  await ensureProgressionRow(characterId);
  const currentRows = await db.query<CharacterProgressionStateRow[]>(
    `SELECT level, xp, version, updated_at as updatedAt
     FROM character_progression
     WHERE character_id = ?`,
    [characterId]
  );
  const current = currentRows[0];

  if (typeof current?.version !== 'number') {
    recordVersionConflict('progression');
    throw new VersionConflictError('progression', expectedVersion, 0);
  }

  if (current.version !== expectedVersion) {
    recordVersionConflict('progression');
    throw new VersionConflictError('progression', expectedVersion, current.version);
  }

  const updatedAt = new Date().toISOString();
  await db.execute(
    `UPDATE character_progression
     SET level = ?,
         xp = ?,
         version = version + 1,
         updated_at = ?
     WHERE character_id = ?`,
    [level, xp, updatedAt, characterId]
  );

  return {
    level,
    xp,
    version: current.version + 1,
    updatedAt,
  };
}

export async function replaceClassUnlocks(
  characterId: string,
  unlocks: ClassUnlockInput[],
  expectedVersion: number
): Promise<ClassUnlockCollection> {
  await ensureClassUnlockMeta(characterId);
  const currentRows = await db.query<VersionRow[]>(
    `SELECT version, updated_at as updatedAt
     FROM character_class_unlock_state
     WHERE character_id = ?`,
    [characterId]
  );
  const current = currentRows[0];

  const actualVersion = current?.version ?? 0;
  if (actualVersion !== expectedVersion) {
    recordVersionConflict('classUnlocks');
    throw new VersionConflictError('classUnlocks', expectedVersion, actualVersion);
  }

  const characterRaceRows = await db.query<CharacterRaceRow[]>(
    `SELECT race_id as raceId
     FROM characters
     WHERE id = ?`,
    [characterId]
  );
  const characterRaceRow = characterRaceRows[0];

  if (!characterRaceRow) {
    throw new Error(`Character ${characterId} not found while updating class unlocks`);
  }

  const raceId = characterRaceRow.raceId?.trim() || 'human';
  const forbiddenClassIds = Array.from(
    new Set(
      unlocks
        .filter((unlock) => unlock.unlocked)
        .map((unlock) => unlock.classId?.trim() ?? '')
        .filter((classId) => classId && !isClassAllowedForRace(classId, raceId)),
    ),
  );

  if (forbiddenClassIds.length > 0) {
    throw new ForbiddenClassUnlockError(characterId, raceId, forbiddenClassIds);
  }

  const now = new Date().toISOString();
  await db.withTransaction(async (tx) => {
    await tx.execute(`DELETE FROM character_class_unlocks WHERE character_id = ?`, [characterId]);

    for (const unlock of unlocks) {
      await tx.execute(
        `INSERT INTO character_class_unlocks (id, character_id, class_id, unlocked, unlocked_at)
         VALUES (?, ?, ?, ?, ?)
         ON DUPLICATE KEY UPDATE unlocked = VALUES(unlocked), unlocked_at = VALUES(unlocked_at)`,
        [
          randomUUID(),
          characterId,
          unlock.classId,
          unlock.unlocked ? 1 : 0,
          unlock.unlocked ? unlock.unlockedAt ?? now : null,
        ]
      );
    }

    await tx.execute(
      `UPDATE character_class_unlock_state
       SET version = version + 1,
           updated_at = ?
       WHERE character_id = ?`,
      [now, characterId]
    );
  });

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

export async function replaceInventory(
  characterId: string,
  items: InventoryItemInput[],
  expectedVersion: number
): Promise<InventoryCollection> {
  await ensureInventoryMeta(characterId);
  const currentRows = await db.query<VersionRow[]>(
    `SELECT version, updated_at as updatedAt
     FROM character_inventory_state
     WHERE character_id = ?`,
    [characterId]
  );
  const current = currentRows[0];

  const actualVersion = current?.version ?? 0;
  if (actualVersion !== expectedVersion) {
    recordVersionConflict('inventory');
    throw new VersionConflictError('inventory', expectedVersion, actualVersion);
  }

  const now = new Date().toISOString();
  await db.withTransaction(async (tx) => {
    await tx.execute(`DELETE FROM character_inventory_items WHERE character_id = ?`, [
      characterId,
    ]);

    for (const item of items) {
      const metadata = normalizeInventoryMetadata(item);
      await tx.execute(
        `INSERT INTO character_inventory_items (id, character_id, item_id, quantity, metadata_json)
         VALUES (?, ?, ?, ?, ?)
         ON DUPLICATE KEY UPDATE quantity = VALUES(quantity), metadata_json = VALUES(metadata_json)`,
        [randomUUID(), characterId, item.itemId, item.quantity, serializeJson(metadata)]
      );
    }

    await tx.execute(
      `UPDATE character_inventory_state
       SET version = version + 1,
           updated_at = ?
       WHERE character_id = ?`,
      [now, characterId]
    );
  });

  return {
    version: actualVersion + 1,
    updatedAt: now,
    items: items.map((item) => ({
      itemId: item.itemId,
      quantity: item.quantity,
      metadataJson: serializeJson(normalizeInventoryMetadata(item)),
    })),
  };
}

export async function replaceEquipment(
  characterId: string,
  items: EquipmentItemInput[],
  expectedVersion: number
): Promise<EquipmentCollection> {
  await ensureEquipmentMeta(characterId);
  const currentRows = await db.query<VersionRow[]>(
    `SELECT version, updated_at as updatedAt
     FROM character_equipment_state
     WHERE character_id = ?`,
    [characterId]
  );
  const current = currentRows[0];

  const actualVersion = current?.version ?? 0;
  if (actualVersion !== expectedVersion) {
    recordVersionConflict('equipment');
    throw new VersionConflictError('equipment', expectedVersion, actualVersion);
  }

  const characterRaceRows = await db.query<CharacterRaceRow[]>(
    `SELECT race_id as raceId
     FROM characters
     WHERE id = ?`,
    [characterId]
  );
  const characterRaceRow = characterRaceRows[0];
  if (!characterRaceRow) {
    throw new Error(`Character ${characterId} not found while updating equipment`);
  }

  const raceId = characterRaceRow.raceId?.trim() || 'human';
  const forbiddenClassIds = Array.from(
    new Set(
      items
        .map((entry) => entry.classId?.trim() ?? '')
        .filter((classId) => classId && !isClassAllowedForRace(classId, raceId)),
    ),
  );

  if (forbiddenClassIds.length > 0) {
    throw new ForbiddenClassEquipmentError(characterId, raceId, forbiddenClassIds);
  }

  assertEquipmentMatchesCatalog(items);

  const now = new Date().toISOString();
  await db.withTransaction(async (tx) => {
    await tx.execute(`DELETE FROM character_equipment_items WHERE character_id = ?`, [
      characterId,
    ]);

    for (const item of items) {
      const metadata = normalizeInventoryMetadata(item);
      await tx.execute(
        `INSERT INTO character_equipment_items (id, character_id, class_id, slot, item_id, metadata_json)
         VALUES (?, ?, ?, ?, ?, ?)
         ON DUPLICATE KEY UPDATE item_id = VALUES(item_id), metadata_json = VALUES(metadata_json)`,
        [
          randomUUID(),
          characterId,
          item.classId,
          item.slot,
          item.itemId,
          serializeJson(metadata),
        ]
      );
    }

    await tx.execute(
      `UPDATE character_equipment_state
       SET version = version + 1,
           updated_at = ?
       WHERE character_id = ?`,
      [now, characterId]
    );
  });

  return {
    version: actualVersion + 1,
    updatedAt: now,
    items: items.map((item) => ({
      classId: item.classId,
      slot: item.slot,
      itemId: item.itemId,
      metadataJson: serializeJson(normalizeInventoryMetadata(item)),
    })),
  };
}

function assertEquipmentMatchesCatalog(items: EquipmentItemInput[]): void {
  for (const item of items) {
    const slot = item.slot?.trim();
    const itemId = item.itemId?.trim();
    if (!slot || !itemId) {
      throw new InvalidEquipmentCatalogError('Equipment entries must include slot and itemId.');
    }
  }
}

export async function replaceQuestStates(
  characterId: string,
  quests: QuestStateInput[],
  expectedVersion: number
): Promise<QuestStateCollection> {
  await ensureQuestMeta(characterId);
  const currentRows = await db.query<VersionRow[]>(
    `SELECT version, updated_at as updatedAt
     FROM character_quest_state_meta
     WHERE character_id = ?`,
    [characterId]
  );
  const current = currentRows[0];

  const actualVersion = current?.version ?? 0;
  if (actualVersion !== expectedVersion) {
    recordVersionConflict('quests');
    throw new VersionConflictError('quests', expectedVersion, actualVersion);
  }

  const now = new Date().toISOString();
  await db.withTransaction(async (tx) => {
    await tx.execute(`DELETE FROM character_quest_states WHERE character_id = ?`, [
      characterId,
    ]);

    for (const quest of quests) {
      await tx.execute(
        `INSERT INTO character_quest_states (id, character_id, quest_id, status, progress_json, updated_at)
         VALUES (?, ?, ?, ?, ?, ?)
         ON DUPLICATE KEY UPDATE status = VALUES(status), progress_json = VALUES(progress_json), updated_at = VALUES(updated_at)`,
        [
          randomUUID(),
          characterId,
          quest.questId,
          quest.status,
          serializeJson(quest.progress),
          now,
        ]
      );
    }

    await tx.execute(
      `UPDATE character_quest_state_meta
       SET version = version + 1,
           updated_at = ?
       WHERE character_id = ?`,
      [now, characterId]
    );
  });

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

interface CharacterRaceRow {
  raceId?: string | null;
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

interface EquipmentRow {
  classId: string;
  slot: string;
  itemId: string;
  metadataJson: string | null;
}
