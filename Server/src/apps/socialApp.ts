import { socialRouter } from '../routes/socialRoutes';
import { createServiceApp } from './createServiceApp';

export const socialApp = createServiceApp({
  serviceName: 'social',
  routes: [{ path: '/social', router: socialRouter }],
});
