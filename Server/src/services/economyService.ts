import { db } from '../db/database';
import {
  applyCurrencyAdjustments,
  CurrencyAdjustment,
  listBalancesForCharacter,
  listCurrencies,
} from '../db/currencyRepository';
import {
  createTrade,
  findTradeById,
  listTradeItems,
  listTradesForCharacter,
  Trade,
  TradeItem,
  updateTradeStatus,
  upsertTradeItem,
} from '../db/tradeRepository';
import { requireCharacter, requireOwnedCharacter } from './characterAccessService';
import { HttpError } from '../utils/errors';
import { TradeStatus } from '../config/gameEnums';
import {
  getCharacterProgressionSnapshot,
  getInventorySnapshotForUpdate,
  replaceInventory,
  InventoryItemInput,
} from '../db/progressionRepository';

export async function listAvailableCurrencies() {
  return listCurrencies();
}

export async function listCharacterBalances(userId: string, characterId: string) {
  await requireOwnedCharacter(userId, characterId);
  return listBalancesForCharacter(characterId);
}

export async function adjustCharacterCurrencies(
  userId: string,
  characterId: string,
  adjustments: CurrencyAdjustment[]
) {
  await requireOwnedCharacter(userId, characterId);
  return db.withTransaction(async (tx) => applyCurrencyAdjustments(characterId, adjustments, tx));
}

export async function createTradeRequest(
  userId: string,
  initiatorCharacterId: string,
  targetCharacterId: string
): Promise<Trade> {
  const initiator = await requireOwnedCharacter(userId, initiatorCharacterId);
  const target = await requireCharacter(targetCharacterId);

  if (initiator.realmId !== target.realmId) {
    throw new HttpError(400, 'Characters must be in the same realm to trade');
  }

  return createTrade(initiator.realmId, initiatorCharacterId, targetCharacterId);
}

export async function listTrades(userId: string, characterId: string): Promise<Trade[]> {
  await requireOwnedCharacter(userId, characterId);
  return listTradesForCharacter(characterId);
}

export async function addTradeItemForCharacter(
  userId: string,
  tradeId: string,
  characterId: string,
  itemId: string,
  quantity: number,
  metadataJson?: string
): Promise<TradeItem> {
  const trade = await findTradeOrThrow(tradeId);
  const character = await requireOwnedCharacter(userId, characterId);

  if (trade.status !== 'pending') {
    throw new HttpError(409, 'Trade is not open for updates');
  }

  if (![trade.initiatorCharacterId, trade.targetCharacterId].includes(characterId)) {
    throw new HttpError(403, 'Character is not part of this trade');
  }

  if (character.realmId !== trade.realmId) {
    throw new HttpError(400, 'Character is not in the same realm as the trade');
  }

  if (!itemId.trim()) {
    throw new HttpError(400, 'itemId is required');
  }

  if (!Number.isFinite(quantity) || quantity <= 0) {
    throw new HttpError(400, 'quantity must be a positive number');
  }

  const snapshot = await getCharacterProgressionSnapshot(characterId);
  const inventoryItem = snapshot.inventory.items.find((item) => item.itemId === itemId);
  if (!inventoryItem || inventoryItem.quantity < quantity) {
    throw new HttpError(409, 'Insufficient inventory for trade item');
  }

  return upsertTradeItem(tradeId, characterId, itemId, quantity, metadataJson ?? '{}');
}

export async function acceptTrade(
  userId: string,
  tradeId: string,
  characterId: string
): Promise<Trade> {
  const trade = await findTradeOrThrow(tradeId);
  await requireOwnedCharacter(userId, characterId);

  if (trade.status !== 'pending' && trade.status !== 'accepted') {
    throw new HttpError(409, 'Trade is not active');
  }

  if (![trade.initiatorCharacterId, trade.targetCharacterId].includes(characterId)) {
    throw new HttpError(403, 'Character is not part of this trade');
  }

  const initiatorAccepted = trade.initiatorAccepted || characterId === trade.initiatorCharacterId;
  const targetAccepted = trade.targetAccepted || characterId === trade.targetCharacterId;
  const nextStatus: TradeStatus =
    initiatorAccepted && targetAccepted ? 'accepted' : trade.status;

  await updateTradeStatus(tradeId, nextStatus, initiatorAccepted, targetAccepted);

  const updated: Trade = {
    ...trade,
    initiatorAccepted,
    targetAccepted,
    status: nextStatus,
  };

  if (initiatorAccepted && targetAccepted) {
    await completeTrade(updated);
    const completed = await findTradeById(tradeId);
    if (completed) {
      return completed;
    }
  }

  return updated;
}

export async function cancelTrade(
  userId: string,
  tradeId: string,
  characterId: string
): Promise<Trade> {
  const trade = await findTradeOrThrow(tradeId);
  await requireOwnedCharacter(userId, characterId);

  if (![trade.initiatorCharacterId, trade.targetCharacterId].includes(characterId)) {
    throw new HttpError(403, 'Character is not part of this trade');
  }

  if (trade.status === 'completed') {
    throw new HttpError(409, 'Completed trade cannot be cancelled');
  }

  await updateTradeStatus(tradeId, 'cancelled', trade.initiatorAccepted, trade.targetAccepted);
  const cancelled = await findTradeById(tradeId);
  if (!cancelled) {
    throw new HttpError(500, 'Trade cancellation failed');
  }
  return cancelled;
}

async function findTradeOrThrow(tradeId: string): Promise<Trade> {
  const trade = await findTradeById(tradeId);
  if (!trade) {
    throw new HttpError(404, 'Trade not found');
  }
  return trade;
}

async function completeTrade(trade: Trade): Promise<void> {
  await db.withTransaction(async (tx) => {
    const items = await listTradeItems(trade.id, tx);
    const initiatorSnapshot = await getInventorySnapshotForUpdate(
      trade.initiatorCharacterId,
      tx
    );
    const targetSnapshot = await getInventorySnapshotForUpdate(
      trade.targetCharacterId,
      tx
    );

    const initiatorInventory = buildInventoryAfterTrade(
      initiatorSnapshot.items.map((item) => ({ itemId: item.itemId, quantity: item.quantity })),
      items,
      trade.initiatorCharacterId,
      trade.targetCharacterId
    );
    const targetInventory = buildInventoryAfterTrade(
      targetSnapshot.items.map((item) => ({ itemId: item.itemId, quantity: item.quantity })),
      items,
      trade.targetCharacterId,
      trade.initiatorCharacterId
    );

    await replaceInventory(
      trade.initiatorCharacterId,
      initiatorInventory,
      initiatorSnapshot.version,
      tx
    );
    await replaceInventory(
      trade.targetCharacterId,
      targetInventory,
      targetSnapshot.version,
      tx
    );

    await updateTradeStatus(trade.id, 'completed', true, true, tx);
  });
}

function buildInventoryAfterTrade(
  current: { itemId: string; quantity: number }[],
  tradeItems: TradeItem[],
  fromCharacterId: string,
  otherCharacterId: string
): InventoryItemInput[] {
  const inventoryMap = new Map<string, number>();
  for (const item of current) {
    inventoryMap.set(item.itemId, item.quantity);
  }

  const outgoing = tradeItems.filter((item) => item.characterId === fromCharacterId);
  for (const item of outgoing) {
    const currentQty = inventoryMap.get(item.itemId) ?? 0;
    const nextQty = currentQty - item.quantity;
    if (nextQty < 0) {
      throw new HttpError(409, 'Trade inventory mismatch');
    }
    if (nextQty === 0) {
      inventoryMap.delete(item.itemId);
    } else {
      inventoryMap.set(item.itemId, nextQty);
    }
  }

  const incoming = tradeItems.filter((item) => item.characterId === otherCharacterId);
  for (const item of incoming) {
    const currentQty = inventoryMap.get(item.itemId) ?? 0;
    inventoryMap.set(item.itemId, currentQty + item.quantity);
  }

  return Array.from(inventoryMap.entries()).map(([itemId, quantity]) => ({
    itemId,
    quantity,
  }));
}
