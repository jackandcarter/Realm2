import { randomUUID } from 'crypto';
import { db, DbExecutor } from './database';

export type TradeStatus = 'pending' | 'accepted' | 'cancelled' | 'completed';

export interface Trade {
  id: string;
  realmId: string;
  initiatorCharacterId: string;
  targetCharacterId: string;
  initiatorAccepted: boolean;
  targetAccepted: boolean;
  status: TradeStatus;
  createdAt: string;
  updatedAt: string;
}

export interface TradeItem {
  id: string;
  tradeId: string;
  characterId: string;
  itemId: string;
  quantity: number;
  metadataJson: string;
}

function mapTradeRow(row: any): Trade {
  return {
    id: row.id,
    realmId: row.realm_id,
    initiatorCharacterId: row.initiator_character_id,
    targetCharacterId: row.target_character_id,
    initiatorAccepted: Boolean(row.initiator_accepted),
    targetAccepted: Boolean(row.target_accepted),
    status: row.status,
    createdAt: row.created_at,
    updatedAt: row.updated_at,
  };
}

function mapTradeItemRow(row: any): TradeItem {
  return {
    id: row.id,
    tradeId: row.trade_id,
    characterId: row.character_id,
    itemId: row.item_id,
    quantity: row.quantity,
    metadataJson: row.metadata_json ?? '{}',
  };
}

export async function createTrade(
  realmId: string,
  initiatorCharacterId: string,
  targetCharacterId: string,
  executor: DbExecutor = db
): Promise<Trade> {
  const now = new Date().toISOString();
  const trade: Trade = {
    id: randomUUID(),
    realmId,
    initiatorCharacterId,
    targetCharacterId,
    initiatorAccepted: false,
    targetAccepted: false,
    status: 'pending',
    createdAt: now,
    updatedAt: now,
  };

  await executor.execute(
    `INSERT INTO trades
      (id, realm_id, initiator_character_id, target_character_id, initiator_accepted, target_accepted, status, created_at, updated_at)
     VALUES
      (?, ?, ?, ?, ?, ?, ?, ?, ?)`,
    [
      trade.id,
      trade.realmId,
      trade.initiatorCharacterId,
      trade.targetCharacterId,
      trade.initiatorAccepted ? 1 : 0,
      trade.targetAccepted ? 1 : 0,
      trade.status,
      trade.createdAt,
      trade.updatedAt,
    ]
  );

  return trade;
}

export async function findTradeById(
  tradeId: string,
  executor: DbExecutor = db
): Promise<Trade | undefined> {
  const rows = await executor.query(
    `SELECT
       id,
       realm_id,
       initiator_character_id,
       target_character_id,
       initiator_accepted,
       target_accepted,
       status,
       created_at,
       updated_at
     FROM trades
     WHERE id = ?`,
    [tradeId]
  );
  const row = rows[0];
  return row ? mapTradeRow(row) : undefined;
}

export async function listTradesForCharacter(
  characterId: string,
  executor: DbExecutor = db
): Promise<Trade[]> {
  const rows = await executor.query(
    `SELECT
       id,
       realm_id,
       initiator_character_id,
       target_character_id,
       initiator_accepted,
       target_accepted,
       status,
       created_at,
       updated_at
     FROM trades
     WHERE initiator_character_id = ? OR target_character_id = ?
     ORDER BY created_at DESC`,
    [characterId, characterId]
  );
  return rows.map(mapTradeRow);
}

export async function listTradeItems(
  tradeId: string,
  executor: DbExecutor = db
): Promise<TradeItem[]> {
  const rows = await executor.query(
    `SELECT id, trade_id, character_id, item_id, quantity, metadata_json
     FROM trade_items
     WHERE trade_id = ?`,
    [tradeId]
  );
  return rows.map(mapTradeItemRow);
}

export async function upsertTradeItem(
  tradeId: string,
  characterId: string,
  itemId: string,
  quantity: number,
  metadataJson = '{}',
  executor: DbExecutor = db
): Promise<TradeItem> {
  const nowQuantity = Math.max(1, Math.floor(quantity));
  const existingRows = await executor.query(
    `SELECT id, trade_id, character_id, item_id, quantity, metadata_json
     FROM trade_items
     WHERE trade_id = ? AND character_id = ? AND item_id = ?`,
    [tradeId, characterId, itemId]
  );
  const existing = existingRows[0];

  if (existing) {
    await executor.execute(
      `UPDATE trade_items
       SET quantity = ?, metadata_json = ?
       WHERE id = ?`,
      [nowQuantity, metadataJson, existing.id]
    );
    return {
      id: existing.id,
      tradeId,
      characterId,
      itemId,
      quantity: nowQuantity,
      metadataJson,
    };
  }

  const tradeItem: TradeItem = {
    id: randomUUID(),
    tradeId,
    characterId,
    itemId,
    quantity: nowQuantity,
    metadataJson,
  };

  await executor.execute(
    `INSERT INTO trade_items (id, trade_id, character_id, item_id, quantity, metadata_json)
     VALUES (?, ?, ?, ?, ?, ?)`,
    [
      tradeItem.id,
      tradeItem.tradeId,
      tradeItem.characterId,
      tradeItem.itemId,
      tradeItem.quantity,
      tradeItem.metadataJson,
    ]
  );

  return tradeItem;
}

export async function updateTradeStatus(
  tradeId: string,
  status: TradeStatus,
  initiatorAccepted: boolean,
  targetAccepted: boolean,
  executor: DbExecutor = db
): Promise<void> {
  const now = new Date().toISOString();
  await executor.execute(
    `UPDATE trades
     SET status = ?, initiator_accepted = ?, target_accepted = ?, updated_at = ?
     WHERE id = ?`,
    [status, initiatorAccepted ? 1 : 0, targetAccepted ? 1 : 0, now, tradeId]
  );
}
