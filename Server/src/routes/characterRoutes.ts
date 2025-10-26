import { Router } from 'express';
import { requireAuth } from '../middleware/authMiddleware';
import {
  createCharacterForUser,
  CreateCharacterInput,
} from '../services/characterService';
import { HttpError, isHttpError } from '../utils/errors';

export const characterRouter = Router();

/**
 * @openapi
 * /characters:
 *   post:
 *     summary: Create a new character in the selected realm
 *     tags:
 *       - Characters
 *     security:
 *       - bearerAuth: []
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             $ref: '#/components/schemas/CreateCharacterRequest'
 *     responses:
 *       '201':
 *         description: Character successfully created
 *         content:
 *           application/json:
 *             schema:
 *               $ref: '#/components/schemas/CreateCharacterResponse'
 *       '400':
 *         description: Invalid payload
 *       '401':
 *         description: Authentication required
 *       '409':
 *         description: Character name already exists in the realm
 */
characterRouter.post('/', requireAuth, (req, res, next) => {
  try {
    const payload = toCreateCharacterInput(req.body);
    const character = createCharacterForUser(req.user!.id, payload);
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

function toCreateCharacterInput(body: unknown): CreateCharacterInput {
  if (!body || typeof body !== 'object') {
    return {};
  }

  const value = body as Record<string, unknown>;

  return {
    realmId: typeof value.realmId === 'string' ? value.realmId : undefined,
    name: typeof value.name === 'string' ? value.name : undefined,
    bio: typeof value.bio === 'string' ? value.bio : undefined,
    raceId: typeof value.raceId === 'string' ? value.raceId : undefined,
    appearance: value.appearance as CreateCharacterInput['appearance'],
  };
}
