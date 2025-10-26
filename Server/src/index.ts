import http from 'http';
import { WebSocketServer } from 'ws';
import { app } from './app';
import { env } from './config/env';
import { registerProgressionSocketHandlers } from './services/progressionService';
import { registerChunkSocketHandlers } from './services/chunkSocketService';
import { logger } from './observability/logger';
import { startBackupScheduler } from './maintenance/backupManager';

const server = http.createServer(app);
const wsServer = new WebSocketServer({ server, path: '/ws/progression' });
registerProgressionSocketHandlers(wsServer);
const chunkWsServer = new WebSocketServer({ server, path: '/ws/chunks' });
registerChunkSocketHandlers(chunkWsServer);

server.listen(env.port, () => {
  logger.info({ port: env.port }, 'Server listening');
  startBackupScheduler();
});
