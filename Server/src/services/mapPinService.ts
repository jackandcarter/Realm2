import { findCharacterById } from '../db/characterRepository';
import {
  getMapPinSnapshot,
  MapPinStateCollection,
  MapPinStateInput,
  MapPinVersionConflictError,
  replaceMapPinStates,
} from '../db/mapPinRepository';
import { HttpError } from '../utils/errors';

export interface MapPinUpdateInput {
  expectedVersion: number;
  pins: MapPinStateInput[];
}

export async function getMapPinsForUser(
  userId: string,
  characterId: string
): Promise<MapPinStateCollection> {
  const character = await findCharacterById(characterId);
  if (!character) {
    throw new HttpError(404, 'Character not found');
  }

  if (character.userId !== userId) {
    throw new HttpError(403, 'You do not have access to this character');
  }

  return getMapPinSnapshot(characterId);
}

export async function replaceMapPinsForUser(
  userId: string,
  characterId: string,
  input: MapPinUpdateInput
): Promise<MapPinStateCollection> {
  const character = await findCharacterById(characterId);
  if (!character) {
    throw new HttpError(404, 'Character not found');
  }

  if (character.userId !== userId) {
    throw new HttpError(403, 'You do not have access to this character');
  }

  try {
    return await replaceMapPinStates(characterId, input.pins ?? [], input.expectedVersion ?? 0);
  } catch (error) {
    if (error instanceof MapPinVersionConflictError) {
      throw new HttpError(409, 'Map pin progression version conflict', {
        expectedVersion: error.expected,
        actualVersion: error.actual,
      });
    }
    throw error;
  }
}
