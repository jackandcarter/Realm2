import { devToolkitRouter } from '../routes/devToolkitRoutes';
import { devToolkitUiRouter } from '../routes/devToolkitUiRoutes';
import { createServiceApp } from './createServiceApp';

export const devToolkitApp = createServiceApp({
  serviceName: 'devtoolkit',
  routes: [
    { path: '/', router: devToolkitUiRouter },
    { path: '/api/devtools', router: devToolkitRouter },
  ],
});
