import {
  listAbilityTypes,
  listResourceTypes,
  listWeaponTypes,
} from '../db/referenceDataRepository';

export async function listWeaponTypeIds(): Promise<string[]> {
  const rows = await listWeaponTypes();
  return rows.map((row) => row.id);
}

export async function listResourceTypeIds(): Promise<string[]> {
  const rows = await listResourceTypes();
  return rows.map((row) => row.id);
}

export async function listAbilityTypeIds(): Promise<string[]> {
  const rows = await listAbilityTypes();
  return rows.map((row) => row.id);
}
