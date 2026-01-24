import { realmChunkRouter } from '../routes/chunkRoutes';
import { terrainRouter } from '../routes/terrainRoutes';
import { createServiceApp } from './createServiceApp';

export const terrainApp = createServiceApp({
  serviceName: 'terrain',
  routes: [
    { path: '/realms', router: realmChunkRouter },
    { path: '/realms/:realmId/terrain', router: terrainRouter },
  ],
});
