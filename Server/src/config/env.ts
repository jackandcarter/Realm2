import dotenv from 'dotenv';

if (!process.env.DOTENV_LOG_LEVEL) {
  process.env.DOTENV_LOG_LEVEL = 'error';
}
dotenv.config();

export const env = {
  port: parseInt(process.env.PORT ?? '3000', 10),
  jwtSecret: process.env.JWT_SECRET ?? 'dev-secret-change-me',
  accessTokenTtlSeconds: parseInt(process.env.ACCESS_TOKEN_TTL ?? '900', 10), // 15 minutes
  refreshTokenTtlSeconds: parseInt(process.env.REFRESH_TOKEN_TTL ?? '604800', 10), // 7 days
  databaseHost: process.env.DB_HOST ?? '127.0.0.1',
  databasePort: parseInt(process.env.DB_PORT ?? '3306', 10),
  databaseUser: process.env.DB_USER ?? 'realm2',
  databasePassword: process.env.DB_PASSWORD ?? '',
  databaseName: process.env.DB_NAME ?? 'realm2',
  databaseSsl: process.env.DB_SSL === 'true',
  databaseConnectionLimit: parseInt(process.env.DB_POOL_LIMIT ?? '10', 10),
};
