import {
  armorTypes,
  classResourceTypes,
  classRoles,
  equipmentSlots,
  itemCategories,
  itemRarities,
  weaponHandedness,
} from '../config/gameEnums';
import {
  AbilityRecord,
  ArmorRecord,
  ClassBaseStatRecord,
  ClassRecord,
  ItemRecord,
  WeaponRecord,
  getAbilityById,
  getClassBaseStatsById,
  getClassById,
  getItemsByIds,
  getArmorByItemId,
  getWeaponByItemId,
  listAbilities,
  listArmor,
  listClassBaseStats,
  listClasses,
  listItems,
  listWeapons,
} from '../db/catalogRepository';
import {
  AbilityTypeRecord,
  WeaponTypeRecord,
  getAbilityTypeById,
  getWeaponTypeById,
  listAbilityTypes,
  listWeaponTypes,
} from '../db/referenceDataRepository';
import {
  upsertAbility,
  upsertAbilityType,
  upsertArmor,
  upsertClass,
  upsertClassBaseStats,
  upsertItem,
  upsertWeapon,
  upsertWeaponType,
} from '../db/devToolkitRepository';
import { HttpError } from '../utils/errors';

type RecordValue = Record<string, unknown>;

function ensureRecord(value: unknown, label: string): RecordValue {
  if (!value || typeof value !== 'object' || Array.isArray(value)) {
    throw new HttpError(400, `${label} must be an object`);
  }
  return value as RecordValue;
}

function ensureNonEmptyString(value: unknown, label: string): string {
  if (typeof value !== 'string' || !value.trim()) {
    throw new HttpError(400, `${label} is required`);
  }
  return value.trim();
}

function ensureOptionalString(value: unknown): string | null {
  if (typeof value !== 'string') {
    return null;
  }
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
}

function ensureNumber(value: unknown, label: string, fallback = 0): number {
  if (typeof value === 'undefined' || value === null) {
    return fallback;
  }
  if (!Number.isFinite(value)) {
    throw new HttpError(400, `${label} must be a number`);
  }
  return Number(value);
}

function ensureInteger(value: unknown, label: string, fallback = 0): number {
  const num = ensureNumber(value, label, fallback);
  if (!Number.isInteger(num)) {
    throw new HttpError(400, `${label} must be an integer`);
  }
  return num;
}

function ensureEnum<T extends readonly string[]>(
  value: unknown,
  allowed: T,
  label: string,
  fallback?: T[number]
): T[number] {
  if (typeof value === 'undefined' || value === null) {
    if (fallback !== undefined) {
      return fallback;
    }
    throw new HttpError(400, `${label} is required`);
  }
  if (typeof value !== 'string') {
    throw new HttpError(400, `${label} must be a string`);
  }
  const trimmed = value.trim();
  if (!allowed.includes(trimmed)) {
    throw new HttpError(400, `${label} must be one of ${allowed.join(', ')}`);
  }
  return trimmed as T[number];
}

function serializeJson(value: unknown, label: string): string {
  if (typeof value === 'string') {
    return value;
  }
  try {
    return JSON.stringify(value ?? {});
  } catch (_error) {
    throw new HttpError(400, `${label} must be JSON-serializable`);
  }
}

function parseJson(value: string): unknown {
  if (!value) {
    return {};
  }
  try {
    return JSON.parse(value);
  } catch {
    return {};
  }
}

function ensureOptionalId(value: unknown, label: string): string | null {
  if (typeof value !== 'string') {
    return null;
  }
  const trimmed = value.trim();
  if (!trimmed) {
    return null;
  }
  if (trimmed.length < 2) {
    throw new HttpError(400, `${label} must be at least 2 characters`);
  }
  return trimmed;
}

export async function listDevToolkitItems(): Promise<Array<ItemRecord & { metadata: unknown }>> {
  const items = await listItems();
  return items.map((item) => ({ ...item, metadata: parseJson(item.metadataJson) }));
}

export async function listDevToolkitWeapons(): Promise<Array<WeaponRecord & { metadata: unknown }>> {
  const weapons = await listWeapons();
  return weapons.map((weapon) => ({ ...weapon, metadata: parseJson(weapon.metadataJson) }));
}

export async function listDevToolkitArmor(): Promise<
  Array<ArmorRecord & { resistances: unknown; metadata: unknown }>
> {
  const armor = await listArmor();
  return armor.map((entry) => ({
    ...entry,
    resistances: parseJson(entry.resistancesJson),
    metadata: parseJson(entry.metadataJson),
  }));
}

export async function listDevToolkitClasses(): Promise<
  Array<ClassRecord & { metadata: unknown }>
> {
  const classes = await listClasses();
  return classes.map((entry) => ({ ...entry, metadata: parseJson(entry.metadataJson) }));
}

export async function listDevToolkitClassBaseStats(): Promise<ClassBaseStatRecord[]> {
  return listClassBaseStats();
}

export async function listDevToolkitAbilities(): Promise<
  Array<AbilityRecord & { metadata: unknown }>
> {
  const abilities = await listAbilities();
  return abilities.map((entry) => ({ ...entry, metadata: parseJson(entry.metadataJson) }));
}

export async function listDevToolkitWeaponTypes(): Promise<WeaponTypeRecord[]> {
  return listWeaponTypes();
}

export async function listDevToolkitAbilityTypes(): Promise<AbilityTypeRecord[]> {
  return listAbilityTypes();
}

export async function saveItem(input: unknown): Promise<ItemRecord & { metadata: unknown }> {
  const payload = ensureRecord(input, 'item');
  const id = ensureNonEmptyString(payload.id, 'item.id');
  const name = ensureNonEmptyString(payload.name, 'item.name');
  const description = ensureOptionalString(payload.description);
  const category = ensureEnum(payload.category, itemCategories, 'item.category');
  const rarity = ensureEnum(payload.rarity, itemRarities, 'item.rarity', 'common');
  const stackLimit = ensureInteger(payload.stackLimit, 'item.stackLimit', 1);
  if (stackLimit < 1) {
    throw new HttpError(400, 'item.stackLimit must be at least 1');
  }
  const iconUrl = ensureOptionalString(payload.iconUrl);
  const metadataJson = serializeJson(payload.metadata, 'item.metadata');

  await upsertItem({
    id,
    name,
    description,
    category,
    rarity,
    stackLimit,
    iconUrl,
    metadataJson,
  });

  const item = (await getItemsByIds([id]))[0];
  if (!item) {
    throw new HttpError(404, 'item not found after save');
  }
  return { ...item, metadata: parseJson(item.metadataJson) };
}

export async function saveWeaponType(input: unknown): Promise<WeaponTypeRecord> {
  const payload = ensureRecord(input, 'weaponType');
  const id = ensureNonEmptyString(payload.id, 'weaponType.id');
  const displayName = ensureNonEmptyString(payload.displayName, 'weaponType.displayName');
  await upsertWeaponType({ id, displayName });
  const record = await getWeaponTypeById(id);
  if (!record) {
    throw new HttpError(404, 'weapon type not found after save');
  }
  return record;
}

export async function saveAbilityType(input: unknown): Promise<AbilityTypeRecord> {
  const payload = ensureRecord(input, 'abilityType');
  const id = ensureNonEmptyString(payload.id, 'abilityType.id');
  const displayName = ensureNonEmptyString(payload.displayName, 'abilityType.displayName');
  await upsertAbilityType({ id, displayName });
  const record = await getAbilityTypeById(id);
  if (!record) {
    throw new HttpError(404, 'ability type not found after save');
  }
  return record;
}

export async function saveWeapon(
  input: unknown
): Promise<WeaponRecord & { metadata: unknown }> {
  const payload = ensureRecord(input, 'weapon');
  const itemId = ensureNonEmptyString(payload.itemId, 'weapon.itemId');
  const weaponType = ensureNonEmptyString(payload.weaponType, 'weapon.weaponType');
  const handedness = ensureEnum(payload.handedness, weaponHandedness, 'weapon.handedness', 'one-hand');
  const minDamage = ensureInteger(payload.minDamage, 'weapon.minDamage', 0);
  const maxDamage = ensureInteger(payload.maxDamage, 'weapon.maxDamage', 0);
  const attackSpeed = ensureNumber(payload.attackSpeed, 'weapon.attackSpeed', 1);
  const rangeMeters = ensureNumber(payload.rangeMeters, 'weapon.rangeMeters', 1);
  const requiredLevel = ensureInteger(payload.requiredLevel, 'weapon.requiredLevel', 1);
  const requiredClassId = ensureOptionalId(payload.requiredClassId, 'weapon.requiredClassId');
  const metadataJson = serializeJson(payload.metadata, 'weapon.metadata');

  await upsertWeapon({
    itemId,
    weaponType,
    handedness,
    minDamage,
    maxDamage,
    attackSpeed,
    rangeMeters,
    requiredLevel,
    requiredClassId,
    metadataJson,
  });

  const weapon = await getWeaponByItemId(itemId);
  if (!weapon) {
    throw new HttpError(404, 'weapon not found after save');
  }
  return { ...weapon, metadata: parseJson(weapon.metadataJson) };
}

export async function saveArmor(
  input: unknown
): Promise<ArmorRecord & { resistances: unknown; metadata: unknown }> {
  const payload = ensureRecord(input, 'armor');
  const itemId = ensureNonEmptyString(payload.itemId, 'armor.itemId');
  const slot = ensureEnum(payload.slot, equipmentSlots, 'armor.slot');
  const armorType = ensureEnum(payload.armorType, armorTypes, 'armor.armorType');
  const defense = ensureInteger(payload.defense, 'armor.defense', 0);
  const resistancesJson = serializeJson(payload.resistances, 'armor.resistances');
  const requiredLevel = ensureInteger(payload.requiredLevel, 'armor.requiredLevel', 1);
  const requiredClassId = ensureOptionalId(payload.requiredClassId, 'armor.requiredClassId');
  const metadataJson = serializeJson(payload.metadata, 'armor.metadata');

  await upsertArmor({
    itemId,
    slot,
    armorType,
    defense,
    resistancesJson,
    requiredLevel,
    requiredClassId,
    metadataJson,
  });

  const armor = await getArmorByItemId(itemId);
  if (!armor) {
    throw new HttpError(404, 'armor not found after save');
  }
  return {
    ...armor,
    resistances: parseJson(armor.resistancesJson),
    metadata: parseJson(armor.metadataJson),
  };
}

export async function saveClass(input: unknown): Promise<ClassRecord & { metadata: unknown }> {
  const payload = ensureRecord(input, 'class');
  const id = ensureNonEmptyString(payload.id, 'class.id');
  const name = ensureNonEmptyString(payload.name, 'class.name');
  const description = ensureOptionalString(payload.description);
  const role = payload.role ? ensureEnum(payload.role, classRoles, 'class.role') : null;
  const resourceType = payload.resourceType
    ? ensureEnum(payload.resourceType, classResourceTypes, 'class.resourceType')
    : null;
  const startingLevel = ensureInteger(payload.startingLevel, 'class.startingLevel', 1);
  const metadataJson = serializeJson(payload.metadata, 'class.metadata');

  await upsertClass({
    id,
    name,
    description,
    role,
    resourceType,
    startingLevel,
    metadataJson,
  });

  const record = await getClassById(id);
  if (!record) {
    throw new HttpError(404, 'class not found after save');
  }
  return { ...record, metadata: parseJson(record.metadataJson) };
}

export async function saveClassBaseStats(
  input: unknown
): Promise<ClassBaseStatRecord> {
  const payload = ensureRecord(input, 'classBaseStats');
  const classId = ensureNonEmptyString(payload.classId, 'classBaseStats.classId');
  const baseHealth = ensureInteger(payload.baseHealth, 'classBaseStats.baseHealth', 0);
  const baseMana = ensureInteger(payload.baseMana, 'classBaseStats.baseMana', 0);
  const strength = ensureInteger(payload.strength, 'classBaseStats.strength', 0);
  const agility = ensureInteger(payload.agility, 'classBaseStats.agility', 0);
  const intelligence = ensureInteger(payload.intelligence, 'classBaseStats.intelligence', 0);
  const vitality = ensureInteger(payload.vitality, 'classBaseStats.vitality', 0);
  const defense = ensureInteger(payload.defense, 'classBaseStats.defense', 0);
  const critChance = ensureNumber(payload.critChance, 'classBaseStats.critChance', 0);
  const speed = ensureNumber(payload.speed, 'classBaseStats.speed', 0);

  await upsertClassBaseStats({
    classId,
    baseHealth,
    baseMana,
    strength,
    agility,
    intelligence,
    vitality,
    defense,
    critChance,
    speed,
  });

  const record = await getClassBaseStatsById(classId);
  if (!record) {
    throw new HttpError(404, 'class base stats not found after save');
  }
  return record;
}

export async function saveAbility(
  input: unknown
): Promise<AbilityRecord & { metadata: unknown }> {
  const payload = ensureRecord(input, 'ability');
  const id = ensureNonEmptyString(payload.id, 'ability.id');
  const name = ensureNonEmptyString(payload.name, 'ability.name');
  const description = ensureOptionalString(payload.description);
  const abilityType = ensureOptionalId(payload.abilityType, 'ability.abilityType');
  const cooldownSeconds = ensureNumber(payload.cooldownSeconds, 'ability.cooldownSeconds', 0);
  const resourceCost = ensureInteger(payload.resourceCost, 'ability.resourceCost', 0);
  const rangeMeters = ensureNumber(payload.rangeMeters, 'ability.rangeMeters', 0);
  const castTimeSeconds = ensureNumber(payload.castTimeSeconds, 'ability.castTimeSeconds', 0);
  const metadataJson = serializeJson(payload.metadata, 'ability.metadata');

  await upsertAbility({
    id,
    name,
    description,
    abilityType,
    cooldownSeconds,
    resourceCost,
    rangeMeters,
    castTimeSeconds,
    metadataJson,
  });

  const record = await getAbilityById(id);
  if (!record) {
    throw new HttpError(404, 'ability not found after save');
  }
  return { ...record, metadata: parseJson(record.metadataJson) };
}
