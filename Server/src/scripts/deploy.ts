import { logger } from '../observability/logger';
import { initializeWorldDatabase } from '../db/database';

async function main(): Promise<void> {
  await initializeWorldDatabase();
  logger.info('Deployment migrations completed successfully');
}

main().catch((error) => {
  logger.error({ err: error }, 'Deployment script failed');
  process.exitCode = 1;
});
