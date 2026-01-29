import {
  ArmorType,
  ClassResourceType,
  ClassRole,
  EquipmentSlot,
  ItemCategory,
  ItemRarity,
  WeaponHandedness,
  WeaponType,
} from '../config/gameEnums';
import { db, DbExecutor } from './database';

export interface ItemInput {
  id: string;
  name: string;
  description: string | null;
  category: ItemCategory;
  rarity: ItemRarity;
  stackLimit: number;
  iconUrl: string | null;
  metadataJson: string;
}

export interface WeaponInput {
  itemId: string;
  weaponType: WeaponType;
  handedness: WeaponHandedness;
  minDamage: number;
  maxDamage: number;
  attackSpeed: number;
  rangeMeters: number;
  requiredLevel: number;
  requiredClassId: string | null;
  metadataJson: string;
}

export interface ArmorInput {
  itemId: string;
  slot: EquipmentSlot;
  armorType: ArmorType;
  defense: number;
  resistancesJson: string;
  requiredLevel: number;
  requiredClassId: string | null;
  metadataJson: string;
}

export interface ClassInput {
  id: string;
  name: string;
  description: string | null;
  role: ClassRole | null;
  resourceType: ClassResourceType | null;
  startingLevel: number;
  metadataJson: string;
}

export interface ClassBaseStatInput {
  classId: string;
  baseHealth: number;
  baseMana: number;
  strength: number;
  agility: number;
  intelligence: number;
  vitality: number;
  defense: number;
  critChance: number;
  speed: number;
}

export interface AbilityInput {
  id: string;
  name: string;
  description: string | null;
  abilityType: string | null;
  cooldownSeconds: number;
  resourceCost: number;
  rangeMeters: number;
  castTimeSeconds: number;
  metadataJson: string;
}

export interface NamedTypeInput {
  id: string;
  displayName: string;
}

export interface ResourceTypeInput {
  id: string;
  displayName: string;
  category: string;
}

export interface RaceInput {
  id: string;
  displayName: string;
  customizationJson: string;
}

export interface EnemyInput {
  id: string;
  name: string;
  description: string | null;
  enemyType: string | null;
  level: number;
  faction: string | null;
  isBoss: boolean;
  metadataJson: string;
}

export interface EnemyBaseStatInput {
  enemyId: string;
  baseHealth: number;
  baseMana: number;
  attack: number;
  defense: number;
  agility: number;
  critChance: number;
  xpReward: number;
  goldReward: number;
}

export interface LevelProgressionInput {
  level: number;
  xpRequired: number;
  totalXp: number;
  hpGain: number;
  manaGain: number;
  statPoints: number;
  rewardJson: string;
}

function nowIso(): string {
  return new Date().toISOString();
}

export async function upsertItem(
  input: ItemInput,
  executor: DbExecutor = db
): Promise<void> {
  const now = nowIso();
  await executor.execute(
    `INSERT INTO items (id, name, description, category, rarity, stack_limit, icon_url, metadata_json, created_at, updated_at)
     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
     ON DUPLICATE KEY UPDATE
       name = VALUES(name),
       description = VALUES(description),
       category = VALUES(category),
       rarity = VALUES(rarity),
       stack_limit = VALUES(stack_limit),
       icon_url = VALUES(icon_url),
       metadata_json = VALUES(metadata_json),
       updated_at = VALUES(updated_at)`,
    [
      input.id,
      input.name,
      input.description,
      input.category,
      input.rarity,
      input.stackLimit,
      input.iconUrl,
      input.metadataJson,
      now,
      now,
    ]
  );
}

export async function upsertWeaponType(
  input: NamedTypeInput,
  executor: DbExecutor = db
): Promise<void> {
  const now = nowIso();
  await executor.execute(
    `INSERT INTO weapon_types (id, display_name, created_at, updated_at)
     VALUES (?, ?, ?, ?)
     ON DUPLICATE KEY UPDATE
       display_name = VALUES(display_name),
       updated_at = VALUES(updated_at)`,
    [input.id, input.displayName, now, now]
  );
}

export async function upsertAbilityType(
  input: NamedTypeInput,
  executor: DbExecutor = db
): Promise<void> {
  const now = nowIso();
  await executor.execute(
    `INSERT INTO ability_types (id, display_name, created_at, updated_at)
     VALUES (?, ?, ?, ?)
     ON DUPLICATE KEY UPDATE
       display_name = VALUES(display_name),
       updated_at = VALUES(updated_at)`,
    [input.id, input.displayName, now, now]
  );
}

export async function upsertWeapon(
  input: WeaponInput,
  executor: DbExecutor = db
): Promise<void> {
  await executor.execute(
    `INSERT INTO weapons (item_id, weapon_type, handedness, min_damage, max_damage, attack_speed, range_meters, required_level, required_class_id, metadata_json)
     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
     ON DUPLICATE KEY UPDATE
       weapon_type = VALUES(weapon_type),
       handedness = VALUES(handedness),
       min_damage = VALUES(min_damage),
       max_damage = VALUES(max_damage),
       attack_speed = VALUES(attack_speed),
       range_meters = VALUES(range_meters),
       required_level = VALUES(required_level),
       required_class_id = VALUES(required_class_id),
       metadata_json = VALUES(metadata_json)`,
    [
      input.itemId,
      input.weaponType,
      input.handedness,
      input.minDamage,
      input.maxDamage,
      input.attackSpeed,
      input.rangeMeters,
      input.requiredLevel,
      input.requiredClassId,
      input.metadataJson,
    ]
  );
}

export async function upsertArmor(
  input: ArmorInput,
  executor: DbExecutor = db
): Promise<void> {
  await executor.execute(
    `INSERT INTO armor (item_id, slot, armor_type, defense, resistances_json, required_level, required_class_id, metadata_json)
     VALUES (?, ?, ?, ?, ?, ?, ?, ?)
     ON DUPLICATE KEY UPDATE
       slot = VALUES(slot),
       armor_type = VALUES(armor_type),
       defense = VALUES(defense),
       resistances_json = VALUES(resistances_json),
       required_level = VALUES(required_level),
       required_class_id = VALUES(required_class_id),
       metadata_json = VALUES(metadata_json)`,
    [
      input.itemId,
      input.slot,
      input.armorType,
      input.defense,
      input.resistancesJson,
      input.requiredLevel,
      input.requiredClassId,
      input.metadataJson,
    ]
  );
}

export async function upsertClass(
  input: ClassInput,
  executor: DbExecutor = db
): Promise<void> {
  const now = nowIso();
  await executor.execute(
    `INSERT INTO classes (id, name, description, role, resource_type, starting_level, metadata_json, created_at, updated_at)
     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
     ON DUPLICATE KEY UPDATE
       name = VALUES(name),
       description = VALUES(description),
       role = VALUES(role),
       resource_type = VALUES(resource_type),
       starting_level = VALUES(starting_level),
       metadata_json = VALUES(metadata_json),
       updated_at = VALUES(updated_at)`,
    [
      input.id,
      input.name,
      input.description,
      input.role,
      input.resourceType,
      input.startingLevel,
      input.metadataJson,
      now,
      now,
    ]
  );
}

export async function upsertClassBaseStats(
  input: ClassBaseStatInput,
  executor: DbExecutor = db
): Promise<void> {
  const now = nowIso();
  await executor.execute(
    `INSERT INTO class_base_stats (class_id, base_health, base_mana, strength, agility, intelligence, vitality, defense, crit_chance, speed, updated_at)
     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
     ON DUPLICATE KEY UPDATE
       base_health = VALUES(base_health),
       base_mana = VALUES(base_mana),
       strength = VALUES(strength),
       agility = VALUES(agility),
       intelligence = VALUES(intelligence),
       vitality = VALUES(vitality),
       defense = VALUES(defense),
       crit_chance = VALUES(crit_chance),
       speed = VALUES(speed),
       updated_at = VALUES(updated_at)`,
    [
      input.classId,
      input.baseHealth,
      input.baseMana,
      input.strength,
      input.agility,
      input.intelligence,
      input.vitality,
      input.defense,
      input.critChance,
      input.speed,
      now,
    ]
  );
}

export async function upsertAbility(
  input: AbilityInput,
  executor: DbExecutor = db
): Promise<void> {
  const now = nowIso();
  await executor.execute(
    `INSERT INTO abilities (id, name, description, ability_type, cooldown_seconds, resource_cost, range_meters, cast_time_seconds, metadata_json, created_at, updated_at)
     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
     ON DUPLICATE KEY UPDATE
       name = VALUES(name),
       description = VALUES(description),
       ability_type = VALUES(ability_type),
       cooldown_seconds = VALUES(cooldown_seconds),
       resource_cost = VALUES(resource_cost),
       range_meters = VALUES(range_meters),
       cast_time_seconds = VALUES(cast_time_seconds),
       metadata_json = VALUES(metadata_json),
       updated_at = VALUES(updated_at)`,
    [
      input.id,
      input.name,
      input.description,
      input.abilityType,
      input.cooldownSeconds,
      input.resourceCost,
      input.rangeMeters,
      input.castTimeSeconds,
      input.metadataJson,
      now,
      now,
    ]
  );
}

export async function upsertResourceType(
  input: ResourceTypeInput,
  executor: DbExecutor = db
): Promise<void> {
  const now = nowIso();
  await executor.execute(
    `INSERT INTO resource_types (id, display_name, category, created_at, updated_at)
     VALUES (?, ?, ?, ?, ?)
     ON DUPLICATE KEY UPDATE
       display_name = VALUES(display_name),
       category = VALUES(category),
       updated_at = VALUES(updated_at)`,
    [input.id, input.displayName, input.category, now, now]
  );
}

export async function upsertRace(
  input: RaceInput,
  executor: DbExecutor = db
): Promise<void> {
  const now = nowIso();
  await executor.execute(
    `INSERT INTO races (id, display_name, customization_json, created_at, updated_at)
     VALUES (?, ?, ?, ?, ?)
     ON DUPLICATE KEY UPDATE
       display_name = VALUES(display_name),
       customization_json = VALUES(customization_json),
       updated_at = VALUES(updated_at)`,
    [input.id, input.displayName, input.customizationJson, now, now]
  );
}

export async function upsertEnemy(
  input: EnemyInput,
  executor: DbExecutor = db
): Promise<void> {
  const now = nowIso();
  await executor.execute(
    `INSERT INTO enemies (id, name, description, enemy_type, level, faction, is_boss, metadata_json, created_at, updated_at)
     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
     ON DUPLICATE KEY UPDATE
       name = VALUES(name),
       description = VALUES(description),
       enemy_type = VALUES(enemy_type),
       level = VALUES(level),
       faction = VALUES(faction),
       is_boss = VALUES(is_boss),
       metadata_json = VALUES(metadata_json),
       updated_at = VALUES(updated_at)`,
    [
      input.id,
      input.name,
      input.description,
      input.enemyType,
      input.level,
      input.faction,
      input.isBoss ? 1 : 0,
      input.metadataJson,
      now,
      now,
    ]
  );
}

export async function upsertEnemyBaseStats(
  input: EnemyBaseStatInput,
  executor: DbExecutor = db
): Promise<void> {
  const now = nowIso();
  await executor.execute(
    `INSERT INTO enemy_base_stats (enemy_id, base_health, base_mana, attack, defense, agility, crit_chance, xp_reward, gold_reward, updated_at)
     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
     ON DUPLICATE KEY UPDATE
       base_health = VALUES(base_health),
       base_mana = VALUES(base_mana),
       attack = VALUES(attack),
       defense = VALUES(defense),
       agility = VALUES(agility),
       crit_chance = VALUES(crit_chance),
       xp_reward = VALUES(xp_reward),
       gold_reward = VALUES(gold_reward),
       updated_at = VALUES(updated_at)`,
    [
      input.enemyId,
      input.baseHealth,
      input.baseMana,
      input.attack,
      input.defense,
      input.agility,
      input.critChance,
      input.xpReward,
      input.goldReward,
      now,
    ]
  );
}

export async function upsertLevelProgression(
  input: LevelProgressionInput,
  executor: DbExecutor = db
): Promise<void> {
  const now = nowIso();
  await executor.execute(
    `INSERT INTO level_progression (level, xp_required, total_xp, hp_gain, mana_gain, stat_points, reward_json, updated_at)
     VALUES (?, ?, ?, ?, ?, ?, ?, ?)
     ON DUPLICATE KEY UPDATE
       xp_required = VALUES(xp_required),
       total_xp = VALUES(total_xp),
       hp_gain = VALUES(hp_gain),
       mana_gain = VALUES(mana_gain),
       stat_points = VALUES(stat_points),
       reward_json = VALUES(reward_json),
       updated_at = VALUES(updated_at)`,
    [
      input.level,
      input.xpRequired,
      input.totalXp,
      input.hpGain,
      input.manaGain,
      input.statPoints,
      input.rewardJson,
      now,
    ]
  );
}
