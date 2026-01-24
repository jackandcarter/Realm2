import { randomUUID } from 'crypto';
import { ClassResourceType } from '../config/gameEnums';
import { db } from './database';
import { AbilityResultEvent } from '../gameplay/combat/types';

export interface CombatClientTimeRecord {
  userId: string;
  casterId: string;
  lastClientTime: number;
  updatedAt: string;
}

export interface AbilityCooldownRecord {
  characterId: string;
  abilityId: string;
  lastUsedAt: number;
  updatedAt: string;
}

export interface CharacterResourceState {
  characterId: string;
  resourceType: ClassResourceType;
  currentValue: number;
  maxValue: number;
  updatedAt: string;
}

export async function getCombatClientTime(
  userId: string,
  casterId: string,
): Promise<CombatClientTimeRecord | undefined> {
  const rows = await db.query<CombatClientTimeRecord[]>(
    `SELECT user_id as userId,
            caster_id as casterId,
            last_client_time as lastClientTime,
            updated_at as updatedAt
     FROM combat_client_times
     WHERE user_id = ? AND caster_id = ?`,
    [userId, casterId],
  );
  return rows[0];
}

export async function upsertCombatClientTime(
  userId: string,
  casterId: string,
  clientTime: number,
): Promise<void> {
  const now = new Date().toISOString();
  await db.execute(
    `INSERT INTO combat_client_times (user_id, caster_id, last_client_time, updated_at)
     VALUES (?, ?, ?, ?)
     ON DUPLICATE KEY UPDATE last_client_time = VALUES(last_client_time), updated_at = VALUES(updated_at)`,
    [userId, casterId, clientTime, now],
  );
}

export async function getAbilityCooldown(
  characterId: string,
  abilityId: string,
): Promise<AbilityCooldownRecord | undefined> {
  const rows = await db.query<AbilityCooldownRecord[]>(
    `SELECT character_id as characterId,
            ability_id as abilityId,
            last_used_at as lastUsedAt,
            updated_at as updatedAt
     FROM combat_ability_cooldowns
     WHERE character_id = ? AND ability_id = ?`,
    [characterId, abilityId],
  );
  return rows[0];
}

export async function upsertAbilityCooldown(
  characterId: string,
  abilityId: string,
  lastUsedAt: number,
): Promise<void> {
  const now = new Date().toISOString();
  await db.execute(
    `INSERT INTO combat_ability_cooldowns (character_id, ability_id, last_used_at, updated_at)
     VALUES (?, ?, ?, ?)
     ON DUPLICATE KEY UPDATE last_used_at = VALUES(last_used_at), updated_at = VALUES(updated_at)`,
    [characterId, abilityId, lastUsedAt, now],
  );
}

export async function getCharacterResourceState(
  characterId: string,
  resourceType: ClassResourceType,
): Promise<CharacterResourceState | undefined> {
  const rows = await db.query<CharacterResourceState[]>(
    `SELECT character_id as characterId,
            resource_type as resourceType,
            current_value as currentValue,
            max_value as maxValue,
            updated_at as updatedAt
     FROM character_resource_state
     WHERE character_id = ? AND resource_type = ?`,
    [characterId, resourceType],
  );
  return rows[0];
}

export async function upsertCharacterResourceState(
  characterId: string,
  resourceType: ClassResourceType,
  currentValue: number,
  maxValue: number,
): Promise<void> {
  const now = new Date().toISOString();
  await db.execute(
    `INSERT INTO character_resource_state (character_id, resource_type, current_value, max_value, updated_at)
     VALUES (?, ?, ?, ?, ?)
     ON DUPLICATE KEY UPDATE
       current_value = VALUES(current_value),
       max_value = VALUES(max_value),
       updated_at = VALUES(updated_at)`,
    [characterId, resourceType, currentValue, maxValue, now],
  );
}

export async function recordCombatEvents(
  requestId: string,
  abilityId: string,
  casterId: string,
  events: AbilityResultEvent[],
): Promise<void> {
  if (!events.length) {
    return;
  }
  const now = new Date().toISOString();
  const values = events
    .filter((event) => Boolean(event.targetId))
    .map((event) => [
      randomUUID(),
      requestId,
      abilityId,
      casterId,
      event.targetId,
      event.kind,
      event.amount ?? null,
      event.stateId ?? null,
      event.durationSeconds ?? null,
      now,
    ]);

  if (!values.length) {
    return;
  }

  const placeholders = values.map(() => '(?, ?, ?, ?, ?, ?, ?, ?, ?, ?)').join(',');
  await db.execute(
    `INSERT INTO combat_event_logs
      (id, request_id, ability_id, caster_id, target_id, event_kind, amount, state_id, duration_seconds, created_at)
     VALUES ${placeholders}`,
    values.flat(),
  );
}
