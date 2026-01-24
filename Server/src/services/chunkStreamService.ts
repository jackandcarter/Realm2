import { ChunkChangeDTO } from '../types/chunk';
import { logger } from '../observability/logger';

type ChunkListener = (change: ChunkChangeDTO) => void;

const listeners = new Map<string, Set<ChunkListener>>();

export function addChunkListener(realmId: string, listener: ChunkListener): void {
  const existing = listeners.get(realmId);
  if (existing) {
    existing.add(listener);
    return;
  }
  listeners.set(realmId, new Set([listener]));
}

export function removeChunkListener(realmId: string, listener: ChunkListener): void {
  const existing = listeners.get(realmId);
  if (!existing) {
    return;
  }
  existing.delete(listener);
  if (existing.size === 0) {
    listeners.delete(realmId);
  }
}

export function publishChunkChange(change: ChunkChangeDTO): void {
  logger.info(
    {
      realmId: change.realmId,
      chunkId: change.chunkId,
      changeType: change.changeType,
      changeId: change.changeId,
    },
    'Terrain change published'
  );
  const realmListeners = listeners.get(change.realmId);
  if (!realmListeners) {
    return;
  }
  for (const listener of realmListeners) {
    try {
      listener(change);
    } catch (error) {
      if (process.env.NODE_ENV !== 'test') {
        logger.warn({ err: error, realmId: change.realmId }, 'Failed to notify chunk listener');
      }
    }
  }
}
