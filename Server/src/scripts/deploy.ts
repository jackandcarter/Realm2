import { logger } from '../observability/logger';
import { initializeWorldDatabase } from '../db/database';
import { initializeTerrainDatabase } from '../db/terrainDatabase';

async function main(): Promise<void> {
  await initializeWorldDatabase();
  await initializeTerrainDatabase();
  logger.info('Deployment migrations completed successfully');
}

main().catch((error) => {
  logger.error({ err: error }, 'Deployment script failed');
  process.exitCode = 1;
});
