import { randomUUID } from 'crypto';
import { authDb } from './authDatabase';

export interface User {
  id: string;
  email: string;
  username: string;
  passwordHash: string;
  createdAt: string;
  lastRealmId?: string | null;
  lastCharacterId?: string | null;
  lastRealmSelectedAt?: string | null;
  lastCharacterSelectedAt?: string | null;
}

export async function createUser(
  email: string,
  username: string,
  passwordHash: string
): Promise<User> {
  const user: User = {
    id: randomUUID(),
    email: email.toLowerCase(),
    username,
    passwordHash,
    createdAt: new Date().toISOString(),
  };

  await authDb.execute(
    'INSERT INTO users (id, email, username, password_hash, created_at) VALUES (?, ?, ?, ?, ?)',
    [user.id, user.email, user.username, user.passwordHash, user.createdAt]
  );
  return user;
}

export async function findUserByEmail(email: string): Promise<User | undefined> {
  const rows = await authDb.query<User[]>(
    `SELECT id, email, username, password_hash as passwordHash, created_at as createdAt,
      last_realm_id as lastRealmId, last_character_id as lastCharacterId,
      last_realm_selected_at as lastRealmSelectedAt, last_character_selected_at as lastCharacterSelectedAt
     FROM users WHERE email = ?`,
    [email.toLowerCase()]
  );
  return rows[0];
}

export async function findUserById(id: string): Promise<User | undefined> {
  const rows = await authDb.query<User[]>(
    `SELECT id, email, username, password_hash as passwordHash, created_at as createdAt,
      last_realm_id as lastRealmId, last_character_id as lastCharacterId,
      last_realm_selected_at as lastRealmSelectedAt, last_character_selected_at as lastCharacterSelectedAt
     FROM users WHERE id = ?`,
    [id]
  );
  return rows[0];
}

export async function findUserByUsername(username: string): Promise<User | undefined> {
  const rows = await authDb.query<User[]>(
    `SELECT id, email, username, password_hash as passwordHash, created_at as createdAt,
      last_realm_id as lastRealmId, last_character_id as lastCharacterId,
      last_realm_selected_at as lastRealmSelectedAt, last_character_selected_at as lastCharacterSelectedAt
     FROM users WHERE username = ?`,
    [username]
  );
  return rows[0];
}

export async function updateUserRealmSelection(userId: string, realmId: string): Promise<void> {
  const now = new Date().toISOString();
  await authDb.execute(
    `UPDATE users
     SET last_realm_id = ?, last_realm_selected_at = ?
     WHERE id = ?`,
    [realmId, now, userId]
  );
}

export async function updateUserCharacterSelection(
  userId: string,
  characterId: string,
  realmId: string
): Promise<void> {
  const now = new Date().toISOString();
  await authDb.execute(
    `UPDATE users
     SET last_character_id = ?, last_character_selected_at = ?, last_realm_id = ?, last_realm_selected_at = ?
     WHERE id = ?`,
    [characterId, now, realmId, now, userId]
  );
}
