import { findCharacterById } from '../db/characterRepository';
import { getDockLayout, upsertDockLayout } from '../db/dockLayoutRepository';
import { HttpError } from '../utils/errors';

export interface DockLayoutSnapshot {
  layoutKey: string;
  order: string[];
  updatedAt: string;
}

export async function getDockLayoutForUser(
  userId: string,
  characterId: string,
  layoutKey: string
): Promise<DockLayoutSnapshot> {
  const character = await findCharacterById(characterId);
  if (!character) {
    throw new HttpError(404, 'Character not found');
  }

  if (character.userId !== userId) {
    throw new HttpError(403, 'You do not have access to this character');
  }

  const record = await getDockLayout(characterId, layoutKey);
  if (!record) {
    return {
      layoutKey,
      order: [],
      updatedAt: new Date().toISOString(),
    };
  }

  let order: string[] = [];
  try {
    order = JSON.parse(record.layoutJson) as string[];
    if (!Array.isArray(order)) {
      order = [];
    }
  } catch (_error) {
    order = [];
  }

  return {
    layoutKey,
    order,
    updatedAt: record.updatedAt,
  };
}

export async function replaceDockLayoutForUser(
  userId: string,
  characterId: string,
  layoutKey: string,
  order: string[]
): Promise<DockLayoutSnapshot> {
  const character = await findCharacterById(characterId);
  if (!character) {
    throw new HttpError(404, 'Character not found');
  }

  if (character.userId !== userId) {
    throw new HttpError(403, 'You do not have access to this character');
  }

  const sanitized = Array.isArray(order)
    ? order.filter((entry) => typeof entry === 'string' && entry.trim().length > 0)
    : [];

  const record = await upsertDockLayout(
    characterId,
    layoutKey,
    JSON.stringify(sanitized)
  );

  return {
    layoutKey,
    order: sanitized,
    updatedAt: record.updatedAt,
  };
}
