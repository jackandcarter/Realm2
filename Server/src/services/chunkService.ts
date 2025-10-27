import { randomUUID } from 'crypto';
import { db } from '../db/database';
import {
  ChunkChangeRecord,
  ChunkPlotRecord,
  ChunkStructureRecord,
  findChunkById,
  findPlotById,
  findPlotByIdentifier,
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
  ResourceDeltaDTO,
} from '../types/chunk';
import { publishChunkChange } from './chunkStreamService';
import { applyResourceDeltas, InsufficientResourceError } from '../db/resourceWalletRepository';
import { ResourceDelta } from '../types/resources';

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

export interface ChunkUpdateInput {
  chunkX?: number;
  chunkZ?: number;
  payload?: unknown;
  isDeleted?: boolean;
}

export interface StructureUpdateInput {
  structureId?: string;
  structureType: string;
  data?: unknown;
  isDeleted?: boolean;
}

export interface PlotUpdateInput {
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
  resources?: ResourceDeltaDTO[] | undefined;
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
      resources: payload.resources,
    };
  });
  return {
    realmId,
    serverTimestamp: new Date().toISOString(),
    changes,
  };
}

function normalizeResourceDeltas(deltas: ResourceDelta[] | undefined): ResourceDeltaDTO[] {
  const normalized: ResourceDeltaDTO[] = [];
  for (const delta of deltas ?? []) {
    if (!delta) {
      continue;
    }
    const resourceType = delta.resourceType?.trim();
    if (!resourceType) {
      throw new HttpError(400, 'resourceType is required for resource adjustments');
    }
    if (!Number.isFinite(delta.quantity) || delta.quantity === 0) {
      continue;
    }
    normalized.push({ resourceType, quantity: delta.quantity });
  }
  return normalized;
}

function validatePlotOwnership(
  userId: string,
  realmId: string,
  chunkId: string,
  role: 'player' | 'builder',
  plots: PlotUpdateInput[] | undefined
): void {
  if (!plots || plots.length === 0) {
    return;
  }

  for (const plot of plots) {
    const identifier = plot.plotIdentifier?.trim();
    const targetPlot =
      (plot.plotId && findPlotById(plot.plotId)) ||
      (identifier ? findPlotByIdentifier(realmId, chunkId, identifier) : undefined);

    if (!targetPlot && !plot.plotId && !identifier) {
      throw new HttpError(400, 'plotIdentifier or plotId is required for plot updates');
    }

    if (targetPlot && targetPlot.realmId !== realmId) {
      throw new HttpError(400, 'Plot does not belong to the requested realm');
    }
    if (targetPlot && targetPlot.chunkId !== chunkId) {
      throw new HttpError(400, 'Plot does not belong to the requested chunk');
    }

    if (role !== 'builder') {
      if (!targetPlot || targetPlot.isDeleted) {
        throw new HttpError(403, 'Only builders can create new plots');
      }
      if (targetPlot.ownerUserId !== userId) {
        throw new HttpError(403, 'You do not own this plot');
      }
      if (typeof plot.ownerUserId !== 'undefined' && plot.ownerUserId !== userId && plot.ownerUserId !== null) {
        throw new HttpError(403, 'You cannot transfer plot ownership to another player');
      }
    }
  }
}

export function recordChunkChange(
  userId: string,
  realmId: string,
  chunkId: string,
  changeType: string | undefined,
  chunk?: ChunkUpdateInput,
  structures?: StructureUpdateInput[],
  plots?: PlotUpdateInput[],
  resources?: ResourceDelta[]
): ChunkChangeDTO {
  const { membership } = ensureRealmAccess(userId, realmId);
  if (
    membership.role !== 'builder' &&
    (chunk || (structures && structures.length > 0))
  ) {
    throw new HttpError(403, 'Only builders can modify realm chunks');
  }

  const normalizedResources = normalizeResourceDeltas(resources);

  try {
    const applyChange = db.transaction(() => {
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

      validatePlotOwnership(userId, realmId, chunkRecord.id, membership.role, plots);

      if (normalizedResources.length > 0) {
        applyResourceDeltas(realmId, userId, normalizedResources);
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
          const existingPlot =
            (plot.plotId && findPlotById(plot.plotId)) ||
            findPlotByIdentifier(realmId, chunkRecord!.id, identifier);
          const ownerUserId =
            typeof plot.ownerUserId === 'undefined'
              ? existingPlot?.ownerUserId ?? null
              : plot.ownerUserId ?? null;
          return {
            id: plotId,
            realmId,
            chunkId: chunkRecord!.id,
            plotIdentifier: identifier,
            ownerUserId,
            dataJson: serializePayload(plot.data ?? existingPlot?.dataJson ?? {}),
            isDeleted: Boolean(plot.isDeleted),
          };
        })
      );

      const payload: ChunkChangePayload = {
        chunk: chunkRecord ? toChunkDTO(chunkRecord) : undefined,
        structures: structureRecords.length > 0 ? structureRecords.map(toStructureDTO) : undefined,
        plots: plotRecords.length > 0 ? plotRecords.map(toPlotDTO) : undefined,
        resources: normalizedResources.length > 0 ? normalizedResources : undefined,
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
        resources: payload.resources,
      };

      return changeDto;
    });

    const changeDto = applyChange();
    publishChunkChange(changeDto);
    return changeDto;
  } catch (error) {
    if (error instanceof InsufficientResourceError) {
      throw new HttpError(409, error.message, { retryable: false });
    }
    throw error;
  }
}
