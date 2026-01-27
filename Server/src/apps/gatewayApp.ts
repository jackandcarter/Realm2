import { authRouter } from '../routes/authRoutes';
import { realmRouter } from '../routes/realmRoutes';
import { characterRouter } from '../routes/characterRoutes';
import { combatRouter } from '../routes/combatRoutes';
import { catalogRouter } from '../routes/catalogRoutes';
import { economyRouter } from '../routes/economyRoutes';
import { socialRouter } from '../routes/socialRoutes';
import { createServiceApp } from './createServiceApp';

export const gatewayApp = createServiceApp({
  serviceName: 'gateway',
  routes: [
    { path: '/auth', router: authRouter },
    { path: '/realms', router: realmRouter },
    { path: '/characters', router: characterRouter },
    { path: '/combat', router: combatRouter },
    { path: '/catalog', router: catalogRouter },
    { path: '/economy', router: economyRouter },
    { path: '/social', router: socialRouter },
  ],
  enableDocs: true,
});
