import { db, DbExecutor } from './database';

export interface BuildZoneRecord {
  id: string;
  realmId: string;
  label: string | null;
  centerX: number;
  centerY: number;
  centerZ: number;
  sizeX: number;
  sizeY: number;
  sizeZ: number;
  createdAt: string;
  updatedAt: string;
}

export interface BuildZoneInput {
  id: string;
  realmId: string;
  label: string | null;
  centerX: number;
  centerY: number;
  centerZ: number;
  sizeX: number;
  sizeY: number;
  sizeZ: number;
}

function mapRow(row: any): BuildZoneRecord {
  return {
    id: row.id,
    realmId: row.realm_id,
    label: row.label ?? null,
    centerX: Number(row.center_x),
    centerY: Number(row.center_y),
    centerZ: Number(row.center_z),
    sizeX: Number(row.size_x),
    sizeY: Number(row.size_y),
    sizeZ: Number(row.size_z),
    createdAt: row.created_at,
    updatedAt: row.updated_at,
  };
}

export async function listBuildZones(
  realmId: string,
  executor: DbExecutor = db
): Promise<BuildZoneRecord[]> {
  const rows = await executor.query<any[]>(
    `SELECT id, realm_id, label, center_x, center_y, center_z, size_x, size_y, size_z, created_at, updated_at
     FROM realm_build_zones
     WHERE realm_id = ?
     ORDER BY created_at ASC`,
    [realmId]
  );
  return rows.map(mapRow);
}

export async function replaceBuildZones(
  realmId: string,
  zones: BuildZoneInput[],
  executor: DbExecutor = db
): Promise<void> {
  await executor.execute('DELETE FROM realm_build_zones WHERE realm_id = ?', [realmId]);
  if (!zones || zones.length === 0) {
    return;
  }

  for (const zone of zones) {
    await executor.execute(
      `INSERT INTO realm_build_zones
         (id, realm_id, label, center_x, center_y, center_z, size_x, size_y, size_z)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)`,
      [
        zone.id,
        zone.realmId,
        zone.label,
        zone.centerX,
        zone.centerY,
        zone.centerZ,
        zone.sizeX,
        zone.sizeY,
        zone.sizeZ,
      ]
    );
  }
}
