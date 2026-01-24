import { realmChunkRouter } from '../routes/chunkRoutes';
import { createServiceApp } from './createServiceApp';

export const terrainApp = createServiceApp({
  serviceName: 'terrain',
  routes: [{ path: '/realms', router: realmChunkRouter }],
});
