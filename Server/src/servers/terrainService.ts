import { terrainApp } from '../apps/terrainApp';
import { env } from '../config/env';
import { logger } from '../observability/logger';
import { startHttpService } from './serviceBootstrap';

startHttpService({
  app: terrainApp,
  port: env.terrainPort,
  serviceName: 'terrain',
  initializeWorldDb: false,
  initializeTerrainDb: true,
  enableChunkSockets: true,
}).catch((error) => {
  logger.error({ err: error }, 'Terrain service failed to start');
  process.exit(1);
});
