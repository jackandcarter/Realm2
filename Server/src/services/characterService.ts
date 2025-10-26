import { findRealmById } from '../db/realmRepository';
import { ensureMembership } from './realmService';
import {
  createCharacter,
  findCharacterByNameForUser,
  Character,
} from '../db/characterRepository';
import { HttpError } from '../utils/errors';
import {
  CharacterAppearance,
  isCharacterAppearance,
  JsonValue,
} from '../types/characterCustomization';
import { CharacterClassState } from '../types/classUnlocks';
import {
  findRaceById,
  getCanonicalRaceIds,
  getDefaultRace,
  RaceCustomizationOptions,
  RaceDefinition,
} from '../config/races';

export interface CreateCharacterInput {
  realmId?: string;
  name?: string;
  bio?: string;
  raceId?: string;
  appearance?: CharacterAppearance;
  classId?: string;
  classStates?: CharacterClassState[];
  lastKnownLocation?: string;
}

export function createCharacterForUser(
  userId: string,
  input: CreateCharacterInput
): Character {
  const trimmedName = input.name?.trim();
  if (!input.realmId || !trimmedName) {
    throw new HttpError(400, 'realmId and name are required');
  }

  const realm = findRealmById(input.realmId);
  if (!realm) {
    throw new HttpError(404, 'Realm not found');
  }

  ensureMembership(userId, input.realmId);

  if (findCharacterByNameForUser(userId, input.realmId, trimmedName)) {
    throw new HttpError(409, 'Character with that name already exists in this realm');
  }

  const { raceId, race } = resolveRace(input.raceId);
  const appearance = normalizeAppearanceForRace(input.appearance, race);

  try {
    return createCharacter({
      realmId: realm.id,
      userId,
      name: trimmedName,
      bio: input.bio?.trim() || undefined,
      raceId,
      appearance,
      classId: input.classId,
      classStates: input.classStates,
      lastKnownLocation: input.lastKnownLocation,
    });
  } catch (_error) {
    throw new HttpError(500, 'Unable to create character');
  }
}

function resolveRace(rawRaceId: string | undefined) {
  if (!rawRaceId) {
    const fallback = getDefaultRace();
    return { raceId: fallback.id, race: fallback };
  }

  const resolved = findRaceById(rawRaceId);
  if (!resolved) {
    throw new HttpError(
      400,
      `Invalid raceId. Allowed values: ${getCanonicalRaceIds().join(', ')}`
    );
  }

  return { raceId: resolved.id, race: resolved };
}

function normalizeAppearanceForRace(
  appearanceInput: CharacterAppearance | undefined,
  race: RaceDefinition
): CharacterAppearance | undefined {
  if (!appearanceInput) {
    return undefined;
  }

  if (!isCharacterAppearance(appearanceInput)) {
    throw new HttpError(400, 'Invalid appearance payload');
  }

  const normalized: CharacterAppearance = {};
  const record = appearanceInput as Record<string, JsonValue | undefined>;

  if ('height' in record) {
    normalized.height = validateDimension(
      record.height,
      race.customization,
      'height',
      race.id
    );
  }

  if ('build' in record) {
    normalized.build = validateDimension(
      record.build,
      race.customization,
      'build',
      race.id
    );
  }

  for (const [key, value] of Object.entries(record)) {
    if (key === 'height' || key === 'build' || typeof value === 'undefined') {
      continue;
    }
    normalized[key] = value;
  }

  return Object.keys(normalized).length === 0 ? undefined : normalized;
}

function validateDimension(
  value: JsonValue | undefined,
  customization: RaceCustomizationOptions | undefined,
  dimension: 'height' | 'build',
  raceId: string
): number {
  if (typeof value !== 'number' || !Number.isFinite(value)) {
    throw new HttpError(400, `Appearance.${dimension} must be a number`);
  }

  const range = customization?.[dimension];
  if (range) {
    if (value < range.min || value > range.max) {
      throw new HttpError(
        400,
        `Appearance.${dimension} for race ${raceId} must be between ${range.min} and ${range.max}`
      );
    }
  }

  return value;
}
