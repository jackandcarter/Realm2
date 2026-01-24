import { authApp } from '../apps/authApp';
import { env } from '../config/env';
import { startHttpService } from './serviceBootstrap';
import { logger } from '../observability/logger';

startHttpService({
  app: authApp,
  port: env.authPort,
  serviceName: 'auth',
  initializeAuthDb: true,
  initializeWorldDb: false,
}).catch((error) => {
  logger.error({ err: error }, 'Auth service failed to start');
  process.exit(1);
});
