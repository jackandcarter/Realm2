import { combatApp } from '../apps/combatApp';
import { env } from '../config/env';
import { logger } from '../observability/logger';
import { startHttpService } from './serviceBootstrap';

startHttpService({
  app: combatApp,
  port: env.combatPort,
  serviceName: 'combat',
}).catch((error) => {
  logger.error({ err: error }, 'Combat service failed to start');
  process.exit(1);
});
