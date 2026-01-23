import { economyRouter } from '../routes/economyRoutes';
import { createServiceApp } from './createServiceApp';

export const economyApp = createServiceApp({
  serviceName: 'economy',
  routes: [{ path: '/economy', router: economyRouter }],
});
