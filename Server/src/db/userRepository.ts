import { randomUUID } from 'crypto';
import { db } from './database';

export interface User {
  id: string;
  email: string;
  passwordHash: string;
  createdAt: string;
}

export function createUser(email: string, passwordHash: string): User {
  const user: User = {
    id: randomUUID(),
    email: email.toLowerCase(),
    passwordHash,
    createdAt: new Date().toISOString(),
  };

  const stmt = db.prepare(
    'INSERT INTO users (id, email, password_hash, created_at) VALUES (@id, @email, @passwordHash, @createdAt)'
  );
  stmt.run(user);
  return user;
}

export function findUserByEmail(email: string): User | undefined {
  const stmt = db.prepare('SELECT id, email, password_hash as passwordHash, created_at as createdAt FROM users WHERE email = ?');
  return stmt.get(email.toLowerCase()) as User | undefined;
}

export function findUserById(id: string): User | undefined {
  const stmt = db.prepare('SELECT id, email, password_hash as passwordHash, created_at as createdAt FROM users WHERE id = ?');
  return stmt.get(id) as User | undefined;
}
