import { realmRouter } from '../routes/realmRoutes';
import { realmChunkRouter } from '../routes/chunkRoutes';
import { characterRouter } from '../routes/characterRoutes';
import { createServiceApp } from './createServiceApp';

export const worldApp = createServiceApp({
  serviceName: 'world',
  routes: [
    { path: '/realms', router: realmRouter },
    { path: '/realms', router: realmChunkRouter },
    { path: '/characters', router: characterRouter },
  ],
});
