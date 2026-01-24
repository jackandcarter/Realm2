import { initializeWorldDatabase } from '../db/database';
import { initializeTerrainDatabase } from '../db/terrainDatabase';
import { logger } from '../observability/logger';

async function main(): Promise<void> {
  await initializeWorldDatabase();
  await initializeTerrainDatabase();
  logger.info('Database migrations complete');
}

main().catch((error) => {
  logger.error({ err: error }, 'Migration script failed');
  process.exitCode = 1;
});
