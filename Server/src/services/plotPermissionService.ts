import { findPlotById } from '../db/chunkRepository';
import { findMembership } from '../db/realmMembershipRepository';
import { findRealmById } from '../db/realmRepository';
import {
  getPlotOwner,
  listPlotPermissions,
  replacePlotPermissions,
  upsertPlotOwner,
} from '../db/plotAccessRepository';
import { HttpError } from '../utils/errors';

export interface PlotPermissionInput {
  userId: string;
  permission: string;
}

export interface PlotPermissionSnapshot {
  plotId: string;
  ownerUserId: string | null;
  permissions: PlotPermissionInput[];
}

async function ensurePlotAccess(userId: string, realmId: string, plotId: string) {
  const realm = await findRealmById(realmId);
  if (!realm) {
    throw new HttpError(404, 'Realm not found');
  }

  const membership = await findMembership(userId, realmId);
  if (!membership) {
    throw new HttpError(403, 'Join the realm before accessing plots');
  }

  const plot = await findPlotById(plotId);
  if (!plot || plot.realmId !== realmId) {
    throw new HttpError(404, 'Plot not found');
  }

  return { membership, plot };
}

async function resolveOwnerUserId(plotId: string, fallback?: string | null) {
  const owner = await getPlotOwner(plotId);
  return owner?.ownerUserId ?? fallback ?? null;
}

export async function getPlotPermissionsForUser(
  userId: string,
  realmId: string,
  plotId: string
): Promise<PlotPermissionSnapshot> {
  const { plot } = await ensurePlotAccess(userId, realmId, plotId);
  const ownerUserId = await resolveOwnerUserId(plotId, plot.ownerUserId);
  const permissions = await listPlotPermissions(plotId);

  return {
    plotId,
    ownerUserId,
    permissions: permissions.map((entry) => ({
      userId: entry.userId,
      permission: entry.permission,
    })),
  };
}

export async function replacePlotPermissionsForUser(
  userId: string,
  realmId: string,
  plotId: string,
  permissions: PlotPermissionInput[]
): Promise<PlotPermissionSnapshot> {
  const { membership, plot } = await ensurePlotAccess(userId, realmId, plotId);

  const ownerUserId = await resolveOwnerUserId(plotId, plot.ownerUserId);
  if (membership.role !== 'builder' && ownerUserId !== userId) {
    throw new HttpError(403, 'Only plot owners can update permissions');
  }

  if (ownerUserId && ownerUserId !== plot.ownerUserId) {
    await upsertPlotOwner(plotId, realmId, ownerUserId);
  }

  const sanitized = permissions
    .map((entry) => ({
      userId: entry.userId?.trim(),
      permission: entry.permission?.trim(),
    }))
    .filter((entry) => entry.userId && entry.permission);

  const updated = await replacePlotPermissions(plotId, realmId, sanitized as PlotPermissionInput[]);

  return {
    plotId,
    ownerUserId,
    permissions: updated.map((entry) => ({
      userId: entry.userId,
      permission: entry.permission,
    })),
  };
}
