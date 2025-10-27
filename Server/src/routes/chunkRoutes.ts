import { NextFunction, Request, Response, Router } from 'express';
import { requireAuth } from '../middleware/authMiddleware';
import {
  assertChunkAccess,
  getChunkChangeFeed,
  getRealmChunkSnapshot,
  recordChunkChange,
} from '../services/chunkService';
import { addChunkListener, removeChunkListener } from '../services/chunkStreamService';
import { ChunkChangeDTO } from '../types/chunk';
import { HttpError, isHttpError } from '../utils/errors';

const chunkRouter = Router({ mergeParams: true });

chunkRouter.get('/', requireAuth, (req, res, next) => {
  try {
    const { realmId } = req.params as { realmId: string };
    const since = req.query.since as string | undefined;
    const envelope = getRealmChunkSnapshot(req.user!.id, realmId, since);
    res.json(envelope);
  } catch (error) {
    next(error);
  }
});

chunkRouter.get('/changes', requireAuth, (req, res, next) => {
  try {
    const { realmId } = req.params as { realmId: string };
    const since = req.query.since as string | undefined;
    const limit = req.query.limit ? Number(req.query.limit) : undefined;
    if (limit !== undefined && (Number.isNaN(limit) || limit <= 0)) {
      throw new HttpError(400, 'limit must be a positive integer');
    }
    const envelope = getChunkChangeFeed(req.user!.id, realmId, since, limit);
    res.json(envelope);
  } catch (error) {
    next(error);
  }
});

chunkRouter.post('/:chunkId/changes', requireAuth, (req, res, next) => {
  try {
    const { realmId, chunkId } = req.params as { realmId: string; chunkId: string };
    const { changeType, chunk, structures, plots, resources } = req.body ?? {};
    const change = recordChunkChange(
      req.user!.id,
      realmId,
      chunkId,
      changeType,
      chunk,
      structures,
      plots,
      resources
    );
    res.status(201).json({ change });
  } catch (error) {
    next(error);
  }
});

chunkRouter.get('/stream', requireAuth, (req, res, next) => {
  try {
    const { realmId } = req.params as { realmId: string };
    assertChunkAccess(req.user!.id, realmId);

    res.setHeader('Content-Type', 'text/event-stream');
    res.setHeader('Cache-Control', 'no-cache');
    res.setHeader('Connection', 'keep-alive');
    if (typeof res.flushHeaders === 'function') {
      res.flushHeaders();
    }
    res.write(': connected\n\n');

    const listener = (change: ChunkChangeDTO) => {
      res.write(`data: ${JSON.stringify(change)}\n\n`);
    };

    addChunkListener(realmId, listener);

    const heartbeat = setInterval(() => {
      res.write(': keep-alive\n\n');
    }, 30000);

    req.on('close', () => {
      clearInterval(heartbeat);
      removeChunkListener(realmId, listener);
    });
  } catch (error) {
    next(error);
  }
});

chunkRouter.use((err: unknown, _req: Request, res: Response, _next: NextFunction) => {
  if (isHttpError(err)) {
    const httpErr = err as HttpError;
    res.status(httpErr.status).json({
      message: httpErr.message,
      retryable: httpErr.retryable ?? false,
      retryAfterMs: httpErr.retryAfterMs,
    });
    return;
  }
  res.status(500).json({ message: 'Failed to process chunk request' });
});

export const realmChunkRouter = Router();
realmChunkRouter.use('/:realmId/chunks', chunkRouter);
