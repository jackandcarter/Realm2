import { restoreFromSnapshot } from '../maintenance/backupManager';
import { logger } from '../observability/logger';

async function main(): Promise<void> {
  const snapshot = process.argv[2];
  if (!snapshot) {
    logger.error('Usage: npm run restore -- <snapshot-file>');
    process.exitCode = 1;
    return;
  }
  await restoreFromSnapshot(snapshot);
  logger.info({ snapshot }, 'Database restore completed');
}

main().catch((error) => {
  logger.error({ err: error }, 'Restore script failed');
  process.exitCode = 1;
});
