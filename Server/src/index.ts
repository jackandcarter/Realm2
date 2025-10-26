import http from 'http';
import { WebSocketServer } from 'ws';
import { app } from './app';
import { env } from './config/env';
import { registerProgressionSocketHandlers } from './services/progressionService';
import { registerChunkSocketHandlers } from './services/chunkSocketService';

const server = http.createServer(app);
const wsServer = new WebSocketServer({ server, path: '/ws/progression' });
registerProgressionSocketHandlers(wsServer);
const chunkWsServer = new WebSocketServer({ server, path: '/ws/chunks' });
registerChunkSocketHandlers(chunkWsServer);

server.listen(env.port, () => {
  console.log(`Server listening on port ${env.port}`);
});
