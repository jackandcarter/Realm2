import { Router } from 'express';
import { requireAuth } from '../middleware/authMiddleware';
import { listRealmsForUser, getRealmCharacters } from '../services/realmService';
import { HttpError, isHttpError } from '../utils/errors';

export const realmRouter = Router();

realmRouter.get('/', requireAuth, async (req, res, next) => {
  try {
    const realms = await listRealmsForUser(req.user!.id);
    res.json({ realms });
  } catch (error) {
    next(error);
  }
});

realmRouter.get('/:realmId/characters', requireAuth, async (req, res, next) => {
  try {
    const { realmId } = req.params as { realmId: string };
    const result = await getRealmCharacters(req.user!.id, realmId);
    res.json({
      realm: {
        id: result.realm.id,
        name: result.realm.name,
        narrative: result.realm.narrative,
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
