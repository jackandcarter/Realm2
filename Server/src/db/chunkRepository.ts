import { randomUUID } from 'crypto';
import { DbExecutor } from './database';
import { terrainDb } from './terrainDatabase';
import { setReplicationQueueLength } from '../observability/metrics';

export interface RealmChunkRecord {
  id: string;
  realmId: string;
  chunkX: number;
  chunkZ: number;
  payloadJson: string;
  isDeleted: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface ChunkStructureRecord {
  id: string;
  realmId: string;
  chunkId: string;
  structureType: string;
  dataJson: string;
  isDeleted: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface ChunkPlotRecord {
  id: string;
  realmId: string;
  chunkId: string;
  plotIdentifier: string;
  ownerUserId: string | null;
  dataJson: string;
  isDeleted: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface ChunkChangeRecord {
  id: string;
  realmId: string;
  chunkId: string;
  changeType: string;
  payloadJson: string;
  createdAt: string;
}

interface UpsertChunkInput {
  id: string;
  realmId: string;
  chunkX: number;
  chunkZ: number;
  payloadJson: string;
  isDeleted: boolean;
}

interface UpsertStructureInput {
  id: string;
  realmId: string;
  chunkId: string;
  structureType: string;
  dataJson: string;
  isDeleted: boolean;
}

interface UpsertPlotInput {
  id: string;
  realmId: string;
  chunkId: string;
  plotIdentifier: string;
  ownerUserId: string | null;
  dataJson: string;
  isDeleted: boolean;
}

function mapChunkRow(row: any): RealmChunkRecord {
  return {
    id: row.id,
    realmId: row.realm_id,
    chunkX: row.chunk_x,
    chunkZ: row.chunk_z,
    payloadJson: row.payload_json,
    isDeleted: Boolean(row.is_deleted),
    createdAt: row.created_at,
    updatedAt: row.updated_at,
  };
}

function mapStructureRow(row: any): ChunkStructureRecord {
  return {
    id: row.id,
    realmId: row.realm_id,
    chunkId: row.chunk_id,
    structureType: row.structure_type,
    dataJson: row.data_json,
    isDeleted: Boolean(row.is_deleted),
    createdAt: row.created_at,
    updatedAt: row.updated_at,
  };
}

function mapPlotRow(row: any): ChunkPlotRecord {
  return {
    id: row.id,
    realmId: row.realm_id,
    chunkId: row.chunk_id,
    plotIdentifier: row.plot_identifier,
    ownerUserId: row.owner_user_id ?? null,
    dataJson: row.data_json,
    isDeleted: Boolean(row.is_deleted),
    createdAt: row.created_at,
    updatedAt: row.updated_at,
  };
}

function mapChangeRow(row: any): ChunkChangeRecord {
  return {
    id: row.id,
    realmId: row.realm_id,
    chunkId: row.chunk_id,
    changeType: row.change_type,
    payloadJson: row.payload_json,
    createdAt: row.created_at,
  };
}

async function refreshReplicationGauge(
  realmId: string,
  executor: DbExecutor = terrainDb
): Promise<void> {
  const rows = await executor.query<{ total: number }[]>(
    'SELECT COUNT(*) as total FROM chunk_change_log WHERE realm_id = ?',
    [realmId]
  );
  const total = typeof rows[0]?.total === 'number' ? rows[0].total : 0;
  setReplicationQueueLength(realmId, total);
}

export async function findChunkById(
  id: string,
  executor: DbExecutor = terrainDb
): Promise<RealmChunkRecord | undefined> {
  const rows = await executor.query(
    `SELECT id, realm_id, chunk_x, chunk_z, payload_json, is_deleted, created_at, updated_at
     FROM realm_chunks
     WHERE id = ?`,
    [id]
  );
  const row = rows[0];
  return row ? mapChunkRow(row) : undefined;
}

export async function findChunkByRealmAndCoords(
  realmId: string,
  chunkX: number,
  chunkZ: number,
  executor: DbExecutor = terrainDb
): Promise<RealmChunkRecord | undefined> {
  const rows = await executor.query(
    `SELECT id, realm_id, chunk_x, chunk_z, payload_json, is_deleted, created_at, updated_at
     FROM realm_chunks
     WHERE realm_id = ? AND chunk_x = ? AND chunk_z = ?`,
    [realmId, chunkX, chunkZ]
  );
  const row = rows[0];
  return row ? mapChunkRow(row) : undefined;
}

export async function upsertChunk(
  input: UpsertChunkInput,
  executor: DbExecutor = terrainDb
): Promise<RealmChunkRecord> {
  const now = new Date().toISOString();
  await executor.execute(
    `INSERT INTO realm_chunks (id, realm_id, chunk_x, chunk_z, payload_json, is_deleted, created_at, updated_at)
     VALUES (?, ?, ?, ?, ?, ?, ?, ?)
     ON DUPLICATE KEY UPDATE
       realm_id = VALUES(realm_id),
       chunk_x = VALUES(chunk_x),
       chunk_z = VALUES(chunk_z),
       payload_json = VALUES(payload_json),
       is_deleted = VALUES(is_deleted),
       updated_at = VALUES(updated_at)`,
    [
      input.id,
      input.realmId,
      input.chunkX,
      input.chunkZ,
      input.payloadJson,
      input.isDeleted ? 1 : 0,
      now,
      now,
    ]
  );
  return (await findChunkById(input.id, executor))!;
}

export async function listChunksByRealm(
  realmId: string,
  updatedAfter?: string,
  executor: DbExecutor = terrainDb
): Promise<RealmChunkRecord[]> {
  if (updatedAfter) {
    const rows = await executor.query(
      `SELECT id, realm_id, chunk_x, chunk_z, payload_json, is_deleted, created_at, updated_at
       FROM realm_chunks
       WHERE realm_id = ? AND updated_at > ?
       ORDER BY updated_at ASC`,
      [realmId, updatedAfter]
    );
    return rows.map(mapChunkRow);
  }
  const rows = await executor.query(
    `SELECT id, realm_id, chunk_x, chunk_z, payload_json, is_deleted, created_at, updated_at
     FROM realm_chunks
     WHERE realm_id = ?
     ORDER BY updated_at ASC`,
    [realmId]
  );
  return rows.map(mapChunkRow);
}

export async function listStructuresForChunks(
  chunkIds: string[],
  executor: DbExecutor = terrainDb
): Promise<ChunkStructureRecord[]> {
  if (chunkIds.length === 0) {
    return [];
  }
  const placeholders = chunkIds.map(() => '?').join(', ');
  const rows = await executor.query(
    `SELECT id, realm_id, chunk_id, structure_type, data_json, is_deleted, created_at, updated_at
     FROM chunk_structures
     WHERE chunk_id IN (${placeholders}) AND is_deleted = 0
     ORDER BY updated_at ASC`,
    chunkIds
  );
  return rows.map(mapStructureRow);
}

export async function listPlotsForChunks(
  chunkIds: string[],
  executor: DbExecutor = terrainDb
): Promise<ChunkPlotRecord[]> {
  if (chunkIds.length === 0) {
    return [];
  }
  const placeholders = chunkIds.map(() => '?').join(', ');
  const rows = await executor.query(
    `SELECT id, realm_id, chunk_id, plot_identifier, owner_user_id, data_json, is_deleted, created_at, updated_at
     FROM chunk_plots
     WHERE chunk_id IN (${placeholders}) AND is_deleted = 0
     ORDER BY updated_at ASC`,
    chunkIds
  );
  return rows.map(mapPlotRow);
}

export async function findPlotById(
  plotId: string,
  executor: DbExecutor = terrainDb
): Promise<ChunkPlotRecord | undefined> {
  const rows = await executor.query(
    `SELECT id, realm_id, chunk_id, plot_identifier, owner_user_id, data_json, is_deleted, created_at, updated_at
     FROM chunk_plots
     WHERE id = ?`,
    [plotId]
  );
  const row = rows[0];
  return row ? mapPlotRow(row) : undefined;
}

export async function findPlotByIdentifier(
  realmId: string,
  chunkId: string,
  plotIdentifier: string,
  executor: DbExecutor = terrainDb
): Promise<ChunkPlotRecord | undefined> {
  const rows = await executor.query(
    `SELECT id, realm_id, chunk_id, plot_identifier, owner_user_id, data_json, is_deleted, created_at, updated_at
     FROM chunk_plots
     WHERE realm_id = ? AND chunk_id = ? AND plot_identifier = ?`,
    [realmId, chunkId, plotIdentifier]
  );
  const row = rows[0];
  return row ? mapPlotRow(row) : undefined;
}

export async function upsertStructures(
  structures: UpsertStructureInput[],
  executor: DbExecutor = terrainDb
): Promise<ChunkStructureRecord[]> {
  if (structures.length === 0) {
    return [];
  }
  const now = new Date().toISOString();
  const results: ChunkStructureRecord[] = [];
  const tx = executor;
  for (const item of structures) {
    await tx.execute(
      `INSERT INTO chunk_structures (id, realm_id, chunk_id, structure_type, data_json, is_deleted, created_at, updated_at)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?)
       ON DUPLICATE KEY UPDATE
         realm_id = VALUES(realm_id),
         chunk_id = VALUES(chunk_id),
         structure_type = VALUES(structure_type),
         data_json = VALUES(data_json),
         is_deleted = VALUES(is_deleted),
         updated_at = VALUES(updated_at)`,
      [
        item.id,
        item.realmId,
        item.chunkId,
        item.structureType,
        item.dataJson,
        item.isDeleted ? 1 : 0,
        now,
        now,
      ]
    );
    const rows = await tx.query(
      `SELECT id, realm_id, chunk_id, structure_type, data_json, is_deleted, created_at, updated_at
       FROM chunk_structures
       WHERE id = ?`,
      [item.id]
    );
    const row = rows[0];
    if (row) {
      results.push(mapStructureRow(row));
    }
  }
  return results;
}

export async function upsertPlots(
  plots: UpsertPlotInput[],
  executor: DbExecutor = terrainDb
): Promise<ChunkPlotRecord[]> {
  if (plots.length === 0) {
    return [];
  }
  const now = new Date().toISOString();
  const results: ChunkPlotRecord[] = [];
  const tx = executor;
  for (const item of plots) {
    await tx.execute(
      `INSERT INTO chunk_plots (id, realm_id, chunk_id, plot_identifier, owner_user_id, data_json, is_deleted, created_at, updated_at)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
       ON DUPLICATE KEY UPDATE
         realm_id = VALUES(realm_id),
         chunk_id = VALUES(chunk_id),
         plot_identifier = VALUES(plot_identifier),
         owner_user_id = VALUES(owner_user_id),
         data_json = VALUES(data_json),
         is_deleted = VALUES(is_deleted),
         updated_at = VALUES(updated_at)`,
      [
        item.id,
        item.realmId,
        item.chunkId,
        item.plotIdentifier,
        item.ownerUserId,
        item.dataJson,
        item.isDeleted ? 1 : 0,
        now,
        now,
      ]
    );
    const rows = await tx.query(
      `SELECT id, realm_id, chunk_id, plot_identifier, owner_user_id, data_json, is_deleted, created_at, updated_at
       FROM chunk_plots
       WHERE id = ?`,
      [item.id]
    );
    const row = rows[0];
    if (row) {
      results.push(mapPlotRow(row));
    }
  }
  return results;
}

export async function logChunkChange(
  realmId: string,
  chunkId: string,
  changeType: string,
  payloadJson: string,
  executor: DbExecutor = terrainDb
): Promise<ChunkChangeRecord> {
  const change: ChunkChangeRecord = {
    id: randomUUID(),
    realmId,
    chunkId,
    changeType,
    payloadJson,
    createdAt: new Date().toISOString(),
  };
  await executor.execute(
    `INSERT INTO chunk_change_log (id, realm_id, chunk_id, change_type, payload_json, created_at)
     VALUES (?, ?, ?, ?, ?, ?)`,
    [change.id, change.realmId, change.chunkId, change.changeType, change.payloadJson, change.createdAt]
  );
  await refreshReplicationGauge(realmId, executor);
  return change;
}

export async function listChunkChanges(
  realmId: string,
  createdAfter?: string,
  limit = 500,
  executor: DbExecutor = terrainDb
): Promise<ChunkChangeRecord[]> {
  if (createdAfter) {
    const rows = await executor.query(
      `SELECT id, realm_id, chunk_id, change_type, payload_json, created_at
       FROM chunk_change_log
       WHERE realm_id = ? AND created_at > ?
       ORDER BY created_at ASC
       LIMIT ?`,
      [realmId, createdAfter, limit]
    );
    return rows.map(mapChangeRow);
  }
  const rows = await executor.query(
    `SELECT id, realm_id, chunk_id, change_type, payload_json, created_at
     FROM chunk_change_log
     WHERE realm_id = ?
     ORDER BY created_at ASC
     LIMIT ?`,
    [realmId, limit]
  );
  return rows.map(mapChangeRow);
}

export async function deleteChunkChangeLogBefore(
  realmId: string,
  cutoff: string,
  executor: DbExecutor = terrainDb
): Promise<void> {
  await executor.execute(`DELETE FROM chunk_change_log WHERE realm_id = ? AND created_at < ?`, [
    realmId,
    cutoff,
  ]);
  await refreshReplicationGauge(realmId, executor);
}
