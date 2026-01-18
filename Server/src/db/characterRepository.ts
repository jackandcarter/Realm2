import { randomUUID } from 'crypto';
import { db } from './database';
import { initializeCharacterProgressionState } from './progressionRepository';
import {
  CharacterAppearance,
  deserializeAppearance,
  serializeAppearance,
} from '../types/characterCustomization';
import {
  CharacterClassState,
  deserializeClassStates,
  sanitizeClassStates,
  serializeClassStates,
} from '../types/classUnlocks';

export interface Character {
  id: string;
  userId: string;
  realmId: string;
  name: string;
  bio?: string | null;
  raceId: string;
  appearance: CharacterAppearance;
  createdAt: string;
  classId: string | null;
  classStates: CharacterClassState[];
  lastKnownLocation: string | null;
}

export interface NewCharacter {
  userId: string;
  realmId: string;
  name: string;
  bio?: string;
  raceId?: string;
  appearance?: CharacterAppearance;
  classId?: string;
  classStates?: CharacterClassState[];
  lastKnownLocation?: string;
}

export async function createCharacter(input: NewCharacter): Promise<Character> {
  const raceId = input.raceId?.trim() || 'human';
  const appearance = input.appearance ?? {};
  const classId = input.classId?.trim() || null;
  const classStates = sanitizeClassStates(input.classStates ?? []);
  const lastKnownLocation = input.lastKnownLocation?.trim() || null;
  const character: Character = {
    id: randomUUID(),
    userId: input.userId,
    realmId: input.realmId,
    name: input.name,
    bio: input.bio ?? null,
    raceId,
    appearance,
    createdAt: new Date().toISOString(),
    classId,
    classStates,
    lastKnownLocation,
  };

  await db.execute(
    `INSERT INTO characters (id, user_id, realm_id, name, bio, race_id, appearance_json, created_at, class_id, class_states_json, last_location)
     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
    [
      character.id,
      character.userId,
      character.realmId,
      character.name,
      character.bio ?? null,
      character.raceId,
      serializeAppearance(character.appearance),
      character.createdAt,
      character.classId,
      serializeClassStates(character.classStates),
      character.lastKnownLocation,
    ]
  );
  await initializeCharacterProgressionState(character.id);
  return character;
}

export async function findCharacterById(id: string): Promise<Character | undefined> {
  const rows = await db.query<CharacterRow[]>(
    `SELECT
       id,
       user_id as userId,
       realm_id as realmId,
       name,
       bio,
       race_id as raceId,
       appearance_json as appearanceJson,
       created_at as createdAt,
       class_id as classId,
       class_states_json as classStatesJson,
       last_location as lastKnownLocation
     FROM characters
     WHERE id = ?`,
    [id]
  );

  const row = rows[0];
  return row ? mapRowToCharacter(row) : undefined;
}

export async function findCharacterByNameForUser(
  userId: string,
  realmId: string,
  name: string
): Promise<Character | undefined> {
  const rows = await db.query<CharacterRow[]>(
    `SELECT
       id,
       user_id as userId,
       realm_id as realmId,
       name,
       bio,
       race_id as raceId,
       appearance_json as appearanceJson,
       created_at as createdAt,
       class_id as classId,
       class_states_json as classStatesJson,
       last_location as lastKnownLocation
     FROM characters
     WHERE user_id = ? AND realm_id = ? AND name = ?`,
    [userId, realmId, name]
  );
  const row = rows[0];
  return row ? mapRowToCharacter(row) : undefined;
}

export async function listCharactersForRealm(realmId: string): Promise<Character[]> {
  const rows = await db.query<CharacterRow[]>(
    `SELECT
       id,
       user_id as userId,
       realm_id as realmId,
       name,
       bio,
       race_id as raceId,
       appearance_json as appearanceJson,
       created_at as createdAt,
       class_id as classId,
       class_states_json as classStatesJson,
       last_location as lastKnownLocation
     FROM characters
     WHERE realm_id = ?
     ORDER BY created_at ASC`,
    [realmId]
  );
  return rows.map((row) => mapRowToCharacter(row));
}

export async function listCharactersForUserInRealm(
  userId: string,
  realmId: string
): Promise<Character[]> {
  const rows = await db.query<CharacterRow[]>(
    `SELECT
       id,
       user_id as userId,
       realm_id as realmId,
       name,
       bio,
       race_id as raceId,
       appearance_json as appearanceJson,
       created_at as createdAt,
       class_id as classId,
       class_states_json as classStatesJson,
       last_location as lastKnownLocation
     FROM characters
     WHERE realm_id = ? AND user_id = ?
     ORDER BY created_at ASC`,
    [realmId, userId]
  );
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
  classId: string | null;
  classStatesJson: string | null;
  lastKnownLocation: string | null;
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
    classId: row.classId ?? null,
    classStates: deserializeClassStates(row.classStatesJson),
    lastKnownLocation: row.lastKnownLocation ?? null,
  };
}
