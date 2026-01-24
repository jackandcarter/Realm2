import { db, DbExecutor } from './database';

export interface RaceRecord {
  id: string;
  displayName: string;
  customizationJson: string;
  createdAt: string;
  updatedAt: string;
}

export interface RaceClassRuleRecord {
  raceId: string;
  classId: string;
  unlockMethod: string;
  createdAt: string;
  updatedAt: string;
}

export interface ClassWeaponProficiencyRecord {
  classId: string;
  weaponType: string;
  createdAt: string;
}

export async function listRaces(executor: DbExecutor = db): Promise<RaceRecord[]> {
  return executor.query<RaceRecord[]>(
    `SELECT id,
            display_name as displayName,
            customization_json as customizationJson,
            created_at as createdAt,
            updated_at as updatedAt
     FROM races
     ORDER BY id ASC`
  );
}

export async function listRaceClassRules(
  executor: DbExecutor = db
): Promise<RaceClassRuleRecord[]> {
  return executor.query<RaceClassRuleRecord[]>(
    `SELECT race_id as raceId,
            class_id as classId,
            unlock_method as unlockMethod,
            created_at as createdAt,
            updated_at as updatedAt
     FROM race_class_rules
     ORDER BY race_id ASC, class_id ASC`
  );
}

export async function listClassWeaponProficiencies(
  executor: DbExecutor = db
): Promise<ClassWeaponProficiencyRecord[]> {
  return executor.query<ClassWeaponProficiencyRecord[]>(
    `SELECT class_id as classId,
            weapon_type as weaponType,
            created_at as createdAt
     FROM class_weapon_proficiencies
     ORDER BY class_id ASC, weapon_type ASC`
  );
}
