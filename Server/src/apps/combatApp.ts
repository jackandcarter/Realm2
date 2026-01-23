import { combatRouter } from '../routes/combatRoutes';
import { createServiceApp } from './createServiceApp';

export const combatApp = createServiceApp({
  serviceName: 'combat',
  routes: [{ path: '/combat', router: combatRouter }],
});
