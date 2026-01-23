import { app } from './app';
import { env } from './config/env';
import { logger } from './observability/logger';
import { startHttpService } from './servers/serviceBootstrap';

async function startServer(): Promise<void> {
  await startHttpService({
    app,
    port: env.port,
    serviceName: 'gateway',
    enableWorldSockets: true,
    enableActionProcessor: true,
    enableCommandConsole: true,
  });
}

startServer().catch((error) => {
  logger.error({ err: error }, 'Server failed to start');
  process.exit(1);
});
