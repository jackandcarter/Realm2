import path from 'path';
import fs from 'fs';
import dotenv from 'dotenv';

if (!process.env.DOTENV_LOG_LEVEL) {
  process.env.DOTENV_LOG_LEVEL = 'error';
}
dotenv.config();

const dataDir = process.env.DATA_DIR ?? path.join(process.cwd(), 'data');
if (!fs.existsSync(dataDir)) {
  fs.mkdirSync(dataDir, { recursive: true });
}

export const env = {
  port: parseInt(process.env.PORT ?? '3000', 10),
  jwtSecret: process.env.JWT_SECRET ?? 'dev-secret-change-me',
  accessTokenTtlSeconds: parseInt(process.env.ACCESS_TOKEN_TTL ?? '900', 10), // 15 minutes
  refreshTokenTtlSeconds: parseInt(process.env.REFRESH_TOKEN_TTL ?? '604800', 10), // 7 days
  databasePath: process.env.DB_PATH ?? process.env.DATABASE_URL ?? path.join(dataDir, 'app.db'),
};
