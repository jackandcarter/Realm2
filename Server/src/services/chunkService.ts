import { randomUUID } from 'crypto';
import { DbExecutor, db } from '../db/database';
import { terrainDb } from '../db/terrainDatabase';
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
import { getPlotOwner, getPlotPermissionForUser, upsertPlotOwner } from '../db/plotAccessRepository';
import { listResourceTypeIds } from './referenceDataService';
import { logger } from '../observability/logger';

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

export interface TerrainImportChunkInput {
  chunkId: string;
  chunkX: number;
  chunkZ: number;
  payload?: unknown;
  isDeleted?: boolean;
  structures?: StructureUpdateInput[];
  plots?: PlotUpdateInput[];
}

export interface TerrainImportPayload {
  chunks: TerrainImportChunkInput[];
  emitChangeLog?: boolean;
  changeType?: string;
}

interface ChunkChangePayload {
  chunk?: RealmChunkDTO | undefined;
  structures?: ChunkStructureDTO[] | undefined;
  plots?: ChunkPlotDTO[] | undefined;
  resources?: ResourceDeltaDTO[] | undefined;
}

const SUPPORTED_TERRAIN_PAYLOAD_VERSIONS = new Set([1]);

async function ensureRealmAccess(userId: string, realmId: string) {
  const realm = await findRealmById(realmId);
  if (!realm) {
    throw new HttpError(404, 'Realm not found');
  }
  const membership = await findMembership(userId, realmId);
  if (!membership) {
    throw new HttpError(403, 'Join the realm before accessing its terrain data');
  }
  return { realm, membership };
}

export async function assertChunkAccess(userId: string, realmId: string): Promise<void> {
  await ensureRealmAccess(userId, realmId);
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

function parsePayloadObject(payload: unknown): Record<string, any> | null {
  if (!payload) {
    return null;
  }
  if (typeof payload === 'string') {
    try {
      const parsed = JSON.parse(payload) as Record<string, any>;
      return parsed && typeof parsed === 'object' ? parsed : null;
    } catch (_error) {
      return null;
    }
  }
  if (typeof payload === 'object') {
    return payload as Record<string, any>;
  }
  return null;
}

function isVector3(value: unknown): value is { x: number; y: number; z: number } {
  if (!value || typeof value !== 'object') {
    return false;
  }
  const typed = value as { x?: unknown; y?: unknown; z?: unknown };
  return (
    typeof typed.x === 'number' &&
    Number.isFinite(typed.x) &&
    typeof typed.y === 'number' &&
    Number.isFinite(typed.y) &&
    typeof typed.z === 'number' &&
    Number.isFinite(typed.z)
  );
}

function validateTerrainPayload(chunkId: string, chunkX: number, chunkZ: number, payload: unknown): void {
  const parsed = parsePayloadObject(payload);
  if (!parsed) {
    throw new HttpError(400, `Chunk ${chunkId} payload must be a JSON object`);
  }

  const payloadVersion = parsed.payloadVersion;
  if (typeof payloadVersion !== 'number' || !SUPPORTED_TERRAIN_PAYLOAD_VERSIONS.has(payloadVersion)) {
    throw new HttpError(400, `Chunk ${chunkId} payloadVersion is missing or unsupported`);
  }

  const regionId = parsed.regionId;
  if (typeof regionId !== 'string' || regionId.trim().length === 0) {
    throw new HttpError(400, `Chunk ${chunkId} regionId is required`);
  }

  const chunkPosition = parsed.chunkPosition;
  if (!isVector3(chunkPosition)) {
    throw new HttpError(400, `Chunk ${chunkId} chunkPosition is invalid`);
  }
  if (chunkPosition.x !== chunkX || chunkPosition.z !== chunkZ) {
    throw new HttpError(400, `Chunk ${chunkId} chunkPosition does not match chunk coords`);
  }

  const digger = parsed.digger;
  if (!digger || typeof digger !== 'object') {
    throw new HttpError(400, `Chunk ${chunkId} digger payload is required`);
  }
  const diggerVersion = (digger as { diggerVersion?: unknown }).diggerVersion;
  if (typeof diggerVersion !== 'number' || !Number.isFinite(diggerVersion)) {
    throw new HttpError(400, `Chunk ${chunkId} diggerVersion is invalid`);
  }
  const diggerDataVersion = (digger as { diggerDataVersion?: unknown }).diggerDataVersion;
  if (typeof diggerDataVersion !== 'string' || diggerDataVersion.trim().length === 0) {
    throw new HttpError(400, `Chunk ${chunkId} diggerDataVersion is invalid`);
  }
  const sizeVox = (digger as { sizeVox?: unknown }).sizeVox;
  if (typeof sizeVox !== 'number' || !Number.isFinite(sizeVox) || sizeVox <= 0) {
    throw new HttpError(400, `Chunk ${chunkId} sizeVox is invalid`);
  }
  const heightmapScale = (digger as { heightmapScale?: unknown }).heightmapScale;
  if (!isVector3(heightmapScale)) {
    throw new HttpError(400, `Chunk ${chunkId} heightmapScale is invalid`);
  }
  const voxelData = (digger as { voxelData?: unknown }).voxelData;
  if (typeof voxelData !== 'string' || voxelData.trim().length === 0) {
    throw new HttpError(400, `Chunk ${chunkId} voxelData is required`);
  }
  const voxelMetadata = (digger as { voxelMetadata?: unknown }).voxelMetadata;
  if (typeof voxelMetadata !== 'undefined' && typeof voxelMetadata !== 'string') {
    throw new HttpError(400, `Chunk ${chunkId} voxelMetadata must be a string when present`);
  }
}

function isImmutableBase(payload: unknown): boolean {
  const parsed = parsePayloadObject(payload);
  if (!parsed) {
    return false;
  }
  const terrainLayer = typeof parsed.terrainLayer === 'string' ? parsed.terrainLayer.trim().toLowerCase() : '';
  return terrainLayer === 'base' || parsed.immutableBase === true;
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

export async function getRealmChunkSnapshot(
  userId: string,
  realmId: string,
  updatedAfter?: string
): Promise<ChunkSnapshotEnvelope> {
  await ensureRealmAccess(userId, realmId);
  const chunkRecords = (await listChunksByRealm(realmId, updatedAfter)).filter(
    (chunk) => !chunk.isDeleted
  );
  const chunkIds = chunkRecords.map((chunk) => chunk.id);
  const structures = await listStructuresForChunks(chunkIds);
  const plots = await listPlotsForChunks(chunkIds);
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

export async function getChunkChangeFeed(
  userId: string,
  realmId: string,
  createdAfter?: string,
  limit?: number
): Promise<ChunkChangeEnvelope> {
  await ensureRealmAccess(userId, realmId);
  const changeRecords = await listChunkChanges(realmId, createdAfter, limit ?? 500);
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

async function normalizeResourceDeltas(deltas: ResourceDelta[] | undefined): Promise<ResourceDeltaDTO[]> {
  const normalized: ResourceDeltaDTO[] = [];
  const resourceTypeIds = await listResourceTypeIds();
  for (const delta of deltas ?? []) {
    if (!delta) {
      continue;
    }
    const resourceType = delta.resourceType?.trim();
    if (!resourceType || !resourceTypeIds.includes(resourceType)) {
      throw new HttpError(
        400,
        `resourceType must be one of: ${resourceTypeIds.join(', ')}`
      );
    }
    if (!Number.isFinite(delta.quantity) || delta.quantity === 0) {
      continue;
    }
    normalized.push({ resourceType, quantity: delta.quantity });
  }
  return normalized;
}

async function validatePlotOwnership(
  userId: string,
  realmId: string,
  chunkId: string,
  role: 'player' | 'builder',
  plots: PlotUpdateInput[] | undefined,
  executor: DbExecutor = terrainDb
): Promise<void> {
  if (!plots || plots.length === 0) {
    return;
  }

  for (const plot of plots) {
    const identifier = plot.plotIdentifier?.trim();
    const targetPlot =
      (plot.plotId && (await findPlotById(plot.plotId, executor))) ||
      (identifier ? await findPlotByIdentifier(realmId, chunkId, identifier, executor) : undefined);

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
      const ownerRecord = await getPlotOwner(targetPlot.id, executor);
      const ownerUserId = ownerRecord?.ownerUserId ?? targetPlot.ownerUserId;
      const permission = await getPlotPermissionForUser(targetPlot.id, userId, executor);
      const hasAccess = ownerUserId === userId || Boolean(permission);
      if (!hasAccess) {
        throw new HttpError(403, 'You do not own this plot');
      }
      if (
        typeof plot.ownerUserId !== 'undefined' &&
        plot.ownerUserId !== userId &&
        plot.ownerUserId !== null
      ) {
        throw new HttpError(403, 'You cannot transfer plot ownership to another player');
      }
    }
  }
}

export async function recordChunkChange(
  userId: string,
  realmId: string,
  chunkId: string,
  changeType: string | undefined,
  chunk?: ChunkUpdateInput,
  structures?: StructureUpdateInput[],
  plots?: PlotUpdateInput[],
  resources?: ResourceDelta[]
): Promise<ChunkChangeDTO> {
  const { membership } = await ensureRealmAccess(userId, realmId);
  if (
    membership.role !== 'builder' &&
    (chunk || (structures && structures.length > 0))
  ) {
    throw new HttpError(403, 'Only builders can modify realm chunks');
  }

  const normalizedResources = await normalizeResourceDeltas(resources);
  let resourcesApplied = false;

  try {
    if (normalizedResources.length > 0) {
      await db.withTransaction(async (tx) => {
        await applyResourceDeltas(realmId, userId, normalizedResources, tx);
      });
      resourcesApplied = true;
    }

    const changeDto = await terrainDb.withTransaction(async (tx) => {
      const existingChunk = await findChunkById(chunkId, tx);
      if (!existingChunk && !chunk) {
        throw new HttpError(400, 'Chunk metadata must be supplied for new chunks');
      }
      if (existingChunk && existingChunk.realmId !== realmId) {
        throw new HttpError(403, 'Chunk does not belong to the requested realm');
      }

      const existingIsBase = isImmutableBase(existingChunk?.payloadJson);
      const incomingIsBase = chunk?.payload !== undefined && isImmutableBase(chunk.payload);
      if (existingIsBase || incomingIsBase) {
        if (chunk?.payload !== undefined || chunk?.isDeleted) {
          throw new HttpError(403, 'Base terrain is immutable and cannot be modified');
        }
      }

      let chunkRecord = existingChunk;
      if (chunk) {
        const chunkX = chunk.chunkX ?? chunkRecord?.chunkX;
        const chunkZ = chunk.chunkZ ?? chunkRecord?.chunkZ;
        if (typeof chunkX !== 'number' || typeof chunkZ !== 'number') {
          throw new HttpError(400, 'Chunk coordinates must be provided');
        }
        chunkRecord = await upsertChunk(
          {
          id: chunkId,
          realmId,
          chunkX,
          chunkZ,
          payloadJson: serializePayload(chunk.payload),
          isDeleted: Boolean(chunk.isDeleted),
          },
          tx
        );
      }

      if (!chunkRecord) {
        throw new HttpError(500, 'Failed to load chunk after update');
      }

      await validatePlotOwnership(userId, realmId, chunkRecord.id, membership.role, plots, tx);

      const structureRecords = await upsertStructures(
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
        }),
        tx
      );

      const plotInputs = await Promise.all(
        (plots ?? []).map(async (plot) => {
          const plotId = plot.plotId ?? randomUUID();
          const identifier = plot.plotIdentifier ?? plotId;
          const existingPlot =
            (plot.plotId && (await findPlotById(plot.plotId, tx))) ||
            (await findPlotByIdentifier(realmId, chunkRecord!.id, identifier, tx));
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
        }),
      );
      const plotRecords = await upsertPlots(plotInputs, tx);

      for (const plot of plotRecords) {
        await upsertPlotOwner(
          plot.id,
          realmId,
          plot.isDeleted ? null : plot.ownerUserId ?? null,
          tx
        );
      }

      const payload: ChunkChangePayload = {
        chunk: chunkRecord ? toChunkDTO(chunkRecord) : undefined,
        structures: structureRecords.length > 0 ? structureRecords.map(toStructureDTO) : undefined,
        plots: plotRecords.length > 0 ? plotRecords.map(toPlotDTO) : undefined,
        resources: normalizedResources.length > 0 ? normalizedResources : undefined,
      };

      const change: ChunkChangeRecord = await logChunkChange(
        realmId,
        chunkRecord.id,
        changeType ?? 'chunk:update',
        JSON.stringify(payload),
        tx
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
    publishChunkChange(changeDto);
    return changeDto;
  } catch (error) {
    if (resourcesApplied) {
      try {
        await db.withTransaction(async (tx) => {
          const reversal = normalizedResources.map((delta) => ({
            resourceType: delta.resourceType,
            quantity: -delta.quantity,
          }));
          await applyResourceDeltas(realmId, userId, reversal, tx);
        });
      } catch (revertError) {
        logger.warn(
          { err: revertError, realmId, chunkId },
          'Failed to revert resource adjustments after terrain error'
        );
      }
    }
    if (error instanceof InsufficientResourceError) {
      throw new HttpError(409, error.message, { retryable: false });
    }
    throw error;
  }
}

export async function importTerrainSnapshot(
  userId: string,
  realmId: string,
  payload: TerrainImportPayload
): Promise<ChunkChangeDTO[]> {
  const { membership } = await ensureRealmAccess(userId, realmId);
  if (membership.role !== 'builder') {
    throw new HttpError(403, 'Only builders can import terrain snapshots');
  }

  if (!payload || !Array.isArray(payload.chunks) || payload.chunks.length === 0) {
    throw new HttpError(400, 'chunks array is required for terrain import');
  }

  const changeType = payload.changeType ?? 'terrain:import';
  const emitChangeLog = Boolean(payload.emitChangeLog);
  const changes: ChunkChangeDTO[] = [];
  let skippedChunks = 0;

  await terrainDb.withTransaction(async (tx) => {
    for (const chunkInput of payload.chunks) {
      if (!chunkInput?.chunkId) {
        throw new HttpError(400, 'chunkId is required for terrain import');
      }
      if (!Number.isFinite(chunkInput.chunkX) || !Number.isFinite(chunkInput.chunkZ)) {
        throw new HttpError(400, 'chunkX and chunkZ are required for terrain import');
      }

      validateTerrainPayload(chunkInput.chunkId, chunkInput.chunkX, chunkInput.chunkZ, chunkInput.payload);

      const payloadJson = serializePayload(chunkInput.payload);
      const hasStructures = (chunkInput.structures ?? []).length > 0;
      const hasPlots = (chunkInput.plots ?? []).length > 0;
      const existingChunk = await findChunkById(chunkInput.chunkId, tx);
      if (
        existingChunk &&
        existingChunk.payloadJson === payloadJson &&
        existingChunk.isDeleted === Boolean(chunkInput.isDeleted) &&
        !hasStructures &&
        !hasPlots
      ) {
        skippedChunks += 1;
        continue;
      }

      const chunkRecord = await upsertChunk(
        {
          id: chunkInput.chunkId,
          realmId,
          chunkX: chunkInput.chunkX,
          chunkZ: chunkInput.chunkZ,
          payloadJson,
          isDeleted: Boolean(chunkInput.isDeleted),
        },
        tx
      );

      const structureRecords = await upsertStructures(
        (chunkInput.structures ?? []).map((structure) => {
          if (!structure.structureType) {
            throw new HttpError(400, 'Structure type is required');
          }
          return {
            id: structure.structureId ?? randomUUID(),
            realmId,
            chunkId: chunkRecord.id,
            structureType: structure.structureType,
            dataJson: serializePayload(structure.data),
            isDeleted: Boolean(structure.isDeleted),
          };
        }),
        tx
      );

      const plotInputs = await Promise.all(
        (chunkInput.plots ?? []).map(async (plot) => {
          const plotId = plot.plotId ?? randomUUID();
          const identifier = plot.plotIdentifier ?? plotId;
          const existingPlot =
            (plot.plotId && (await findPlotById(plot.plotId, tx))) ||
            (await findPlotByIdentifier(realmId, chunkRecord.id, identifier, tx));
          const ownerUserId =
            typeof plot.ownerUserId === 'undefined'
              ? existingPlot?.ownerUserId ?? null
              : plot.ownerUserId ?? null;
          return {
            id: plotId,
            realmId,
            chunkId: chunkRecord.id,
            plotIdentifier: identifier,
            ownerUserId,
            dataJson: serializePayload(plot.data ?? existingPlot?.dataJson ?? {}),
            isDeleted: Boolean(plot.isDeleted),
          };
        })
      );
      const plotRecords = await upsertPlots(plotInputs, tx);

      for (const plot of plotRecords) {
        await upsertPlotOwner(
          plot.id,
          realmId,
          plot.isDeleted ? null : plot.ownerUserId ?? null,
          tx
        );
      }

      const changePayload: ChunkChangePayload = {
        chunk: toChunkDTO(chunkRecord),
        structures: structureRecords.length > 0 ? structureRecords.map(toStructureDTO) : undefined,
        plots: plotRecords.length > 0 ? plotRecords.map(toPlotDTO) : undefined,
      };

      if (emitChangeLog) {
        const change = await logChunkChange(
          realmId,
          chunkRecord.id,
          changeType,
          JSON.stringify(changePayload),
          tx
        );
        changes.push({
          changeId: change.id,
          realmId: change.realmId,
          chunkId: change.chunkId,
          changeType: change.changeType,
          createdAt: change.createdAt,
          chunk: changePayload.chunk,
          structures: changePayload.structures,
          plots: changePayload.plots,
        });
      }
    }
  });

  if (emitChangeLog) {
    for (const change of changes) {
      publishChunkChange(change);
    }
  }

  if (skippedChunks > 0) {
    logger.info({ realmId, skippedChunks }, 'Skipped unchanged terrain chunks during import.');
  }

  return changes;
}
