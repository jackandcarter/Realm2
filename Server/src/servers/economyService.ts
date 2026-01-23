import { economyApp } from '../apps/economyApp';
import { env } from '../config/env';
import { logger } from '../observability/logger';
import { startHttpService } from './serviceBootstrap';

startHttpService({
  app: economyApp,
  port: env.economyPort,
  serviceName: 'economy',
}).catch((error) => {
  logger.error({ err: error }, 'Economy service failed to start');
  process.exit(1);
});
