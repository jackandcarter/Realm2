import { findCharacterById, Character } from '../db/characterRepository';
import { HttpError } from '../utils/errors';

export async function requireOwnedCharacter(
  userId: string,
  characterId: string
): Promise<Character> {
  const character = await findCharacterById(characterId);
  if (!character) {
    throw new HttpError(404, 'Character not found');
  }

  if (character.userId !== userId) {
    throw new HttpError(403, 'You do not have access to this character');
  }

  return character;
}

export async function requireCharacter(
  characterId: string
): Promise<Character> {
  const character = await findCharacterById(characterId);
  if (!character) {
    throw new HttpError(404, 'Character not found');
  }
  return character;
}
