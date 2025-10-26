import Database from 'better-sqlite3';
import { env } from '../config/env';
import { createBackupSnapshot } from '../maintenance/backupManager';
import { runMigrations } from '../db/migrationRunner';
import { logger } from '../observability/logger';

async function main(): Promise<void> {
  const connection = new Database(env.databasePath);
  connection.pragma('foreign_keys = ON');
  connection.pragma('journal_mode = WAL');
  try {
    await createBackupSnapshot('pre-deploy', connection);
    runMigrations(connection);
    logger.info('Deployment migrations completed successfully');
  } finally {
    connection.close();
  }
}

main().catch((error) => {
  logger.error({ err: error }, 'Deployment script failed');
  process.exitCode = 1;
});
