import { socialApp } from '../apps/socialApp';
import { env } from '../config/env';
import { logger } from '../observability/logger';
import { startHttpService } from './serviceBootstrap';

startHttpService({
  app: socialApp,
  port: env.socialPort,
  serviceName: 'social',
}).catch((error) => {
  logger.error({ err: error }, 'Social service failed to start');
  process.exit(1);
});
