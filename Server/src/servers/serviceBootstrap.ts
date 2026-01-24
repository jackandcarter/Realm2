import http from 'http';
import { WebSocketServer } from 'ws';
import type { Express } from 'express';
import { logger } from '../observability/logger';
import { initializeAuthDatabase } from '../db/authDatabase';
import { initializeWorldDatabase } from '../db/database';
import { registerProgressionSocketHandlers } from '../services/progressionService';
import { registerChunkSocketHandlers } from '../services/chunkSocketService';
import { startActionRequestProcessor } from '../services/actionRequestProcessor';
import { startCommandConsole } from '../cli/commandConsole';

export interface ServiceBootstrapOptions {
  app: Express;
  port: number;
  serviceName: string;
  initializeDb?: boolean;
  initializeAuthDb?: boolean;
  initializeWorldDb?: boolean;
  enableWorldSockets?: boolean;
  enableActionProcessor?: boolean;
  enableCommandConsole?: boolean;
}

export async function startHttpService(options: ServiceBootstrapOptions): Promise<void> {
  if (options.initializeDb !== false) {
    if (options.initializeAuthDb) {
      await initializeAuthDatabase();
    }
    if (options.initializeWorldDb !== false) {
      await initializeWorldDatabase();
    }
  }

  const server = http.createServer(options.app);

  if (options.enableWorldSockets) {
    const wsServer = new WebSocketServer({ server, path: '/ws/progression' });
    registerProgressionSocketHandlers(wsServer);
    const chunkWsServer = new WebSocketServer({ server, path: '/ws/chunks' });
    registerChunkSocketHandlers(chunkWsServer);
  }

  if (options.enableActionProcessor) {
    startActionRequestProcessor();
  }

  server.listen(options.port, () => {
    logger.info({ port: options.port, service: options.serviceName }, 'Service listening');
  });

  if (options.enableCommandConsole) {
    startCommandConsole();
  }
}
