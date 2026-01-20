import { findCharacterById } from '../db/characterRepository';
import { BuildStateSnapshot, getBuildState, upsertBuildState } from '../db/buildStateRepository';
import { HttpError } from '../utils/errors';
import { JsonValue } from '../types/characterCustomization';

export interface BuildStateUpdateInput {
  plots: JsonValue[];
  constructions: JsonValue[];
}

export async function getBuildStateForUser(
  userId: string,
  characterId: string,
  realmId?: string
): Promise<BuildStateSnapshot> {
  const character = await findCharacterById(characterId);
  if (!character) {
    throw new HttpError(404, 'Character not found');
  }

  if (character.userId !== userId) {
    throw new HttpError(403, 'You do not have access to this character');
  }

  if (realmId && realmId !== character.realmId) {
    throw new HttpError(400, 'Realm id does not match character realm');
  }

  const effectiveRealmId = realmId ?? character.realmId;
  const existing = await getBuildState(characterId, effectiveRealmId);
  if (existing) {
    return existing;
  }

  return {
    id: '',
    realmId: effectiveRealmId,
    characterId,
    plots: [],
    constructions: [],
    updatedAt: new Date().toISOString(),
  };
}

export async function replaceBuildStateForUser(
  userId: string,
  characterId: string,
  realmId: string | undefined,
  input: BuildStateUpdateInput
): Promise<BuildStateSnapshot> {
  const character = await findCharacterById(characterId);
  if (!character) {
    throw new HttpError(404, 'Character not found');
  }

  if (character.userId !== userId) {
    throw new HttpError(403, 'You do not have access to this character');
  }

  if (realmId && realmId !== character.realmId) {
    throw new HttpError(400, 'Realm id does not match character realm');
  }

  const effectiveRealmId = realmId ?? character.realmId;

  return upsertBuildState(
    characterId,
    effectiveRealmId,
    input.plots ?? [],
    input.constructions ?? []
  );
}
