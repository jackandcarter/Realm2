import { randomUUID } from 'crypto';
import { db } from './database';

export interface RefreshToken {
  id: string;
  userId: string;
  tokenHash: string;
  expiresAt: string;
  createdAt: string;
}

export function storeRefreshToken(userId: string, tokenHash: string, expiresAt: Date): RefreshToken {
  const token: RefreshToken = {
    id: randomUUID(),
    userId,
    tokenHash,
    expiresAt: expiresAt.toISOString(),
    createdAt: new Date().toISOString(),
  };
  const stmt = db.prepare(
    'INSERT INTO refresh_tokens (id, user_id, token_hash, expires_at, created_at) VALUES (@id, @userId, @tokenHash, @expiresAt, @createdAt)'
  );
  stmt.run(token);
  return token;
}

export function removeRefreshTokenByHash(tokenHash: string): void {
  const stmt = db.prepare('DELETE FROM refresh_tokens WHERE token_hash = ?');
  stmt.run(tokenHash);
}

export function findRefreshToken(tokenHash: string): RefreshToken | undefined {
  const stmt = db.prepare(
    'SELECT id, user_id as userId, token_hash as tokenHash, expires_at as expiresAt, created_at as createdAt FROM refresh_tokens WHERE token_hash = ?'
  );
  return stmt.get(tokenHash) as RefreshToken | undefined;
}

export function removeUserTokens(userId: string): void {
  const stmt = db.prepare('DELETE FROM refresh_tokens WHERE user_id = ?');
  stmt.run(userId);
}
