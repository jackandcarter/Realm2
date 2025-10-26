import { randomUUID } from 'crypto';
import { db } from './database';

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

export function findChunkById(id: string): RealmChunkRecord | undefined {
  const stmt = db.prepare(
    `SELECT id, realm_id, chunk_x, chunk_z, payload_json, is_deleted, created_at, updated_at
     FROM realm_chunks
     WHERE id = ?`
  );
  const row = stmt.get(id);
  return row ? mapChunkRow(row) : undefined;
}

export function findChunkByRealmAndCoords(
  realmId: string,
  chunkX: number,
  chunkZ: number
): RealmChunkRecord | undefined {
  const stmt = db.prepare(
    `SELECT id, realm_id, chunk_x, chunk_z, payload_json, is_deleted, created_at, updated_at
     FROM realm_chunks
     WHERE realm_id = ? AND chunk_x = ? AND chunk_z = ?`
  );
  const row = stmt.get(realmId, chunkX, chunkZ);
  return row ? mapChunkRow(row) : undefined;
}

export function upsertChunk(input: UpsertChunkInput): RealmChunkRecord {
  const now = new Date().toISOString();
  const stmt = db.prepare(
    `INSERT INTO realm_chunks (id, realm_id, chunk_x, chunk_z, payload_json, is_deleted, created_at, updated_at)
     VALUES (@id, @realmId, @chunkX, @chunkZ, @payloadJson, @isDeleted, @createdAt, @updatedAt)
     ON CONFLICT(id) DO UPDATE SET
       realm_id = excluded.realm_id,
       chunk_x = excluded.chunk_x,
       chunk_z = excluded.chunk_z,
       payload_json = excluded.payload_json,
       is_deleted = excluded.is_deleted,
       updated_at = excluded.updated_at`
  );
  stmt.run({
    id: input.id,
    realmId: input.realmId,
    chunkX: input.chunkX,
    chunkZ: input.chunkZ,
    payloadJson: input.payloadJson,
    isDeleted: input.isDeleted ? 1 : 0,
    createdAt: now,
    updatedAt: now,
  });
  return findChunkById(input.id)!;
}

export function listChunksByRealm(
  realmId: string,
  updatedAfter?: string
): RealmChunkRecord[] {
  if (updatedAfter) {
    const stmt = db.prepare(
      `SELECT id, realm_id, chunk_x, chunk_z, payload_json, is_deleted, created_at, updated_at
       FROM realm_chunks
       WHERE realm_id = ? AND updated_at > ?
       ORDER BY updated_at ASC`
    );
    return stmt.all(realmId, updatedAfter).map(mapChunkRow);
  }
  const stmt = db.prepare(
    `SELECT id, realm_id, chunk_x, chunk_z, payload_json, is_deleted, created_at, updated_at
     FROM realm_chunks
     WHERE realm_id = ?
     ORDER BY updated_at ASC`
  );
  return stmt.all(realmId).map(mapChunkRow);
}

export function listStructuresForChunks(chunkIds: string[]): ChunkStructureRecord[] {
  if (chunkIds.length === 0) {
    return [];
  }
  const placeholders = chunkIds.map(() => '?').join(', ');
  const stmt = db.prepare(
    `SELECT id, realm_id, chunk_id, structure_type, data_json, is_deleted, created_at, updated_at
     FROM chunk_structures
     WHERE chunk_id IN (${placeholders}) AND is_deleted = 0
     ORDER BY updated_at ASC`
  );
  return stmt.all(...chunkIds).map(mapStructureRow);
}

export function listPlotsForChunks(chunkIds: string[]): ChunkPlotRecord[] {
  if (chunkIds.length === 0) {
    return [];
  }
  const placeholders = chunkIds.map(() => '?').join(', ');
  const stmt = db.prepare(
    `SELECT id, realm_id, chunk_id, plot_identifier, owner_user_id, data_json, is_deleted, created_at, updated_at
     FROM chunk_plots
     WHERE chunk_id IN (${placeholders}) AND is_deleted = 0
     ORDER BY updated_at ASC`
  );
  return stmt.all(...chunkIds).map(mapPlotRow);
}

export function upsertStructures(structures: UpsertStructureInput[]): ChunkStructureRecord[] {
  if (structures.length === 0) {
    return [];
  }
  const stmt = db.prepare(
    `INSERT INTO chunk_structures (id, realm_id, chunk_id, structure_type, data_json, is_deleted, created_at, updated_at)
     VALUES (@id, @realmId, @chunkId, @structureType, @dataJson, @isDeleted, @createdAt, @updatedAt)
     ON CONFLICT(id) DO UPDATE SET
       realm_id = excluded.realm_id,
       chunk_id = excluded.chunk_id,
       structure_type = excluded.structure_type,
       data_json = excluded.data_json,
       is_deleted = excluded.is_deleted,
       updated_at = excluded.updated_at`
  );
  const now = new Date().toISOString();
  const results: ChunkStructureRecord[] = [];
  const selectStmt = db.prepare(
    `SELECT id, realm_id, chunk_id, structure_type, data_json, is_deleted, created_at, updated_at
     FROM chunk_structures
     WHERE id = ?`
  );
  const insertMany = db.transaction((items: UpsertStructureInput[]) => {
    for (const item of items) {
      stmt.run({
        id: item.id,
        realmId: item.realmId,
        chunkId: item.chunkId,
        structureType: item.structureType,
        dataJson: item.dataJson,
        isDeleted: item.isDeleted ? 1 : 0,
        createdAt: now,
        updatedAt: now,
      });
      const row = selectStmt.get(item.id);
      if (row) {
        results.push(mapStructureRow(row));
      }
    }
  });
  insertMany(structures);
  return results;
}

export function upsertPlots(plots: UpsertPlotInput[]): ChunkPlotRecord[] {
  if (plots.length === 0) {
    return [];
  }
  const stmt = db.prepare(
    `INSERT INTO chunk_plots (id, realm_id, chunk_id, plot_identifier, owner_user_id, data_json, is_deleted, created_at, updated_at)
     VALUES (@id, @realmId, @chunkId, @plotIdentifier, @ownerUserId, @dataJson, @isDeleted, @createdAt, @updatedAt)
     ON CONFLICT(id) DO UPDATE SET
       realm_id = excluded.realm_id,
       chunk_id = excluded.chunk_id,
       plot_identifier = excluded.plot_identifier,
       owner_user_id = excluded.owner_user_id,
       data_json = excluded.data_json,
       is_deleted = excluded.is_deleted,
       updated_at = excluded.updated_at`
  );
  const now = new Date().toISOString();
  const results: ChunkPlotRecord[] = [];
  const selectStmt = db.prepare(
    `SELECT id, realm_id, chunk_id, plot_identifier, owner_user_id, data_json, is_deleted, created_at, updated_at
     FROM chunk_plots
     WHERE id = ?`
  );
  const insertMany = db.transaction((items: UpsertPlotInput[]) => {
    for (const item of items) {
      stmt.run({
        id: item.id,
        realmId: item.realmId,
        chunkId: item.chunkId,
        plotIdentifier: item.plotIdentifier,
        ownerUserId: item.ownerUserId,
        dataJson: item.dataJson,
        isDeleted: item.isDeleted ? 1 : 0,
        createdAt: now,
        updatedAt: now,
      });
      const row = selectStmt.get(item.id);
      if (row) {
        results.push(mapPlotRow(row));
      }
    }
  });
  insertMany(plots);
  return results;
}

export function logChunkChange(
  realmId: string,
  chunkId: string,
  changeType: string,
  payloadJson: string
): ChunkChangeRecord {
  const change: ChunkChangeRecord = {
    id: randomUUID(),
    realmId,
    chunkId,
    changeType,
    payloadJson,
    createdAt: new Date().toISOString(),
  };
  const stmt = db.prepare(
    `INSERT INTO chunk_change_log (id, realm_id, chunk_id, change_type, payload_json, created_at)
     VALUES (@id, @realmId, @chunkId, @changeType, @payloadJson, @createdAt)`
  );
  stmt.run(change);
  return change;
}

export function listChunkChanges(
  realmId: string,
  createdAfter?: string,
  limit = 500
): ChunkChangeRecord[] {
  if (createdAfter) {
    const stmt = db.prepare(
      `SELECT id, realm_id, chunk_id, change_type, payload_json, created_at
       FROM chunk_change_log
       WHERE realm_id = ? AND created_at > ?
       ORDER BY created_at ASC
       LIMIT ?`
    );
    return stmt.all(realmId, createdAfter, limit).map(mapChangeRow);
  }
  const stmt = db.prepare(
    `SELECT id, realm_id, chunk_id, change_type, payload_json, created_at
     FROM chunk_change_log
     WHERE realm_id = ?
     ORDER BY created_at ASC
     LIMIT ?`
  );
  return stmt.all(realmId, limit).map(mapChangeRow);
}

export function deleteChunkChangeLogBefore(realmId: string, cutoff: string): void {
  const stmt = db.prepare(
    `DELETE FROM chunk_change_log WHERE realm_id = ? AND created_at < ?`
  );
  stmt.run(realmId, cutoff);
}
