import Database from 'better-sqlite3';
import { env } from '../config/env';
import { createBackupSnapshot } from '../maintenance/backupManager';
import { logger } from '../observability/logger';

async function main(): Promise<void> {
  const connection = new Database(env.databasePath);
  connection.pragma('foreign_keys = ON');
  connection.pragma('journal_mode = WAL');
  try {
    const snapshotPath = await createBackupSnapshot('manual', connection);
    logger.info({ snapshotPath }, 'Manual backup completed');
  } finally {
    connection.close();
  }
}

main().catch((error) => {
  logger.error({ err: error }, 'Backup script failed');
  process.exitCode = 1;
});
