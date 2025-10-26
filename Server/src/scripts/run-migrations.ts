import Database from 'better-sqlite3';
import { env } from '../config/env';
import { runMigrations } from '../db/migrationRunner';
import { logger } from '../observability/logger';

function main(): void {
  const connection = new Database(env.databasePath);
  connection.pragma('foreign_keys = ON');
  connection.pragma('journal_mode = WAL');
  try {
    runMigrations(connection);
    logger.info('Database migrations complete');
  } finally {
    connection.close();
  }
}

try {
  main();
} catch (error) {
  logger.error({ err: error }, 'Migration script failed');
  process.exitCode = 1;
}
