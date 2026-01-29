import { devToolkitApp } from '../apps/devToolkitApp';
import { env } from '../config/env';
import { logger } from '../observability/logger';
import { startHttpService } from './serviceBootstrap';

startHttpService({
  app: devToolkitApp,
  port: env.devToolkitPort,
  serviceName: 'devtoolkit',
}).catch((error) => {
  logger.error({ err: error }, 'Dev toolkit service failed to start');
  process.exit(1);
});
