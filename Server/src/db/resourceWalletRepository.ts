import { randomUUID } from 'crypto';
import { db } from './database';
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

export function listWalletEntries(realmId: string, userId: string): ResourceWalletRecord[] {
  const stmt = db.prepare(
    `SELECT id, realm_id, user_id, resource_type, quantity, updated_at
     FROM realm_resource_wallets
     WHERE realm_id = ? AND user_id = ?`
  );
  const rows = stmt.all(realmId, userId) as any[];
  return rows.map(mapRow);
}

export function applyResourceAdjustments(
  realmId: string,
  userId: string,
  adjustments: ResourceAdjustment[]
): ResourceWalletRecord[] {
  const filtered = adjustments.filter((adjustment) => adjustment.delta !== 0);
  if (filtered.length === 0) {
    return listWalletEntries(realmId, userId);
  }

  const now = new Date().toISOString();
  const updated: ResourceWalletRecord[] = [];

  for (const adjustment of filtered) {
    const resourceType = adjustment.resourceType.trim();
    if (!resourceType) {
      throw new Error('resourceType is required for resource adjustments');
    }

    const selectStmt = db.prepare(
      `SELECT id, realm_id, user_id, resource_type, quantity, updated_at
       FROM realm_resource_wallets
       WHERE realm_id = ? AND user_id = ? AND resource_type = ?`
    );
    const existingRow = selectStmt.get(realmId, userId, resourceType) as
      | ResourceWalletRecord
      | undefined;

    const currentQuantity = existingRow?.quantity ?? 0;
    const nextQuantity = currentQuantity + adjustment.delta;

    if (nextQuantity < 0) {
      throw new InsufficientResourceError(resourceType, Math.abs(adjustment.delta), currentQuantity);
    }

    if (existingRow) {
      const updateStmt = db.prepare(
        `UPDATE realm_resource_wallets
         SET quantity = @quantity,
             updated_at = @updatedAt
         WHERE id = @id`
      );
      updateStmt.run({ id: existingRow.id, quantity: nextQuantity, updatedAt: now });
      updated.push({ ...existingRow, quantity: nextQuantity, updatedAt: now });
    } else {
      const insertStmt = db.prepare(
        `INSERT INTO realm_resource_wallets
           (id, realm_id, user_id, resource_type, quantity, updated_at)
         VALUES
           (@id, @realmId, @userId, @resourceType, @quantity, @updatedAt)`
      );
      const record: ResourceWalletRecord = {
        id: randomUUID(),
        realmId,
        userId,
        resourceType,
        quantity: nextQuantity,
        updatedAt: now,
      };
      insertStmt.run(record);
      updated.push(record);
    }
  }

  return updated;
}

export function applyResourceDeltas(
  realmId: string,
  userId: string,
  deltas: ResourceDelta[]
): ResourceWalletRecord[] {
  const adjustments: ResourceAdjustment[] = deltas
    .filter((delta) => Number.isFinite(delta.quantity) && delta.quantity !== 0)
    .map((delta) => ({
      resourceType: delta.resourceType,
      delta: delta.quantity > 0 ? -Math.abs(delta.quantity) : Math.abs(delta.quantity),
    }));

  return applyResourceAdjustments(realmId, userId, adjustments);
}
