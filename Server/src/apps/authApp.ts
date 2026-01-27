import { authRouter } from '../routes/authRoutes';
import { createServiceApp } from './createServiceApp';

export const authApp = createServiceApp({
  serviceName: 'auth',
  routes: [{ path: '/auth', router: authRouter }],
});
