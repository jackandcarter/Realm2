import { randomUUID } from 'crypto';
import {
  ChunkChangeRecord,
  ChunkPlotRecord,
  ChunkStructureRecord,
  findChunkById,
  listChunkChanges,
  listChunksByRealm,
  listPlotsForChunks,
  listStructuresForChunks,
  logChunkChange,
  upsertChunk,
  upsertPlots,
  upsertStructures,
} from '../db/chunkRepository';
import { findRealmById } from '../db/realmRepository';
import { findMembership } from '../db/realmMembershipRepository';
import { HttpError } from '../utils/errors';
import {
  ChunkChangeDTO,
  ChunkPlotDTO,
  ChunkSnapshotDTO,
  ChunkStructureDTO,
  RealmChunkDTO,
} from '../types/chunk';
import { publishChunkChange } from './chunkStreamService';

export interface ChunkSnapshotEnvelope {
  realmId: string;
  serverTimestamp: string;
  chunks: ChunkSnapshotDTO[];
}

export interface ChunkChangeEnvelope {
  realmId: string;
  serverTimestamp: string;
  changes: ChunkChangeDTO[];
}

interface ChunkUpdateInput {
  chunkX?: number;
  chunkZ?: number;
  payload?: unknown;
  isDeleted?: boolean;
}

interface StructureUpdateInput {
  structureId?: string;
  structureType: string;
  data?: unknown;
  isDeleted?: boolean;
}

interface PlotUpdateInput {
  plotId?: string;
  plotIdentifier?: string;
  ownerUserId?: string | null;
  data?: unknown;
  isDeleted?: boolean;
}

interface ChunkChangePayload {
  chunk?: RealmChunkDTO | undefined;
  structures?: ChunkStructureDTO[] | undefined;
  plots?: ChunkPlotDTO[] | undefined;
}

function ensureRealmAccess(userId: string, realmId: string) {
  const realm = findRealmById(realmId);
  if (!realm) {
    throw new HttpError(404, 'Realm not found');
  }
  const membership = findMembership(userId, realmId);
  if (!membership) {
    throw new HttpError(403, 'Join the realm before accessing its terrain data');
  }
  return { realm, membership };
}

export function assertChunkAccess(userId: string, realmId: string): void {
  ensureRealmAccess(userId, realmId);
}

function toChunkDTO(record: {
  id: string;
  realmId: string;
  chunkX: number;
  chunkZ: number;
  payloadJson: string;
  isDeleted: boolean;
  createdAt: string;
  updatedAt: string;
}): RealmChunkDTO {
  return {
    chunkId: record.id,
    realmId: record.realmId,
    chunkX: record.chunkX,
    chunkZ: record.chunkZ,
    payload: record.payloadJson,
    isDeleted: record.isDeleted,
    createdAt: record.createdAt,
    updatedAt: record.updatedAt,
  };
}

function toStructureDTO(record: ChunkStructureRecord): ChunkStructureDTO {
  return {
    structureId: record.id,
    realmId: record.realmId,
    chunkId: record.chunkId,
    structureType: record.structureType,
    data: record.dataJson,
    isDeleted: record.isDeleted,
    createdAt: record.createdAt,
    updatedAt: record.updatedAt,
  };
}

function toPlotDTO(record: ChunkPlotRecord): ChunkPlotDTO {
  return {
    plotId: record.id,
    realmId: record.realmId,
    chunkId: record.chunkId,
    plotIdentifier: record.plotIdentifier,
    ownerUserId: record.ownerUserId,
    data: record.dataJson,
    isDeleted: record.isDeleted,
    createdAt: record.createdAt,
    updatedAt: record.updatedAt,
  };
}

function serializePayload(data: unknown): string {
  if (typeof data === 'string') {
    return data;
  }
  try {
    return JSON.stringify(data ?? {});
  } catch (error) {
    throw new HttpError(400, 'Chunk payload must be serializable');
  }
}

function deserializeChangePayload(payloadJson: string): ChunkChangePayload {
  if (!payloadJson) {
    return {};
  }
  try {
    const parsed = JSON.parse(payloadJson) as ChunkChangePayload;
    if (!parsed || typeof parsed !== 'object') {
      return {};
    }
    return parsed;
  } catch (error) {
    if (process.env.NODE_ENV !== 'test') {
      console.warn('Failed to parse chunk change payload', error);
    }
    return {};
  }
}

export function getRealmChunkSnapshot(
  userId: string,
  realmId: string,
  updatedAfter?: string
): ChunkSnapshotEnvelope {
  ensureRealmAccess(userId, realmId);
  const chunkRecords = listChunksByRealm(realmId, updatedAfter).filter((chunk) => !chunk.isDeleted);
  const chunkIds = chunkRecords.map((chunk) => chunk.id);
  const structures = listStructuresForChunks(chunkIds);
  const plots = listPlotsForChunks(chunkIds);
  const structuresByChunk = new Map<string, ChunkStructureDTO[]>();
  for (const structure of structures) {
    const collection = structuresByChunk.get(structure.chunkId) ?? [];
    collection.push(toStructureDTO(structure));
    structuresByChunk.set(structure.chunkId, collection);
  }
  const plotsByChunk = new Map<string, ChunkPlotDTO[]>();
  for (const plot of plots) {
    const collection = plotsByChunk.get(plot.chunkId) ?? [];
    collection.push(toPlotDTO(plot));
    plotsByChunk.set(plot.chunkId, collection);
  }
  const snapshots: ChunkSnapshotDTO[] = chunkRecords.map((chunk) => ({
    ...toChunkDTO(chunk),
    structures: structuresByChunk.get(chunk.id) ?? [],
    plots: plotsByChunk.get(chunk.id) ?? [],
  }));
  return {
    realmId,
    serverTimestamp: new Date().toISOString(),
    chunks: snapshots,
  };
}

export function getChunkChangeFeed(
  userId: string,
  realmId: string,
  createdAfter?: string,
  limit?: number
): ChunkChangeEnvelope {
  ensureRealmAccess(userId, realmId);
  const changeRecords = listChunkChanges(realmId, createdAfter, limit ?? 500);
  const changes: ChunkChangeDTO[] = changeRecords.map((record) => {
    const payload = deserializeChangePayload(record.payloadJson);
    return {
      changeId: record.id,
      realmId: record.realmId,
      chunkId: record.chunkId,
      changeType: record.changeType,
      createdAt: record.createdAt,
      chunk: payload.chunk,
      structures: payload.structures,
      plots: payload.plots,
    };
  });
  return {
    realmId,
    serverTimestamp: new Date().toISOString(),
    changes,
  };
}

export function recordChunkChange(
  userId: string,
  realmId: string,
  chunkId: string,
  changeType: string | undefined,
  chunk?: ChunkUpdateInput,
  structures?: StructureUpdateInput[],
  plots?: PlotUpdateInput[]
): ChunkChangeDTO {
  const { membership } = ensureRealmAccess(userId, realmId);
  if (membership.role !== 'builder') {
    throw new HttpError(403, 'Only builders can modify realm chunks');
  }

  const existingChunk = findChunkById(chunkId);
  if (!existingChunk && !chunk) {
    throw new HttpError(400, 'Chunk metadata must be supplied for new chunks');
  }
  if (existingChunk && existingChunk.realmId !== realmId) {
    throw new HttpError(403, 'Chunk does not belong to the requested realm');
  }

  let chunkRecord = existingChunk;
  if (chunk) {
    const chunkX = chunk.chunkX ?? chunkRecord?.chunkX;
    const chunkZ = chunk.chunkZ ?? chunkRecord?.chunkZ;
    if (typeof chunkX !== 'number' || typeof chunkZ !== 'number') {
      throw new HttpError(400, 'Chunk coordinates must be provided');
    }
    chunkRecord = upsertChunk({
      id: chunkId,
      realmId,
      chunkX,
      chunkZ,
      payloadJson: serializePayload(chunk.payload),
      isDeleted: Boolean(chunk.isDeleted),
    });
  }

  if (!chunkRecord) {
    throw new HttpError(500, 'Failed to load chunk after update');
  }

  const structureRecords = upsertStructures(
    (structures ?? []).map((structure) => {
      if (!structure.structureType) {
        throw new HttpError(400, 'Structure type is required');
      }
      return {
        id: structure.structureId ?? randomUUID(),
        realmId,
        chunkId: chunkRecord!.id,
        structureType: structure.structureType,
        dataJson: serializePayload(structure.data),
        isDeleted: Boolean(structure.isDeleted),
      };
    })
  );

  const plotRecords = upsertPlots(
    (plots ?? []).map((plot) => {
      const plotId = plot.plotId ?? randomUUID();
      const identifier = plot.plotIdentifier ?? plotId;
      return {
        id: plotId,
        realmId,
        chunkId: chunkRecord!.id,
        plotIdentifier: identifier,
        ownerUserId: plot.ownerUserId ?? null,
        dataJson: serializePayload(plot.data),
        isDeleted: Boolean(plot.isDeleted),
      };
    })
  );

  const payload: ChunkChangePayload = {
    chunk: chunkRecord ? toChunkDTO(chunkRecord) : undefined,
    structures: structureRecords.length > 0 ? structureRecords.map(toStructureDTO) : undefined,
    plots: plotRecords.length > 0 ? plotRecords.map(toPlotDTO) : undefined,
  };

  const change: ChunkChangeRecord = logChunkChange(
    realmId,
    chunkRecord.id,
    changeType ?? 'chunk:update',
    JSON.stringify(payload)
  );

  const changeDto: ChunkChangeDTO = {
    changeId: change.id,
    realmId: change.realmId,
    chunkId: change.chunkId,
    changeType: change.changeType,
    createdAt: change.createdAt,
    chunk: payload.chunk,
    structures: payload.structures,
    plots: payload.plots,
  };

  publishChunkChange(changeDto);

  return changeDto;
}
