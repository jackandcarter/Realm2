import { randomUUID } from 'crypto';
import { db, DbExecutor } from './database';

export interface Currency {
  id: string;
  name: string;
  description?: string | null;
  isPremium: boolean;
  iconUrl?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CurrencyBalance {
  id: string;
  characterId: string;
  currencyId: string;
  balance: number;
  updatedAt: string;
}

export interface CurrencyAdjustment {
  currencyId: string;
  delta: number;
}

function mapCurrencyRow(row: any): Currency {
  return {
    id: row.id,
    name: row.name,
    description: row.description,
    isPremium: Boolean(row.is_premium),
    iconUrl: row.icon_url,
    createdAt: row.created_at,
    updatedAt: row.updated_at,
  };
}

function mapBalanceRow(row: any): CurrencyBalance {
  return {
    id: row.id,
    characterId: row.character_id,
    currencyId: row.currency_id,
    balance: row.balance,
    updatedAt: row.updated_at,
  };
}

export async function listCurrencies(executor: DbExecutor = db): Promise<Currency[]> {
  const rows = await executor.query(
    `SELECT id, name, description, is_premium, icon_url, created_at, updated_at
     FROM currencies
     ORDER BY name ASC`
  );
  return rows.map(mapCurrencyRow);
}

export async function upsertCurrency(
  input: Pick<Currency, 'id' | 'name' | 'description' | 'isPremium' | 'iconUrl'>,
  executor: DbExecutor = db
): Promise<Currency> {
  const now = new Date().toISOString();
  const record: Currency = {
    id: input.id,
    name: input.name,
    description: input.description ?? null,
    isPremium: input.isPremium,
    iconUrl: input.iconUrl ?? null,
    createdAt: now,
    updatedAt: now,
  };

  await executor.execute(
    `INSERT INTO currencies (id, name, description, is_premium, icon_url, created_at, updated_at)
     VALUES (?, ?, ?, ?, ?, ?, ?)
     ON DUPLICATE KEY UPDATE
       name = VALUES(name),
       description = VALUES(description),
       is_premium = VALUES(is_premium),
       icon_url = VALUES(icon_url),
       updated_at = VALUES(updated_at)`,
    [
      record.id,
      record.name,
      record.description,
      record.isPremium ? 1 : 0,
      record.iconUrl,
      record.createdAt,
      record.updatedAt,
    ]
  );

  return record;
}

export async function listBalancesForCharacter(
  characterId: string,
  executor: DbExecutor = db
): Promise<CurrencyBalance[]> {
  const rows = await executor.query(
    `SELECT id, character_id, currency_id, balance, updated_at
     FROM character_currencies
     WHERE character_id = ?
     ORDER BY currency_id ASC`,
    [characterId]
  );
  return rows.map(mapBalanceRow);
}

export async function applyCurrencyAdjustments(
  characterId: string,
  adjustments: CurrencyAdjustment[],
  executor: DbExecutor = db
): Promise<CurrencyBalance[]> {
  const normalized = adjustments
    .map((entry) => ({
      currencyId: entry.currencyId.trim(),
      delta: Number(entry.delta),
    }))
    .filter((entry) => entry.currencyId && Number.isFinite(entry.delta) && entry.delta !== 0);

  if (normalized.length === 0) {
    return listBalancesForCharacter(characterId, executor);
  }

  const now = new Date().toISOString();
  const updated: CurrencyBalance[] = [];

  for (const adjustment of normalized) {
    const rows = await executor.query(
      `SELECT id, character_id, currency_id, balance, updated_at
       FROM character_currencies
       WHERE character_id = ? AND currency_id = ?`,
      [characterId, adjustment.currencyId]
    );
    const existing = rows[0];
    const currentBalance = existing?.balance ?? 0;
    const nextBalance = currentBalance + adjustment.delta;
    if (nextBalance < 0) {
      throw new Error(`Insufficient currency ${adjustment.currencyId}`);
    }

    if (existing) {
      await executor.execute(
        `UPDATE character_currencies
         SET balance = ?, updated_at = ?
         WHERE id = ?`,
        [nextBalance, now, existing.id]
      );
      updated.push({
        id: existing.id,
        characterId,
        currencyId: adjustment.currencyId,
        balance: nextBalance,
        updatedAt: now,
      });
    } else {
      const record: CurrencyBalance = {
        id: randomUUID(),
        characterId,
        currencyId: adjustment.currencyId,
        balance: nextBalance,
        updatedAt: now,
      };
      await executor.execute(
        `INSERT INTO character_currencies (id, character_id, currency_id, balance, updated_at)
         VALUES (?, ?, ?, ?, ?)`,
        [record.id, record.characterId, record.currencyId, record.balance, record.updatedAt]
      );
      updated.push(record);
    }
  }

  return updated;
}

export async function ensureCurrencyExists(
  currencyId: string,
  executor: DbExecutor = db
): Promise<void> {
  const rows = await executor.query<{ id: string }[]>(
    `SELECT id FROM currencies WHERE id = ?`,
    [currencyId]
  );
  if (!rows[0]?.id) {
    throw new Error(`Currency ${currencyId} not found`);
  }
}
