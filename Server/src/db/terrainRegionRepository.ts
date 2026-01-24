import { randomUUID } from 'crypto';
import { DbExecutor } from './database';
import { terrainDb } from './terrainDatabase';

export interface TerrainRegionRecord {
  id: string;
  realmId: string;
  name: string;
  boundsJson: string;
  terrainCount: number;
  payloadJson: string;
  createdAt: string;
  updatedAt: string;
}

interface UpsertTerrainRegionInput {
  id?: string;
  realmId: string;
  name: string;
  boundsJson: string;
  terrainCount: number;
  payloadJson: string;
}

function mapRow(row: any): TerrainRegionRecord {
  return {
    id: row.id,
    realmId: row.realm_id,
    name: row.name,
    boundsJson: row.bounds_json,
    terrainCount: row.terrain_count,
    payloadJson: row.payload_json,
    createdAt: row.created_at,
    updatedAt: row.updated_at,
  };
}

export async function listTerrainRegionsByRealm(
  realmId: string,
  executor: DbExecutor = terrainDb
): Promise<TerrainRegionRecord[]> {
  const rows = await executor.query(
    `SELECT id, realm_id, name, bounds_json, terrain_count, payload_json, created_at, updated_at
     FROM terrain_regions
     WHERE realm_id = ?
     ORDER BY updated_at DESC`,
    [realmId]
  );
  return rows.map(mapRow);
}

export async function findTerrainRegionById(
  realmId: string,
  regionId: string,
  executor: DbExecutor = terrainDb
): Promise<TerrainRegionRecord | undefined> {
  const rows = await executor.query(
    `SELECT id, realm_id, name, bounds_json, terrain_count, payload_json, created_at, updated_at
     FROM terrain_regions
     WHERE realm_id = ? AND id = ?`,
    [realmId, regionId]
  );
  const row = rows[0];
  return row ? mapRow(row) : undefined;
}

export async function upsertTerrainRegion(
  input: UpsertTerrainRegionInput,
  executor: DbExecutor = terrainDb
): Promise<TerrainRegionRecord> {
  const now = new Date().toISOString();
  const regionId = input.id ?? randomUUID();
  await executor.execute(
    `INSERT INTO terrain_regions
       (id, realm_id, name, bounds_json, terrain_count, payload_json, created_at, updated_at)
     VALUES (?, ?, ?, ?, ?, ?, ?, ?)
     ON DUPLICATE KEY UPDATE
       name = VALUES(name),
       bounds_json = VALUES(bounds_json),
       terrain_count = VALUES(terrain_count),
       payload_json = VALUES(payload_json),
       updated_at = VALUES(updated_at)`,
    [
      regionId,
      input.realmId,
      input.name,
      input.boundsJson,
      input.terrainCount,
      input.payloadJson,
      now,
      now,
    ]
  );
  return (await findTerrainRegionById(input.realmId, regionId, executor))!;
}
