import dotenv from 'dotenv';

if (!process.env.DOTENV_LOG_LEVEL) {
  process.env.DOTENV_LOG_LEVEL = 'error';
}
dotenv.config();

export const env = {
  port: parseInt(process.env.PORT ?? '3000', 10),
  gatewayPort: parseInt(process.env.GATEWAY_PORT ?? process.env.PORT ?? '3000', 10),
  authPort: parseInt(process.env.AUTH_PORT ?? '3001', 10),
  worldPort: parseInt(process.env.WORLD_PORT ?? '3002', 10),
  combatPort: parseInt(process.env.COMBAT_PORT ?? '3003', 10),
  catalogPort: parseInt(process.env.CATALOG_PORT ?? '3004', 10),
  economyPort: parseInt(process.env.ECONOMY_PORT ?? '3005', 10),
  socialPort: parseInt(process.env.SOCIAL_PORT ?? '3006', 10),
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
  authDatabaseHost: process.env.AUTH_DB_HOST ?? process.env.DB_HOST ?? '127.0.0.1',
  authDatabasePort: parseInt(process.env.AUTH_DB_PORT ?? process.env.DB_PORT ?? '3306', 10),
  authDatabaseUser: process.env.AUTH_DB_USER ?? process.env.DB_USER ?? 'realm2',
  authDatabasePassword: process.env.AUTH_DB_PASSWORD ?? process.env.DB_PASSWORD ?? '',
  authDatabaseName: process.env.AUTH_DB_NAME ?? process.env.DB_NAME ?? 'realm2',
  authDatabaseSsl: process.env.AUTH_DB_SSL === 'true',
  authDatabaseConnectionLimit: parseInt(
    process.env.AUTH_DB_POOL_LIMIT ?? process.env.DB_POOL_LIMIT ?? '10',
    10
  ),
};
