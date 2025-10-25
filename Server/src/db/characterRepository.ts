import { randomUUID } from 'crypto';
import { db } from './database';

export interface Character {
  id: string;
  userId: string;
  realmId: string;
  name: string;
  bio?: string | null;
  createdAt: string;
}

export interface NewCharacter {
  userId: string;
  realmId: string;
  name: string;
  bio?: string;
}

export function createCharacter(input: NewCharacter): Character {
  const character: Character = {
    id: randomUUID(),
    userId: input.userId,
    realmId: input.realmId,
    name: input.name,
    bio: input.bio ?? null,
    createdAt: new Date().toISOString(),
  };

  const stmt = db.prepare(
    `INSERT INTO characters (id, user_id, realm_id, name, bio, created_at)
     VALUES (@id, @userId, @realmId, @name, @bio, @createdAt)`
  );
  stmt.run(character);
  return character;
}

export function findCharacterByNameForUser(
  userId: string,
  realmId: string,
  name: string
): Character | undefined {
  const stmt = db.prepare(
    `SELECT id, user_id as userId, realm_id as realmId, name, bio, created_at as createdAt
     FROM characters
     WHERE user_id = ? AND realm_id = ? AND name = ?`
  );
  return stmt.get(userId, realmId, name) as Character | undefined;
}

export function listCharactersForRealm(realmId: string): Character[] {
  const stmt = db.prepare(
    `SELECT id, user_id as userId, realm_id as realmId, name, bio, created_at as createdAt
     FROM characters
     WHERE realm_id = ?
     ORDER BY created_at ASC`
  );
  return stmt.all(realmId) as Character[];
}

export function listCharactersForUserInRealm(
  userId: string,
  realmId: string
): Character[] {
  const stmt = db.prepare(
    `SELECT id, user_id as userId, realm_id as realmId, name, bio, created_at as createdAt
     FROM characters
     WHERE realm_id = ? AND user_id = ?
     ORDER BY created_at ASC`
  );
  return stmt.all(realmId, userId) as Character[];
}
