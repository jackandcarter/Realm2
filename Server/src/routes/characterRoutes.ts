import { Router } from 'express';
import { requireAuth } from '../middleware/authMiddleware';
import { createCharacterForUser } from '../services/characterService';
import { HttpError, isHttpError } from '../utils/errors';

export const characterRouter = Router();

characterRouter.post('/', requireAuth, (req, res, next) => {
  try {
    const character = createCharacterForUser(req.user!.id, req.body);
    res.status(201).json({ character });
  } catch (error) {
    if (isHttpError(error)) {
      next(error);
      return;
    }
    next(new HttpError(500, 'Failed to create character'));
  }
});

export { characterRouter as charactersRouter };
