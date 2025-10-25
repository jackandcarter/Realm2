import { findRealmById } from '../db/realmRepository';
import { ensureMembership } from './realmService';
import {
  createCharacter,
  findCharacterByNameForUser,
  Character,
} from '../db/characterRepository';
import { HttpError } from '../utils/errors';

export interface CreateCharacterInput {
  realmId: string;
  name: string;
  bio?: string;
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

  try {
    return createCharacter({
      realmId: realm.id,
      userId,
      name: trimmedName,
      bio: input.bio?.trim() || undefined,
    });
  } catch (_error) {
    throw new HttpError(500, 'Unable to create character');
  }
}
