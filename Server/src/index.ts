import http from 'http';
import { WebSocketServer } from 'ws';
import { app } from './app';
import { env } from './config/env';
import { registerProgressionSocketHandlers } from './services/progressionService';
import { registerChunkSocketHandlers } from './services/chunkSocketService';
import { logger } from './observability/logger';
import { initializeDatabase } from './db/database';
import { loadContentCatalogFromDisk } from './gameplay/design/contentLoader';

async function startServer(): Promise<void> {
  await initializeDatabase();
  await loadContentCatalogFromDisk(env.contentCatalogPath);
  const server = http.createServer(app);
  const wsServer = new WebSocketServer({ server, path: '/ws/progression' });
  registerProgressionSocketHandlers(wsServer);
  const chunkWsServer = new WebSocketServer({ server, path: '/ws/chunks' });
  registerChunkSocketHandlers(chunkWsServer);

  server.listen(env.port, () => {
    logger.info({ port: env.port }, 'Server listening');
  });
}

startServer().catch((error) => {
  logger.error({ err: error }, 'Server failed to start');
  process.exit(1);
});
