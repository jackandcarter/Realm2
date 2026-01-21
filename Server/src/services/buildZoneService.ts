import { findMembership } from '../db/realmMembershipRepository';
import { findRealmById } from '../db/realmRepository';
import { listBuildZones } from '../db/buildZoneRepository';
import { HttpError } from '../utils/errors';

export interface BuildZoneBoundsInput {
  center: { x: number; y: number; z: number };
  size: { x: number; y: number; z: number };
}

export interface BuildZoneValidationResult {
  isValid: boolean;
  zoneId?: string;
  failureReason?: string;
}

function isBoundsInside(container: BuildZoneBoundsInput, candidate: BuildZoneBoundsInput): boolean {
  const containerMin = {
    x: container.center.x - container.size.x * 0.5,
    y: container.center.y - container.size.y * 0.5,
    z: container.center.z - container.size.z * 0.5,
  };
  const containerMax = {
    x: container.center.x + container.size.x * 0.5,
    y: container.center.y + container.size.y * 0.5,
    z: container.center.z + container.size.z * 0.5,
  };
  const candidateMin = {
    x: candidate.center.x - candidate.size.x * 0.5,
    y: candidate.center.y - candidate.size.y * 0.5,
    z: candidate.center.z - candidate.size.z * 0.5,
  };
  const candidateMax = {
    x: candidate.center.x + candidate.size.x * 0.5,
    y: candidate.center.y + candidate.size.y * 0.5,
    z: candidate.center.z + candidate.size.z * 0.5,
  };

  return (
    candidateMin.x >= containerMin.x &&
    candidateMax.x <= containerMax.x &&
    candidateMin.y >= containerMin.y &&
    candidateMax.y <= containerMax.y &&
    candidateMin.z >= containerMin.z &&
    candidateMax.z <= containerMax.z
  );
}

export async function validateBuildZoneForUser(
  userId: string,
  realmId: string,
  bounds: BuildZoneBoundsInput
): Promise<BuildZoneValidationResult> {
  const realm = await findRealmById(realmId);
  if (!realm) {
    throw new HttpError(404, 'Realm not found');
  }

  const membership = await findMembership(userId, realmId);
  if (!membership) {
    throw new HttpError(403, 'Join the realm before accessing build zones');
  }

  const zones = await listBuildZones(realmId);
  if (!zones || zones.length === 0) {
    return { isValid: true };
  }

  for (const zone of zones) {
    const zoneBounds: BuildZoneBoundsInput = {
      center: { x: zone.centerX, y: zone.centerY, z: zone.centerZ },
      size: { x: zone.sizeX, y: zone.sizeY, z: zone.sizeZ },
    };

    if (isBoundsInside(zoneBounds, bounds)) {
      return { isValid: true, zoneId: zone.id };
    }
  }

  return {
    isValid: false,
    failureReason: 'Target plot must be inside an approved build zone.',
  };
}
