import http from 'http';
import { WebSocketServer } from 'ws';
import { app } from './app';
import { env } from './config/env';
import { registerProgressionSocketHandlers } from './services/progressionService';

const server = http.createServer(app);
const wsServer = new WebSocketServer({ server, path: '/ws/progression' });
registerProgressionSocketHandlers(wsServer);

server.listen(env.port, () => {
  console.log(`Server listening on port ${env.port}`);
});
