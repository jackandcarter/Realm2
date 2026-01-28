import { Router } from 'express';
import { requireAuth } from '../middleware/authMiddleware';
import { listRealmsForUser, getRealmCharacters, selectRealmForUser } from '../services/realmService';
import { HttpError, isHttpError } from '../utils/errors';
import { replaceBuildZonesForRealm, validateBuildZoneForUser } from '../services/buildZoneService';
import { applyWalletAdjustmentsForUser, listWalletForUser } from '../services/resourceWalletService';
import { getPlotPermissionsForUser, replacePlotPermissionsForUser } from '../services/plotPermissionService';
import { resolveRealmHosting } from '../config/realmHosting';

export const realmRouter = Router();

realmRouter.get('/', requireAuth, async (req, res, next) => {
  try {
    const realms = await listRealmsForUser(req.user!.id);
    res.json({ realms });
  } catch (error) {
    next(error);
  }
});

realmRouter.post('/select', requireAuth, async (req, res, next) => {
  try {
    const realmId = typeof req.body?.realmId === 'string' ? req.body.realmId.trim() : '';
    if (!realmId) {
      throw new HttpError(400, 'realmId is required');
    }
    const realm = await selectRealmForUser(req.user!.id, realmId);
    res.status(200).json({ realmId: realm.id });
  } catch (error) {
    next(error);
  }
});

realmRouter.get('/:realmId/characters', requireAuth, async (req, res, next) => {
  try {
    const { realmId } = req.params as { realmId: string };
    const result = await getRealmCharacters(req.user!.id, realmId);
    const hosting = resolveRealmHosting(result.realm.id);
    res.json({
      realm: {
        id: result.realm.id,
        name: result.realm.name,
        narrative: result.realm.narrative,
        worldSceneName: hosting.worldSceneName,
        worldServiceUrl: hosting.worldServiceUrl,
      },
      membership: {
        realmId: result.membership.realmId,
        role: result.membership.role,
      },
      characters: result.characters,
    });
  } catch (error) {
    if (isHttpError(error)) {
      next(error);
      return;
    }
    next(new HttpError(500, 'Failed to load characters'));
  }
});

realmRouter.post('/:realmId/build-zones/validate', requireAuth, async (req, res, next) => {
  try {
    const { realmId } = req.params as { realmId: string };
    const bounds = toBuildZoneBounds(req.body?.bounds);
    const result = await validateBuildZoneForUser(req.user!.id, realmId, bounds);
    res.json(result);
  } catch (error) {
    next(error);
  }
});

realmRouter.put('/:realmId/build-zones', requireAuth, async (req, res, next) => {
  try {
    const { realmId } = req.params as { realmId: string };
    const zones = toBuildZoneDefinitions(req.body?.zones);
    const updated = await replaceBuildZonesForRealm(req.user!.id, realmId, zones);
    res.json({ zones: updated });
  } catch (error) {
    next(error);
  }
});

realmRouter.get('/:realmId/resources/wallet', requireAuth, async (req, res, next) => {
  try {
    const { realmId } = req.params as { realmId: string };
    const wallet = await listWalletForUser(req.user!.id, realmId);
    res.json({ wallet });
  } catch (error) {
    next(error);
  }
});

realmRouter.post('/:realmId/resources/wallet/adjustments', requireAuth, async (req, res, next) => {
  try {
    const { realmId } = req.params as { realmId: string };
    const adjustments = req.body?.adjustments;
    if (!Array.isArray(adjustments)) {
      throw new HttpError(400, 'adjustments must be an array');
    }
    const wallet = await applyWalletAdjustmentsForUser(req.user!.id, realmId, adjustments);
    res.json({ wallet });
  } catch (error) {
    next(error);
  }
});

realmRouter.get('/:realmId/plots/:plotId/permissions', requireAuth, async (req, res, next) => {
  try {
    const { realmId, plotId } = req.params as { realmId: string; plotId: string };
    const snapshot = await getPlotPermissionsForUser(req.user!.id, realmId, plotId);
    res.json(snapshot);
  } catch (error) {
    next(error);
  }
});

realmRouter.put('/:realmId/plots/:plotId/permissions', requireAuth, async (req, res, next) => {
  try {
    const { realmId, plotId } = req.params as { realmId: string; plotId: string };
    const permissions = req.body?.permissions;
    if (!Array.isArray(permissions)) {
      throw new HttpError(400, 'permissions must be an array');
    }
    const snapshot = await replacePlotPermissionsForUser(
      req.user!.id,
      realmId,
      plotId,
      permissions
    );
    res.json(snapshot);
  } catch (error) {
    next(error);
  }
});

function toBuildZoneBounds(value: unknown): { center: { x: number; y: number; z: number }; size: { x: number; y: number; z: number } } {
  if (!value || typeof value !== 'object') {
    throw new HttpError(400, 'bounds are required for validation');
  }

  const record = value as Record<string, any>;
  const center = record.center;
  const size = record.size;
  if (!center || !size) {
    throw new HttpError(400, 'bounds.center and bounds.size are required');
  }

  const centerX = Number(center.x);
  const centerY = Number(center.y);
  const centerZ = Number(center.z);
  const sizeX = Number(size.x);
  const sizeY = Number(size.y);
  const sizeZ = Number(size.z);

  if (![centerX, centerY, centerZ, sizeX, sizeY, sizeZ].every(Number.isFinite)) {
    throw new HttpError(400, 'bounds must include numeric center and size values');
  }

  return {
    center: { x: centerX, y: centerY, z: centerZ },
    size: { x: sizeX, y: sizeY, z: sizeZ },
  };
}

function toBuildZoneDefinitions(value: unknown): { zoneId?: string; label?: string | null; bounds: { center: { x: number; y: number; z: number }; size: { x: number; y: number; z: number } } }[] {
  if (!Array.isArray(value)) {
    throw new HttpError(400, 'zones must be an array');
  }

  return value.map((entry) => {
    if (!entry || typeof entry !== 'object') {
      throw new HttpError(400, 'each build zone must be an object');
    }
    const record = entry as Record<string, any>;
    return {
      zoneId: typeof record.zoneId === 'string' ? record.zoneId : undefined,
      label: typeof record.label === 'string' ? record.label : record.label ?? null,
      bounds: toBuildZoneBounds(record.bounds),
    };
  });
}
