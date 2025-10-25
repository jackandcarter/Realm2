import { randomUUID } from 'crypto';
import { db } from './database';

export interface User {
  id: string;
  email: string;
  username: string;
  passwordHash: string;
  createdAt: string;
}

export function createUser(email: string, username: string, passwordHash: string): User {
  const user: User = {
    id: randomUUID(),
    email: email.toLowerCase(),
    username,
    passwordHash,
    createdAt: new Date().toISOString(),
  };

  const stmt = db.prepare(
    'INSERT INTO users (id, email, username, password_hash, created_at) VALUES (@id, @email, @username, @passwordHash, @createdAt)'
  );
  stmt.run(user);
  return user;
}

export function findUserByEmail(email: string): User | undefined {
  const stmt = db.prepare(
    'SELECT id, email, username, password_hash as passwordHash, created_at as createdAt FROM users WHERE email = ?'
  );
  return stmt.get(email.toLowerCase()) as User | undefined;
}

export function findUserById(id: string): User | undefined {
  const stmt = db.prepare(
    'SELECT id, email, username, password_hash as passwordHash, created_at as createdAt FROM users WHERE id = ?'
  );
  return stmt.get(id) as User | undefined;
}

export function findUserByUsername(username: string): User | undefined {
  const stmt = db.prepare(
    'SELECT id, email, username, password_hash as passwordHash, created_at as createdAt FROM users WHERE username = ?'
  );
  return stmt.get(username) as User | undefined;
}
