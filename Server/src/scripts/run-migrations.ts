import { initializeDatabase } from '../db/database';
import { logger } from '../observability/logger';

async function main(): Promise<void> {
  await initializeDatabase();
  logger.info('Database migrations complete');
}

main().catch((error) => {
  logger.error({ err: error }, 'Migration script failed');
  process.exitCode = 1;
});
