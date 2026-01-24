import { worldApp } from '../apps/worldApp';
import { env } from '../config/env';
import { logger } from '../observability/logger';
import { startHttpService } from './serviceBootstrap';

startHttpService({
  app: worldApp,
  port: env.worldPort,
  serviceName: 'world',
  initializeWorldDb: true,
  enableWorldSockets: true,
  enableActionProcessor: true,
}).catch((error) => {
  logger.error({ err: error }, 'World service failed to start');
  process.exit(1);
});
