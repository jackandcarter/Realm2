import { randomUUID } from 'crypto';
import { authDb } from './authDatabase';

export interface RefreshToken {
  id: string;
  userId: string;
  tokenHash: string;
  expiresAt: string;
  createdAt: string;
}

export async function storeRefreshToken(
  userId: string,
  tokenHash: string,
  expiresAt: Date
): Promise<RefreshToken> {
  const token: RefreshToken = {
    id: randomUUID(),
    userId,
    tokenHash,
    expiresAt: expiresAt.toISOString(),
    createdAt: new Date().toISOString(),
  };
  await authDb.execute(
    'INSERT INTO refresh_tokens (id, user_id, token_hash, expires_at, created_at) VALUES (?, ?, ?, ?, ?)',
    [token.id, token.userId, token.tokenHash, token.expiresAt, token.createdAt]
  );
  return token;
}

export async function removeRefreshTokenByHash(tokenHash: string): Promise<void> {
  await authDb.execute('DELETE FROM refresh_tokens WHERE token_hash = ?', [tokenHash]);
}

export async function findRefreshToken(tokenHash: string): Promise<RefreshToken | undefined> {
  const rows = await authDb.query<RefreshToken[]>(
    'SELECT id, user_id as userId, token_hash as tokenHash, expires_at as expiresAt, created_at as createdAt FROM refresh_tokens WHERE token_hash = ?',
    [tokenHash]
  );
  return rows[0];
}

export async function removeUserTokens(userId: string): Promise<void> {
  await authDb.execute('DELETE FROM refresh_tokens WHERE user_id = ?', [userId]);
}
