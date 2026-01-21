import { randomUUID } from 'crypto';
import { db, DbExecutor } from './database';

export interface PlotOwnerRecord {
  plotId: string;
  realmId: string;
  ownerUserId: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface PlotPermissionRecord {
  id: string;
  plotId: string;
  realmId: string;
  userId: string;
  permission: string;
  createdAt: string;
  updatedAt: string;
}

function mapOwnerRow(row: any): PlotOwnerRecord {
  return {
    plotId: row.plot_id,
    realmId: row.realm_id,
    ownerUserId: row.owner_user_id,
    createdAt: row.created_at,
    updatedAt: row.updated_at,
  };
}

function mapPermissionRow(row: any): PlotPermissionRecord {
  return {
    id: row.id,
    plotId: row.plot_id,
    realmId: row.realm_id,
    userId: row.user_id,
    permission: row.permission,
    createdAt: row.created_at,
    updatedAt: row.updated_at,
  };
}

export async function getPlotOwner(
  plotId: string,
  executor: DbExecutor = db
): Promise<PlotOwnerRecord | undefined> {
  const rows = await executor.query<any[]>(
    `SELECT plot_id, realm_id, owner_user_id, created_at, updated_at
     FROM plot_ownerships
     WHERE plot_id = ?`,
    [plotId]
  );
  const row = rows[0];
  return row ? mapOwnerRow(row) : undefined;
}

export async function upsertPlotOwner(
  plotId: string,
  realmId: string,
  ownerUserId: string | null,
  executor: DbExecutor = db
): Promise<PlotOwnerRecord> {
  const now = new Date().toISOString();
  await executor.execute(
    `INSERT INTO plot_ownerships (plot_id, realm_id, owner_user_id, created_at, updated_at)
     VALUES (?, ?, ?, ?, ?)
     ON DUPLICATE KEY UPDATE
       owner_user_id = VALUES(owner_user_id),
       updated_at = VALUES(updated_at)`,
    [plotId, realmId, ownerUserId, now, now]
  );
  return {
    plotId,
    realmId,
    ownerUserId,
    createdAt: now,
    updatedAt: now,
  };
}

export async function clearPlotOwner(
  plotId: string,
  executor: DbExecutor = db
): Promise<void> {
  await executor.execute('DELETE FROM plot_ownerships WHERE plot_id = ?', [plotId]);
}

export async function listPlotPermissions(
  plotId: string,
  executor: DbExecutor = db
): Promise<PlotPermissionRecord[]> {
  const rows = await executor.query<any[]>(
    `SELECT id, plot_id, realm_id, user_id, permission, created_at, updated_at
     FROM plot_permissions
     WHERE plot_id = ?
     ORDER BY created_at ASC`,
    [plotId]
  );
  return rows.map(mapPermissionRow);
}

export async function getPlotPermissionForUser(
  plotId: string,
  userId: string,
  executor: DbExecutor = db
): Promise<PlotPermissionRecord | undefined> {
  const rows = await executor.query<any[]>(
    `SELECT id, plot_id, realm_id, user_id, permission, created_at, updated_at
     FROM plot_permissions
     WHERE plot_id = ? AND user_id = ?`,
    [plotId, userId]
  );
  const row = rows[0];
  return row ? mapPermissionRow(row) : undefined;
}

export async function replacePlotPermissions(
  plotId: string,
  realmId: string,
  permissions: Array<{ userId: string; permission: string }>,
  executor: DbExecutor = db
): Promise<PlotPermissionRecord[]> {
  const now = new Date().toISOString();
  await executor.execute('DELETE FROM plot_permissions WHERE plot_id = ?', [plotId]);

  const records: PlotPermissionRecord[] = [];
  for (const entry of permissions) {
    const record: PlotPermissionRecord = {
      id: randomUUID(),
      plotId,
      realmId,
      userId: entry.userId,
      permission: entry.permission,
      createdAt: now,
      updatedAt: now,
    };
    await executor.execute(
      `INSERT INTO plot_permissions (id, plot_id, realm_id, user_id, permission, created_at, updated_at)
       VALUES (?, ?, ?, ?, ?, ?, ?)`,
      [
        record.id,
        record.plotId,
        record.realmId,
        record.userId,
        record.permission,
        record.createdAt,
        record.updatedAt,
      ]
    );
    records.push(record);
  }

  return records;
}
