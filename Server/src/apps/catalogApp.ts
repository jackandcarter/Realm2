import { catalogRouter } from '../routes/catalogRoutes';
import { createServiceApp } from './createServiceApp';

export const catalogApp = createServiceApp({
  serviceName: 'catalog',
  routes: [{ path: '/catalog', router: catalogRouter }],
});
