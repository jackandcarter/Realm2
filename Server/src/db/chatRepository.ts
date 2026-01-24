import { randomUUID } from 'crypto';
import { ChatChannelType } from '../config/gameEnums';
import { db, DbExecutor } from './database';

export interface ChatChannel {
  id: string;
  realmId: string;
  name: string;
  type: ChatChannelType;
  createdAt: string;
}

export interface ChatMessage {
  id: string;
  channelId: string;
  characterId: string;
  message: string;
  sentAt: string;
}

function mapChannelRow(row: any): ChatChannel {
  return {
    id: row.id,
    realmId: row.realm_id,
    name: row.name,
    type: row.type,
    createdAt: row.created_at,
  };
}

function mapMessageRow(row: any): ChatMessage {
  return {
    id: row.id,
    channelId: row.channel_id,
    characterId: row.character_id,
    message: row.message,
    sentAt: row.sent_at,
  };
}

export async function findChatChannelById(
  channelId: string,
  executor: DbExecutor = db
): Promise<ChatChannel | undefined> {
  const rows = await executor.query(
    `SELECT id, realm_id, name, type, created_at
     FROM chat_channels
     WHERE id = ?`,
    [channelId]
  );
  const row = rows[0];
  return row ? mapChannelRow(row) : undefined;
}

export async function createChatChannel(
  realmId: string,
  name: string,
  type: ChatChannelType,
  executor: DbExecutor = db
): Promise<ChatChannel> {
  const now = new Date().toISOString();
  const channel: ChatChannel = {
    id: randomUUID(),
    realmId,
    name,
    type,
    createdAt: now,
  };

  await executor.execute(
    `INSERT INTO chat_channels (id, realm_id, name, type, created_at)
     VALUES (?, ?, ?, ?, ?)
     ON DUPLICATE KEY UPDATE name = VALUES(name)`,
    [channel.id, channel.realmId, channel.name, channel.type, channel.createdAt]
  );

  const rows = await executor.query(
    `SELECT id, realm_id, name, type, created_at
     FROM chat_channels
     WHERE realm_id = ? AND name = ? AND type = ?
     LIMIT 1`,
    [realmId, name, type]
  );
  const row = rows[0];
  return row ? mapChannelRow(row) : channel;
}

export async function listChatChannels(
  realmId: string,
  executor: DbExecutor = db
): Promise<ChatChannel[]> {
  const rows = await executor.query(
    `SELECT id, realm_id, name, type, created_at
     FROM chat_channels
     WHERE realm_id = ?
     ORDER BY name ASC`,
    [realmId]
  );
  return rows.map(mapChannelRow);
}

export async function addChatMessage(
  channelId: string,
  characterId: string,
  message: string,
  executor: DbExecutor = db
): Promise<ChatMessage> {
  const now = new Date().toISOString();
  const record: ChatMessage = {
    id: randomUUID(),
    channelId,
    characterId,
    message,
    sentAt: now,
  };

  await executor.execute(
    `INSERT INTO chat_messages (id, channel_id, character_id, message, sent_at)
     VALUES (?, ?, ?, ?, ?)`,
    [record.id, record.channelId, record.characterId, record.message, record.sentAt]
  );

  return record;
}

export async function listChatMessages(
  channelId: string,
  limit = 50,
  executor: DbExecutor = db
): Promise<ChatMessage[]> {
  const safeLimit = Math.min(Math.max(limit, 1), 200);
  const rows = await executor.query(
    `SELECT id, channel_id, character_id, message, sent_at
     FROM chat_messages
     WHERE channel_id = ?
     ORDER BY sent_at DESC
     LIMIT ?`,
    [channelId, safeLimit]
  );
  return rows.map(mapMessageRow);
}
