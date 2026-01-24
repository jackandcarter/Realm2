import { findMembership } from '../db/realmMembershipRepository';
import { findRealmById } from '../db/realmRepository';
import {
  listTerrainRegionsByRealm,
  upsertTerrainRegion,
} from '../db/terrainRegionRepository';
import { HttpError } from '../utils/errors';

export interface TerrainRegionInput {
  regionId?: string;
  name: string;
  bounds: unknown;
  terrainCount?: number;
  payload?: unknown;
}

function serializeJson(value: unknown, label: string): string {
  if (typeof value === 'string') {
    return value;
  }
  try {
    return JSON.stringify(value ?? {});
  } catch (_error) {
    throw new HttpError(400, `${label} must be JSON-serializable`);
  }
}

async function ensureRegionAccess(userId: string, realmId: string) {
  const realm = await findRealmById(realmId);
  if (!realm) {
    throw new HttpError(404, 'Realm not found');
  }
  const membership = await findMembership(userId, realmId);
  if (!membership) {
    throw new HttpError(403, 'Join the realm before accessing terrain regions');
  }
  return { membership };
}

export async function listTerrainRegionsForRealm(userId: string, realmId: string) {
  await ensureRegionAccess(userId, realmId);
  return listTerrainRegionsByRealm(realmId);
}

export async function upsertTerrainRegionForRealm(
  userId: string,
  realmId: string,
  input: TerrainRegionInput
) {
  const { membership } = await ensureRegionAccess(userId, realmId);
  if (membership.role !== 'builder') {
    throw new HttpError(403, 'Only builders can edit terrain regions');
  }
  const name = input.name?.trim();
  if (!name) {
    throw new HttpError(400, 'region name is required');
  }
  const boundsJson = serializeJson(input.bounds, 'bounds');
  const payloadJson = serializeJson(input.payload, 'payload');
  return upsertTerrainRegion({
    id: input.regionId,
    realmId,
    name,
    boundsJson,
    terrainCount: input.terrainCount ?? 0,
    payloadJson,
  });
}
