import { randomUUID } from 'crypto';
import { db, DbExecutor } from './database';
import { ResourceDelta } from '../types/resources';

export interface ResourceWalletRecord {
  id: string;
  realmId: string;
  userId: string;
  resourceType: string;
  quantity: number;
  updatedAt: string;
}

export interface ResourceAdjustment {
  resourceType: string;
  delta: number;
}

export class InsufficientResourceError extends Error {
  constructor(
    public readonly resourceType: string,
    public readonly requested: number,
    public readonly available: number
  ) {
    super(
      `Insufficient ${resourceType}: requested ${requested.toLocaleString()} but only ${available.toLocaleString()} available`
    );
    this.name = 'InsufficientResourceError';
  }
}

function mapRow(row: any): ResourceWalletRecord {
  return {
    id: row.id,
    realmId: row.realm_id,
    userId: row.user_id,
    resourceType: row.resource_type,
    quantity: row.quantity,
    updatedAt: row.updated_at,
  };
}

export async function listWalletEntries(
  realmId: string,
  userId: string,
  executor: DbExecutor = db
): Promise<ResourceWalletRecord[]> {
  const rows = await executor.query(
    `SELECT id, realm_id, user_id, resource_type, quantity, updated_at
     FROM realm_resource_wallets
     WHERE realm_id = ? AND user_id = ?`,
    [realmId, userId]
  );
  return rows.map(mapRow);
}

export async function applyResourceAdjustments(
  realmId: string,
  userId: string,
  adjustments: ResourceAdjustment[],
  executor: DbExecutor = db
): Promise<ResourceWalletRecord[]> {
  const filtered = adjustments.filter((adjustment) => adjustment.delta !== 0);
  if (filtered.length === 0) {
    return listWalletEntries(realmId, userId, executor);
  }

  const now = new Date().toISOString();
  const updated: ResourceWalletRecord[] = [];

  for (const adjustment of filtered) {
    const resourceType = adjustment.resourceType.trim();
    if (!resourceType) {
      throw new Error('resourceType is required for resource adjustments');
    }

    const rows = await executor.query<ResourceWalletRecord[]>(
      `SELECT id, realm_id, user_id, resource_type, quantity, updated_at
       FROM realm_resource_wallets
       WHERE realm_id = ? AND user_id = ? AND resource_type = ?`,
      [realmId, userId, resourceType]
    );
    const existingRow = rows[0];

    const currentQuantity = existingRow?.quantity ?? 0;
    const nextQuantity = currentQuantity + adjustment.delta;

    if (nextQuantity < 0) {
      throw new InsufficientResourceError(resourceType, Math.abs(adjustment.delta), currentQuantity);
    }

    if (existingRow) {
      await executor.execute(
        `UPDATE realm_resource_wallets
         SET quantity = ?,
             updated_at = ?
         WHERE id = ?`,
        [nextQuantity, now, existingRow.id]
      );
      updated.push({ ...existingRow, quantity: nextQuantity, updatedAt: now });
    } else {
      const record: ResourceWalletRecord = {
        id: randomUUID(),
        realmId,
        userId,
        resourceType,
        quantity: nextQuantity,
        updatedAt: now,
      };
      await executor.execute(
        `INSERT INTO realm_resource_wallets
           (id, realm_id, user_id, resource_type, quantity, updated_at)
         VALUES
           (?, ?, ?, ?, ?, ?)`,
        [
          record.id,
          record.realmId,
          record.userId,
          record.resourceType,
          record.quantity,
          record.updatedAt,
        ]
      );
      updated.push(record);
    }
  }

  return updated;
}

export async function applyResourceDeltas(
  realmId: string,
  userId: string,
  deltas: ResourceDelta[],
  executor: DbExecutor = db
): Promise<ResourceWalletRecord[]> {
  const adjustments: ResourceAdjustment[] = deltas
    .filter((delta) => Number.isFinite(delta.quantity) && delta.quantity !== 0)
    .map((delta) => ({
      resourceType: delta.resourceType,
      delta: delta.quantity > 0 ? -Math.abs(delta.quantity) : Math.abs(delta.quantity),
    }));

  return applyResourceAdjustments(realmId, userId, adjustments, executor);
}
