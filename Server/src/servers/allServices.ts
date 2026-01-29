import { authApp } from '../apps/authApp';
import { catalogApp } from '../apps/catalogApp';
import { combatApp } from '../apps/combatApp';
import { devToolkitApp } from '../apps/devToolkitApp';
import { economyApp } from '../apps/economyApp';
import { gatewayApp } from '../apps/gatewayApp';
import { socialApp } from '../apps/socialApp';
import { terrainApp } from '../apps/terrainApp';
import { worldApp } from '../apps/worldApp';
import { env } from '../config/env';
import { initializeAuthDatabase } from '../db/authDatabase';
import { initializeWorldDatabase } from '../db/database';
import { initializeTerrainDatabase } from '../db/terrainDatabase';
import { logger } from '../observability/logger';
import { startHttpService } from './serviceBootstrap';

async function startAll(): Promise<void> {
  await initializeAuthDatabase();
  await initializeWorldDatabase();
  await initializeTerrainDatabase();

  await startHttpService({
    app: authApp,
    port: env.authPort,
    serviceName: 'auth',
    initializeDb: false,
  });

  await startHttpService({
    app: worldApp,
    port: env.worldPort,
    serviceName: 'world',
    initializeDb: false,
    enableWorldSockets: true,
    enableActionProcessor: true,
  });

  await startHttpService({
    app: terrainApp,
    port: env.terrainPort,
    serviceName: 'terrain',
    initializeDb: false,
    enableChunkSockets: true,
  });

  await startHttpService({
    app: devToolkitApp,
    port: env.devToolkitPort,
    serviceName: 'devtoolkit',
    initializeDb: false,
  });

  await startHttpService({
    app: combatApp,
    port: env.combatPort,
    serviceName: 'combat',
    initializeDb: false,
  });

  await startHttpService({
    app: catalogApp,
    port: env.catalogPort,
    serviceName: 'catalog',
    initializeDb: false,
  });

  await startHttpService({
    app: economyApp,
    port: env.economyPort,
    serviceName: 'economy',
    initializeDb: false,
  });

  await startHttpService({
    app: socialApp,
    port: env.socialPort,
    serviceName: 'social',
    initializeDb: false,
  });

  await startHttpService({
    app: gatewayApp,
    port: env.gatewayPort,
    serviceName: 'gateway',
    initializeDb: false,
    enableCommandConsole: true,
  });
}

startAll().catch((error) => {
  logger.error({ err: error }, 'Failed to start service suite');
  process.exit(1);
});
