import { db, DbExecutor } from './database';

export interface WeaponTypeRecord {
  id: string;
  displayName: string;
  createdAt: string;
  updatedAt: string;
}

export interface ResourceTypeRecord {
  id: string;
  displayName: string;
  category: string;
  createdAt: string;
  updatedAt: string;
}

export interface AbilityTypeRecord {
  id: string;
  displayName: string;
  createdAt: string;
  updatedAt: string;
}

export async function listWeaponTypes(executor: DbExecutor = db): Promise<WeaponTypeRecord[]> {
  return executor.query<WeaponTypeRecord[]>(
    `SELECT id,
            display_name as displayName,
            created_at as createdAt,
            updated_at as updatedAt
     FROM weapon_types
     ORDER BY id ASC`
  );
}

export async function getWeaponTypeById(
  id: string,
  executor: DbExecutor = db
): Promise<WeaponTypeRecord | undefined> {
  const rows = await executor.query<WeaponTypeRecord[]>(
    `SELECT id,
            display_name as displayName,
            created_at as createdAt,
            updated_at as updatedAt
     FROM weapon_types
     WHERE id = ?`,
    [id]
  );
  return rows[0];
}

export async function listResourceTypes(
  executor: DbExecutor = db
): Promise<ResourceTypeRecord[]> {
  return executor.query<ResourceTypeRecord[]>(
    `SELECT id,
            display_name as displayName,
            category,
            created_at as createdAt,
            updated_at as updatedAt
     FROM resource_types
     ORDER BY id ASC`
  );
}

export async function listAbilityTypes(
  executor: DbExecutor = db
): Promise<AbilityTypeRecord[]> {
  return executor.query<AbilityTypeRecord[]>(
    `SELECT id,
            display_name as displayName,
            created_at as createdAt,
            updated_at as updatedAt
     FROM ability_types
     ORDER BY id ASC`
  );
}

export async function getAbilityTypeById(
  id: string,
  executor: DbExecutor = db
): Promise<AbilityTypeRecord | undefined> {
  const rows = await executor.query<AbilityTypeRecord[]>(
    `SELECT id,
            display_name as displayName,
            created_at as createdAt,
            updated_at as updatedAt
     FROM ability_types
     WHERE id = ?`,
    [id]
  );
  return rows[0];
}
