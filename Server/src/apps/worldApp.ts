import { realmRouter } from '../routes/realmRoutes';
import { characterRouter } from '../routes/characterRoutes';
import { createServiceApp } from './createServiceApp';

export const worldApp = createServiceApp({
  serviceName: 'world',
  routes: [
    { path: '/realms', router: realmRouter },
    { path: '/characters', router: characterRouter },
  ],
});
