import { randomUUID } from 'crypto';
import { db } from './database';
import {
  CharacterAppearance,
  deserializeAppearance,
  serializeAppearance,
} from '../types/characterCustomization';

export interface Character {
  id: string;
  userId: string;
  realmId: string;
  name: string;
  bio?: string | null;
  raceId: string;
  appearance: CharacterAppearance;
  createdAt: string;
}

export interface NewCharacter {
  userId: string;
  realmId: string;
  name: string;
  bio?: string;
  raceId?: string;
  appearance?: CharacterAppearance;
}

export function createCharacter(input: NewCharacter): Character {
  const raceId = input.raceId?.trim() || 'human';
  const appearance = input.appearance ?? {};
  const character: Character = {
    id: randomUUID(),
    userId: input.userId,
    realmId: input.realmId,
    name: input.name,
    bio: input.bio ?? null,
    raceId,
    appearance,
    createdAt: new Date().toISOString(),
  };

  const stmt = db.prepare(
    `INSERT INTO characters (id, user_id, realm_id, name, bio, race_id, appearance_json, created_at)
     VALUES (@id, @userId, @realmId, @name, @bio, @raceId, @appearanceJson, @createdAt)`
  );
  stmt.run({
    id: character.id,
    userId: character.userId,
    realmId: character.realmId,
    name: character.name,
    bio: character.bio ?? null,
    raceId: character.raceId,
    appearanceJson: serializeAppearance(character.appearance),
    createdAt: character.createdAt,
  });
  return character;
}

export function findCharacterByNameForUser(
  userId: string,
  realmId: string,
  name: string
): Character | undefined {
  const stmt = db.prepare(
    `SELECT
       id,
       user_id as userId,
       realm_id as realmId,
       name,
       bio,
       race_id as raceId,
       appearance_json as appearanceJson,
       created_at as createdAt
     FROM characters
     WHERE user_id = ? AND realm_id = ? AND name = ?`
  );
  const row = stmt.get(userId, realmId, name) as CharacterRow | undefined;
  return row ? mapRowToCharacter(row) : undefined;
}

export function listCharactersForRealm(realmId: string): Character[] {
  const stmt = db.prepare(
    `SELECT
       id,
       user_id as userId,
       realm_id as realmId,
       name,
       bio,
       race_id as raceId,
       appearance_json as appearanceJson,
       created_at as createdAt
     FROM characters
     WHERE realm_id = ?
     ORDER BY created_at ASC`
  );
  const rows = stmt.all(realmId) as CharacterRow[];
  return rows.map((row) => mapRowToCharacter(row));
}

export function listCharactersForUserInRealm(
  userId: string,
  realmId: string
): Character[] {
  const stmt = db.prepare(
    `SELECT
       id,
       user_id as userId,
       realm_id as realmId,
       name,
       bio,
       race_id as raceId,
       appearance_json as appearanceJson,
       created_at as createdAt
     FROM characters
     WHERE realm_id = ? AND user_id = ?
     ORDER BY created_at ASC`
  );
  const rows = stmt.all(realmId, userId) as CharacterRow[];
  return rows.map((row) => mapRowToCharacter(row));
}

interface CharacterRow {
  id: string;
  userId: string;
  realmId: string;
  name: string;
  bio: string | null;
  raceId: string;
  appearanceJson: string | null;
  createdAt: string;
}

function mapRowToCharacter(row: CharacterRow): Character {
  return {
    id: row.id,
    userId: row.userId,
    realmId: row.realmId,
    name: row.name,
    bio: row.bio,
    raceId: row.raceId || 'human',
    appearance: deserializeAppearance(row.appearanceJson),
    createdAt: row.createdAt,
  };
}
