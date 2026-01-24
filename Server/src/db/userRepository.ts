import { randomUUID } from 'crypto';
import { authDb } from './authDatabase';

export interface User {
  id: string;
  email: string;
  username: string;
  passwordHash: string;
  createdAt: string;
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
    'SELECT id, email, username, password_hash as passwordHash, created_at as createdAt FROM users WHERE email = ?',
    [email.toLowerCase()]
  );
  return rows[0];
}

export async function findUserById(id: string): Promise<User | undefined> {
  const rows = await authDb.query<User[]>(
    'SELECT id, email, username, password_hash as passwordHash, created_at as createdAt FROM users WHERE id = ?',
    [id]
  );
  return rows[0];
}

export async function findUserByUsername(username: string): Promise<User | undefined> {
  const rows = await authDb.query<User[]>(
    'SELECT id, email, username, password_hash as passwordHash, created_at as createdAt FROM users WHERE username = ?',
    [username]
  );
  return rows[0];
}
