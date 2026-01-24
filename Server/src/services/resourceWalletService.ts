import { findMembership } from '../db/realmMembershipRepository';
import { findRealmById } from '../db/realmRepository';
import {
  applyResourceAdjustments,
  listWalletEntries,
  ResourceAdjustment,
  InsufficientResourceError,
} from '../db/resourceWalletRepository';
import { HttpError } from '../utils/errors';
import { listResourceTypeIds } from './referenceDataService';

export async function listWalletForUser(userId: string, realmId: string) {
  const realm = await findRealmById(realmId);
  if (!realm) {
    throw new HttpError(404, 'Realm not found');
  }

  const membership = await findMembership(userId, realmId);
  if (!membership) {
    throw new HttpError(403, 'Join the realm before accessing its resources');
  }

  return listWalletEntries(realmId, userId);
}

export async function applyWalletAdjustmentsForUser(
  userId: string,
  realmId: string,
  adjustments: ResourceAdjustment[]
) {
  const realm = await findRealmById(realmId);
  if (!realm) {
    throw new HttpError(404, 'Realm not found');
  }

  const membership = await findMembership(userId, realmId);
  if (!membership) {
    throw new HttpError(403, 'Join the realm before accessing its resources');
  }

  const resourceTypeIds = await listResourceTypeIds();
  for (const adjustment of adjustments ?? []) {
    const resourceType = adjustment?.resourceType?.trim();
    if (!resourceType || !resourceTypeIds.includes(resourceType)) {
      throw new HttpError(
        400,
        `resourceType must be one of: ${resourceTypeIds.join(', ')}`
      );
    }
  }

  try {
    return await applyResourceAdjustments(realmId, userId, adjustments ?? []);
  } catch (error) {
    if (error instanceof InsufficientResourceError) {
      throw new HttpError(409, error.message, { retryable: false });
    }
    throw error;
  }
}
