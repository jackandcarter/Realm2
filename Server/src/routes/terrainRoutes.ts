import { Router } from 'express';
import { requireAuth } from '../middleware/authMiddleware';
import { HttpError } from '../utils/errors';
import {
  importTerrainSnapshot,
  TerrainImportPayload,
} from '../services/chunkService';
import {
  listTerrainRegionsForRealm,
  upsertTerrainRegionForRealm,
} from '../services/terrainRegionService';

export const terrainRouter = Router({ mergeParams: true });

terrainRouter.get('/regions', requireAuth, async (req, res, next) => {
  try {
    const { realmId } = req.params as { realmId: string };
    const regions = await listTerrainRegionsForRealm(req.user!.id, realmId);
    res.json({ regions });
  } catch (error) {
    next(error);
  }
});

terrainRouter.put('/regions/:regionId', requireAuth, async (req, res, next) => {
  try {
    const { realmId, regionId } = req.params as { realmId: string; regionId: string };
    const { name, bounds, terrainCount, payload } = req.body ?? {};
    const region = await upsertTerrainRegionForRealm(req.user!.id, realmId, {
      regionId,
      name,
      bounds,
      terrainCount,
      payload,
    });
    res.json({ region });
  } catch (error) {
    next(error);
  }
});

terrainRouter.post('/import', requireAuth, async (req, res, next) => {
  try {
    const { realmId } = req.params as { realmId: string };
    const importPayload = req.body as TerrainImportPayload;
    if (!importPayload) {
      throw new HttpError(400, 'import payload is required');
    }
    const changes = await importTerrainSnapshot(req.user!.id, realmId, importPayload);
    res.status(201).json({ changes });
  } catch (error) {
    next(error);
  }
});
