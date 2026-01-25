import { NextFunction, Router } from 'express';
import { requireAuth } from '../middleware/authMiddleware';
import {
  createCharacterForUser,
  CreateCharacterInput,
  selectCharacterForUser,
} from '../services/characterService';
import {
  getCharacterProgressionForUser,
  ProgressionUpdateInput,
  submitProgressionIntentForUser,
  submitQuestCompletionForUser,
  QuestCompletionInput,
} from '../services/progressionService';
import {
  BuildStateUpdateInput,
  getBuildStateForUser,
  replaceBuildStateForUser,
} from '../services/buildStateService';
import { getDockLayoutForUser, replaceDockLayoutForUser } from '../services/dockLayoutService';
import { getMapPinsForUser, MapPinUpdateInput, replaceMapPinsForUser } from '../services/mapPinService';
import { VersionConflictError } from '../db/progressionRepository';
import { CharacterClassState } from '../types/classUnlocks';
import { HttpError, isHttpError } from '../utils/errors';
import { JsonValue, isJsonValue } from '../types/characterCustomization';
import { EquipmentSlot, equipmentSlots } from '../config/gameEnums';

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
characterRouter.post('/', requireAuth, async (req, res, next) => {
  try {
    const payload = toCreateCharacterInput(req.body);
    const character = await createCharacterForUser(req.user!.id, payload);
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

characterRouter.post('/select', requireAuth, async (req, res, next) => {
  try {
    const characterId = typeof req.body?.characterId === 'string' ? req.body.characterId.trim() : '';
    if (!characterId) {
      throw new HttpError(400, 'characterId is required');
    }
    await selectCharacterForUser(req.user!.id, characterId);
    res.sendStatus(204);
  } catch (error) {
    next(error);
  }
});

characterRouter.get('/:characterId/progression', requireAuth, async (req, res, next) => {
  try {
    const characterId = String(req.params.characterId ?? '').trim();
    if (!characterId) {
      throw new HttpError(400, 'Character id is required');
    }
    const snapshot = await getCharacterProgressionForUser(req.user!.id, characterId);
    res.json(snapshot);
  } catch (error) {
    handleProgressionError(error, next);
  }
});

characterRouter.put('/:characterId/progression', requireAuth, async (req, res, next) => {
  try {
    const characterId = String(req.params.characterId ?? '').trim();
    if (!characterId) {
      throw new HttpError(400, 'Character id is required');
    }
    const payload = toProgressionUpdateInput(req.body);
    const intent = await submitProgressionIntentForUser(
      req.user!.id,
      characterId,
      payload
    );
    res.status(202).json(intent);
  } catch (error) {
    handleProgressionError(error, next);
  }
});

characterRouter.post('/:characterId/quests/complete', requireAuth, async (req, res, next) => {
  try {
    const characterId = String(req.params.characterId ?? '').trim();
    if (!characterId) {
      throw new HttpError(400, 'Character id is required');
    }
    const payload = toQuestCompletionInput(req.body);
    const intent = await submitQuestCompletionForUser(req.user!.id, characterId, payload);
    res.status(202).json(intent);
  } catch (error) {
    handleProgressionError(error, next);
  }
});

characterRouter.get('/:characterId/build-state', requireAuth, async (req, res, next) => {
  try {
    const characterId = String(req.params.characterId ?? '').trim();
    if (!characterId) {
      throw new HttpError(400, 'Character id is required');
    }
    const realmId = typeof req.query.realmId === 'string' ? req.query.realmId.trim() : undefined;
    const snapshot = await getBuildStateForUser(req.user!.id, characterId, realmId);
    res.json(snapshot);
  } catch (error) {
    next(error);
  }
});

characterRouter.put('/:characterId/build-state', requireAuth, async (req, res, next) => {
  try {
    const characterId = String(req.params.characterId ?? '').trim();
    if (!characterId) {
      throw new HttpError(400, 'Character id is required');
    }
    const realmId = typeof req.query.realmId === 'string' ? req.query.realmId.trim() : undefined;
    const payload = toBuildStateUpdateInput(req.body);
    const snapshot = await replaceBuildStateForUser(
      req.user!.id,
      characterId,
      realmId,
      payload
    );
    res.json(snapshot);
  } catch (error) {
    next(error);
  }
});

characterRouter.get('/:characterId/dock-layouts/:layoutKey', requireAuth, async (req, res, next) => {
  try {
    const characterId = String(req.params.characterId ?? '').trim();
    const layoutKey = decodeURIComponent(String(req.params.layoutKey ?? '').trim());
    if (!characterId || !layoutKey) {
      throw new HttpError(400, 'Character id and layout key are required');
    }
    const snapshot = await getDockLayoutForUser(req.user!.id, characterId, layoutKey);
    res.json(snapshot);
  } catch (error) {
    next(error);
  }
});

characterRouter.put('/:characterId/dock-layouts/:layoutKey', requireAuth, async (req, res, next) => {
  try {
    const characterId = String(req.params.characterId ?? '').trim();
    const layoutKey = decodeURIComponent(String(req.params.layoutKey ?? '').trim());
    if (!characterId || !layoutKey) {
      throw new HttpError(400, 'Character id and layout key are required');
    }
    const order = req.body?.order;
    if (order !== undefined && !Array.isArray(order)) {
      throw new HttpError(400, 'order must be an array');
    }
    const snapshot = await replaceDockLayoutForUser(
      req.user!.id,
      characterId,
      layoutKey,
      order ?? []
    );
    res.json(snapshot);
  } catch (error) {
    next(error);
  }
});

characterRouter.get('/:characterId/map-pins', requireAuth, async (req, res, next) => {
  try {
    const characterId = String(req.params.characterId ?? '').trim();
    if (!characterId) {
      throw new HttpError(400, 'Character id is required');
    }
    const snapshot = await getMapPinsForUser(req.user!.id, characterId);
    res.json(snapshot);
  } catch (error) {
    next(error);
  }
});

characterRouter.put('/:characterId/map-pins', requireAuth, async (req, res, next) => {
  try {
    const characterId = String(req.params.characterId ?? '').trim();
    if (!characterId) {
      throw new HttpError(400, 'Character id is required');
    }
    const payload = toMapPinUpdateInput(req.body);
    const snapshot = await replaceMapPinsForUser(req.user!.id, characterId, payload);
    res.json(snapshot);
  } catch (error) {
    next(error);
  }
});

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
    classId: typeof value.classId === 'string' ? value.classId : undefined,
    classStates: Array.isArray(value.classStates)
      ? (value.classStates as CharacterClassState[])
      : undefined,
    lastKnownLocation:
      typeof value.lastKnownLocation === 'string' ? value.lastKnownLocation : undefined,
  };
}

function toProgressionUpdateInput(body: unknown): ProgressionUpdateInput {
  if (!body || typeof body !== 'object') {
    return {};
  }

  const value = body as Record<string, unknown>;
  const result: ProgressionUpdateInput = {};

  if (value.progression !== undefined) {
    if (value.progression === null) {
      // explicit null means no update
    } else if (typeof value.progression !== 'object') {
      throw new HttpError(400, 'progression must be an object');
    } else {
      const progression = value.progression as Record<string, unknown>;
      const level = ensureInteger(progression.level, 'progression.level');
      if (level < 1) {
        throw new HttpError(400, 'progression.level must be at least 1');
      }
      const xp = ensureInteger(progression.xp, 'progression.xp');
      if (xp < 0) {
        throw new HttpError(400, 'progression.xp cannot be negative');
      }
      const expectedVersion = ensureInteger(progression.expectedVersion, 'progression.expectedVersion');
      if (expectedVersion < 0) {
        throw new HttpError(400, 'progression.expectedVersion must be non-negative');
      }
      result.progression = { level, xp, expectedVersion };
    }
  }

  if (value.classUnlocks !== undefined) {
    if (value.classUnlocks === null) {
      // no update
    } else if (typeof value.classUnlocks !== 'object') {
      throw new HttpError(400, 'classUnlocks must be an object');
    } else {
      const classUnlocksValue = value.classUnlocks as Record<string, unknown>;
      const expectedVersion = ensureInteger(
        classUnlocksValue.expectedVersion,
        'classUnlocks.expectedVersion'
      );
      if (expectedVersion < 0) {
        throw new HttpError(400, 'classUnlocks.expectedVersion must be non-negative');
      }
      const unlocksRaw = classUnlocksValue.unlocks;
      if (!Array.isArray(unlocksRaw)) {
        throw new HttpError(400, 'classUnlocks.unlocks must be an array');
      }
      const unlocks = unlocksRaw.map((entry, index) => {
        if (!entry || typeof entry !== 'object') {
          throw new HttpError(400, `classUnlocks.unlocks[${index}] must be an object`);
        }
        const record = entry as Record<string, unknown>;
        const classId = typeof record.classId === 'string' ? record.classId.trim() : '';
        if (!classId) {
          throw new HttpError(400, `classUnlocks.unlocks[${index}].classId is required`);
        }
        const unlocked = typeof record.unlocked === 'boolean' ? record.unlocked : false;
        const unlockedAt =
          typeof record.unlockedAt === 'string' && record.unlockedAt.trim() !== ''
            ? record.unlockedAt
            : null;
        return { classId, unlocked, unlockedAt };
      });
      result.classUnlocks = { expectedVersion, unlocks };
    }
  }

  if (value.inventory !== undefined) {
    if (value.inventory === null) {
      // no update
    } else if (typeof value.inventory !== 'object') {
      throw new HttpError(400, 'inventory must be an object');
    } else {
      const inventoryValue = value.inventory as Record<string, unknown>;
      const expectedVersion = ensureInteger(
        inventoryValue.expectedVersion,
        'inventory.expectedVersion'
      );
      if (expectedVersion < 0) {
        throw new HttpError(400, 'inventory.expectedVersion must be non-negative');
      }
      const itemsRaw = inventoryValue.items;
      if (!Array.isArray(itemsRaw)) {
        throw new HttpError(400, 'inventory.items must be an array');
      }
      const items = itemsRaw.map((entry, index) => {
        if (!entry || typeof entry !== 'object') {
          throw new HttpError(400, `inventory.items[${index}] must be an object`);
        }
        const record = entry as Record<string, unknown>;
        const itemId = typeof record.itemId === 'string' ? record.itemId.trim() : '';
        if (!itemId) {
          throw new HttpError(400, `inventory.items[${index}].itemId is required`);
        }
        const quantity = ensureInteger(record.quantity, `inventory.items[${index}].quantity`);
        if (quantity < 0) {
          throw new HttpError(400, `inventory.items[${index}].quantity cannot be negative`);
        }
        const metadata = record.metadata;
        if (metadata !== undefined && !isJsonValue(metadata)) {
          throw new HttpError(400, `inventory.items[${index}].metadata must be JSON-serializable`);
        }
        const normalizedMetadata = metadata as JsonValue | undefined;
        return { itemId, quantity, metadata: normalizedMetadata };
      });
      result.inventory = { expectedVersion, items };
    }
  }

  if (value.equipment !== undefined) {
    if (value.equipment === null) {
      // no update
    } else if (typeof value.equipment !== 'object') {
      throw new HttpError(400, 'equipment must be an object');
    } else {
      const equipmentValue = value.equipment as Record<string, unknown>;
      const expectedVersion = ensureInteger(
        equipmentValue.expectedVersion,
        'equipment.expectedVersion'
      );
      if (expectedVersion < 0) {
        throw new HttpError(400, 'equipment.expectedVersion must be non-negative');
      }
      const itemsRaw = equipmentValue.items;
      if (!Array.isArray(itemsRaw)) {
        throw new HttpError(400, 'equipment.items must be an array');
      }
      const allowedSlots = new Set<EquipmentSlot>(equipmentSlots);
      const items = itemsRaw.map((entry, index) => {
        if (!entry || typeof entry !== 'object') {
          throw new HttpError(400, `equipment.items[${index}] must be an object`);
        }
        const record = entry as Record<string, unknown>;
        const classId = typeof record.classId === 'string' ? record.classId.trim() : '';
        if (!classId) {
          throw new HttpError(400, `equipment.items[${index}].classId is required`);
        }
        const slot = typeof record.slot === 'string' ? record.slot.trim() : '';
        if (!slot || !allowedSlots.has(slot as EquipmentSlot)) {
          throw new HttpError(400, `equipment.items[${index}].slot is invalid`);
        }
        const itemId = typeof record.itemId === 'string' ? record.itemId.trim() : '';
        if (!itemId) {
          throw new HttpError(400, `equipment.items[${index}].itemId is required`);
        }
        const metadata = record.metadata;
        if (metadata !== undefined && !isJsonValue(metadata)) {
          throw new HttpError(400, `equipment.items[${index}].metadata must be JSON-serializable`);
        }
        const normalizedMetadata = metadata as JsonValue | undefined;
        return { classId, slot, itemId, metadata: normalizedMetadata };
      });
      result.equipment = { expectedVersion, items };
    }
  }

  if (value.quests !== undefined) {
    if (value.quests === null) {
      // no update
    } else if (typeof value.quests !== 'object') {
      throw new HttpError(400, 'quests must be an object');
    } else {
      const questsValue = value.quests as Record<string, unknown>;
      const expectedVersion = ensureInteger(questsValue.expectedVersion, 'quests.expectedVersion');
      if (expectedVersion < 0) {
        throw new HttpError(400, 'quests.expectedVersion must be non-negative');
      }
      const questsRaw = questsValue.quests;
      if (!Array.isArray(questsRaw)) {
        throw new HttpError(400, 'quests.quests must be an array');
      }
      const quests = questsRaw.map((entry, index) => {
        if (!entry || typeof entry !== 'object') {
          throw new HttpError(400, `quests.quests[${index}] must be an object`);
        }
        const record = entry as Record<string, unknown>;
        const questId = typeof record.questId === 'string' ? record.questId.trim() : '';
        if (!questId) {
          throw new HttpError(400, `quests.quests[${index}].questId is required`);
        }
        const status = typeof record.status === 'string' ? record.status.trim() : '';
        if (!status) {
          throw new HttpError(400, `quests.quests[${index}].status is required`);
        }
        const progress = record.progress;
        if (progress !== undefined && !isJsonValue(progress)) {
          throw new HttpError(400, `quests.quests[${index}].progress must be JSON-serializable`);
        }
        const normalizedProgress = progress as JsonValue | undefined;
        return { questId, status, progress: normalizedProgress };
      });
      result.quests = { expectedVersion, quests };
    }
  }

  return result;
}

function toQuestCompletionInput(body: unknown): QuestCompletionInput {
  if (!body || typeof body !== 'object') {
    throw new HttpError(400, 'Request body is required');
  }

  const record = body as Record<string, unknown>;
  const questId = typeof record.questId === 'string' ? record.questId.trim() : '';
  if (!questId) {
    throw new HttpError(400, 'questId is required');
  }

  let progress: JsonValue | undefined;
  if ('progress' in record) {
    if (!isJsonValue(record.progress)) {
      throw new HttpError(400, 'progress must be JSON-serializable');
    }
    progress = record.progress as JsonValue;
  }

  return { questId, progress };
}

function toMapPinUpdateInput(body: unknown): MapPinUpdateInput {
  if (!body || typeof body !== 'object') {
    return { expectedVersion: 0, pins: [] };
  }

  const value = body as Record<string, unknown>;
  const pins = Array.isArray(value.pins) ? value.pins : [];
  const expectedVersion = ensureInteger(value.expectedVersion, 'expectedVersion');
  if (expectedVersion < 0) {
    throw new HttpError(400, 'expectedVersion must be non-negative');
  }

  const mappedPins = pins
    .map((entry) => {
      if (!entry || typeof entry !== 'object') {
        return null;
      }
      const item = entry as Record<string, unknown>;
      const pinId = typeof item.pinId === 'string' ? item.pinId.trim() : '';
      const unlocked = Boolean(item.unlocked);
      if (!pinId) {
        return null;
      }
      return { pinId, unlocked };
    })
    .filter((entry) => entry != null) as MapPinUpdateInput['pins'];

  return { expectedVersion, pins: mappedPins };
}

function toBuildStateUpdateInput(body: unknown): BuildStateUpdateInput {
  if (!body || typeof body !== 'object') {
    throw new HttpError(400, 'build state payload is required');
  }

  const value = body as Record<string, unknown>;
  const plots = value.plots;
  const constructions = value.constructions;

  if (!Array.isArray(plots)) {
    throw new HttpError(400, 'plots must be an array');
  }
  if (!isJsonValue(plots)) {
    throw new HttpError(400, 'plots must be JSON-serializable');
  }

  if (!Array.isArray(constructions)) {
    throw new HttpError(400, 'constructions must be an array');
  }
  if (!isJsonValue(constructions)) {
    throw new HttpError(400, 'constructions must be JSON-serializable');
  }

  return {
    plots: plots as JsonValue[],
    constructions: constructions as JsonValue[],
  };
}

function ensureInteger(value: unknown, field: string): number {
  if (typeof value === 'number' && Number.isFinite(value)) {
    return Math.trunc(value);
  }
  throw new HttpError(400, `${field} must be a number`);
}

function isJsonValue(value: unknown): boolean {
  if (value === null) {
    return true;
  }
  const type = typeof value;
  if (type === 'string' || type === 'number' || type === 'boolean') {
    return true;
  }
  if (Array.isArray(value)) {
    return value.every(isJsonValue);
  }
  if (type === 'object') {
    return Object.values(value as Record<string, unknown>).every(isJsonValue);
  }
  return false;
}

function handleProgressionError(error: unknown, next: NextFunction): void {
  if (error instanceof VersionConflictError) {
    next(new HttpError(409, `${error.entity} update failed due to version mismatch`));
    return;
  }
  next(error);
}
