import { catalogApp } from '../apps/catalogApp';
import { env } from '../config/env';
import { logger } from '../observability/logger';
import { startHttpService } from './serviceBootstrap';

startHttpService({
  app: catalogApp,
  port: env.catalogPort,
  serviceName: 'catalog',
}).catch((error) => {
  logger.error({ err: error }, 'Catalog service failed to start');
  process.exit(1);
});
