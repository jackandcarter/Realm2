import bcrypt from 'bcryptjs';
import jwt from 'jsonwebtoken';
import crypto from 'crypto';
import { env } from '../config/env';
import { createUser, findUserByEmail, findUserById } from '../db/userRepository';
import type { User } from '../db/userRepository';
import {
  storeRefreshToken,
  removeRefreshTokenByHash,
  findRefreshToken,
  removeUserTokens,
} from '../db/refreshTokenRepository';

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
}

export interface AuthResult {
  user: Pick<User, 'id' | 'email' | 'createdAt'>;
  tokens: AuthTokens;
}

function hashRefreshToken(token: string): string {
  return crypto.createHash('sha256').update(token).digest('hex');
}

function createAccessToken(user: User): string {
  return jwt.sign(
    {
      sub: user.id,
      email: user.email,
    },
    env.jwtSecret,
    { expiresIn: env.accessTokenTtlSeconds }
  );
}

function createRefreshToken(): { raw: string; hashed: string; expiresAt: Date } {
  const raw = crypto.randomBytes(32).toString('hex');
  const expiresAt = new Date(Date.now() + env.refreshTokenTtlSeconds * 1000);
  return { raw, hashed: hashRefreshToken(raw), expiresAt };
}

function toPublicUser(user: User): Pick<User, 'id' | 'email' | 'createdAt'> {
  return { id: user.id, email: user.email, createdAt: user.createdAt };
}

export async function register(email: string, password: string): Promise<AuthResult> {
  const existing = findUserByEmail(email);
  if (existing) {
    throw new Error('Email already registered');
  }

  const passwordHash = await bcrypt.hash(password, 10);
  const user = createUser(email, passwordHash);
  const tokens = await issueTokens(user);
  return { user: toPublicUser(user), tokens };
}

export async function login(email: string, password: string): Promise<AuthResult> {
  const user = findUserByEmail(email);
  if (!user) {
    throw new Error('Invalid email or password');
  }

  const matches = await bcrypt.compare(password, user.passwordHash);
  if (!matches) {
    throw new Error('Invalid email or password');
  }

  removeUserTokens(user.id);
  const tokens = await issueTokens(user);
  return { user: toPublicUser(user), tokens };
}

export async function logout(refreshToken: string): Promise<void> {
  const tokenHash = hashRefreshToken(refreshToken);
  removeRefreshTokenByHash(tokenHash);
}

export async function refresh(refreshToken: string): Promise<AuthTokens> {
  const tokenHash = hashRefreshToken(refreshToken);
  const stored = findRefreshToken(tokenHash);
  if (!stored) {
    throw new Error('Invalid refresh token');
  }

  if (new Date(stored.expiresAt) < new Date()) {
    removeRefreshTokenByHash(tokenHash);
    throw new Error('Refresh token expired');
  }

  const user = findUserById(stored.userId);
  if (!user) {
    removeRefreshTokenByHash(tokenHash);
    throw new Error('Invalid refresh token');
  }

  removeRefreshTokenByHash(tokenHash);
  return issueTokens(user);
}

async function issueTokens(user: User): Promise<AuthTokens> {
  const accessToken = createAccessToken(user);
  const refreshToken = createRefreshToken();
  storeRefreshToken(user.id, refreshToken.hashed, refreshToken.expiresAt);
  return { accessToken, refreshToken: refreshToken.raw };
}
