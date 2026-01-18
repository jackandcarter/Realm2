import { logger } from '../observability/logger';
import { initializeDatabase } from '../db/database';

async function main(): Promise<void> {
  await initializeDatabase();
  logger.info('Deployment migrations completed successfully');
}

main().catch((error) => {
  logger.error({ err: error }, 'Deployment script failed');
  process.exitCode = 1;
});
