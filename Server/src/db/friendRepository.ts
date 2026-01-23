import { randomUUID } from 'crypto';
import { db, DbExecutor } from './database';
import { FriendStatus } from '../config/gameEnums';

export interface FriendRecord {
  id: string;
  characterId: string;
  friendCharacterId: string;
  status: FriendStatus;
  createdAt: string;
  updatedAt: string;
}

function mapFriendRow(row: any): FriendRecord {
  return {
    id: row.id,
    characterId: row.character_id,
    friendCharacterId: row.friend_character_id,
    status: row.status,
    createdAt: row.created_at,
    updatedAt: row.updated_at,
  };
}

export async function listFriendsForCharacter(
  characterId: string,
  executor: DbExecutor = db
): Promise<FriendRecord[]> {
  const rows = await executor.query(
    `SELECT id, character_id, friend_character_id, status, created_at, updated_at
     FROM friends
     WHERE character_id = ?
     ORDER BY updated_at DESC`,
    [characterId]
  );
  return rows.map(mapFriendRow);
}

export async function upsertFriendRelationship(
  characterId: string,
  friendCharacterId: string,
  status: FriendStatus,
  executor: DbExecutor = db
): Promise<FriendRecord> {
  const now = new Date().toISOString();
  const existingRows = await executor.query(
    `SELECT id, character_id, friend_character_id, status, created_at, updated_at
     FROM friends
     WHERE character_id = ? AND friend_character_id = ?`,
    [characterId, friendCharacterId]
  );
  const existing = existingRows[0];

  if (existing) {
    await executor.execute(
      `UPDATE friends
       SET status = ?, updated_at = ?
       WHERE id = ?`,
      [status, now, existing.id]
    );
    return {
      id: existing.id,
      characterId,
      friendCharacterId,
      status,
      createdAt: existing.created_at,
      updatedAt: now,
    };
  }

  const record: FriendRecord = {
    id: randomUUID(),
    characterId,
    friendCharacterId,
    status,
    createdAt: now,
    updatedAt: now,
  };

  await executor.execute(
    `INSERT INTO friends (id, character_id, friend_character_id, status, created_at, updated_at)
     VALUES (?, ?, ?, ?, ?, ?)`,
    [
      record.id,
      record.characterId,
      record.friendCharacterId,
      record.status,
      record.createdAt,
      record.updatedAt,
    ]
  );

  return record;
}
