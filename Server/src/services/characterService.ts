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
import { CharacterClassState, sanitizeClassStates } from '../types/classUnlocks';
import {
  findRaceById,
  getCanonicalRaceIds,
  getDefaultRace,
  RaceCustomizationOptions,
  RaceDefinition,
} from '../config/races';
import {
  findClassRule,
  getAllowedClassIdsForRace,
  getStarterClassIdsForRace,
} from '../config/classRules';

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

export async function createCharacterForUser(
  userId: string,
  input: CreateCharacterInput
): Promise<Character> {
  const trimmedName = input.name?.trim();
  if (!input.realmId || !trimmedName) {
    throw new HttpError(400, 'realmId and name are required');
  }

  const realm = await findRealmById(input.realmId);
  if (!realm) {
    throw new HttpError(404, 'Realm not found');
  }

  await ensureMembership(userId, input.realmId);

  if (await findCharacterByNameForUser(userId, input.realmId, trimmedName)) {
    throw new HttpError(409, 'Character with that name already exists in this realm');
  }

  const { raceId, race } = resolveRace(input.raceId);
  const appearance = normalizeAppearanceForRace(input.appearance, race);
  const { classId, classStates } = normalizeClassSelectionForRace(
    race.id,
    input.classId,
    input.classStates
  );

  try {
    return await createCharacter({
      realmId: realm.id,
      userId,
      name: trimmedName,
      bio: input.bio?.trim() || undefined,
      raceId,
      appearance,
      classId,
      classStates,
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

function normalizeClassSelectionForRace(
  raceId: string,
  rawClassId: string | undefined,
  classStatesInput: CharacterClassState[] | undefined
): { classId?: string; classStates: CharacterClassState[] } {
  const allowedClassIds = getAllowedClassIdsForRace(raceId);
  const starterClassIds = getStarterClassIdsForRace(raceId);
  const allowedSet = new Set(allowedClassIds.map((id) => id.toLowerCase()));
  const starterSet = new Set(starterClassIds.map((id) => id.toLowerCase()));

  let classId: string | undefined;
  if (rawClassId && rawClassId.trim() !== '') {
    const trimmed = rawClassId.trim();
    if (!allowedSet.has(trimmed.toLowerCase())) {
      throw new HttpError(400, `Class ${trimmed} is not available to race ${raceId}`);
    }
    classId = trimmed;
  }

  const sanitizedStates = sanitizeClassStates(classStatesInput ?? []);
  const normalizedStates: CharacterClassState[] = [];
  const seen = new Set<string>();

  for (const state of sanitizedStates) {
    const trimmedId = state.classId.trim();
    const key = trimmedId.toLowerCase();
    if (!allowedSet.has(key) || seen.has(key)) {
      continue;
    }
    normalizedStates.push({ classId: trimmedId, unlocked: Boolean(state.unlocked) });
    seen.add(key);
  }

  for (const allowedId of allowedClassIds) {
    const key = allowedId.toLowerCase();
    if (seen.has(key)) {
      continue;
    }
    normalizedStates.push({ classId: allowedId, unlocked: starterSet.has(key) });
    seen.add(key);
  }

  if (classId) {
    const rule = findClassRule(classId);
    if (rule && rule.unlockMethod !== 'starter') {
      const state = normalizedStates.find(
        (entry) => entry.classId.toLowerCase() === classId!.toLowerCase()
      );
      if (!state || !state.unlocked) {
        throw new HttpError(400, `Class ${classId} must be unlocked before it can be selected`);
      }
    }
  }

  return { classId, classStates: normalizedStates };
}
