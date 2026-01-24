import {
  AbilityType,
  ArmorType,
  ClassResourceType,
  ClassRole,
  EquipmentSlot,
  ItemCategory,
  ItemRarity,
  WeaponHandedness,
  WeaponType,
} from '../config/gameEnums';
import { db } from './database';

export interface ItemRecord {
  id: string;
  name: string;
  description: string | null;
  category: ItemCategory;
  rarity: ItemRarity;
  stackLimit: number;
  iconUrl: string | null;
  metadataJson: string;
  createdAt: string;
  updatedAt: string;
}

export interface WeaponRecord {
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

export interface ArmorRecord {
  itemId: string;
  slot: EquipmentSlot;
  armorType: ArmorType;
  defense: number;
  resistancesJson: string;
  requiredLevel: number;
  requiredClassId: string | null;
  metadataJson: string;
}

export interface ClassRecord {
  id: string;
  name: string;
  description: string | null;
  role: ClassRole | null;
  resourceType: ClassResourceType | null;
  startingLevel: number;
  metadataJson: string;
  createdAt: string;
  updatedAt: string;
}

export interface ClassBaseStatRecord {
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
  updatedAt: string;
}

export interface EnemyRecord {
  id: string;
  name: string;
  description: string | null;
  enemyType: string | null;
  level: number;
  faction: string | null;
  isBoss: number;
  metadataJson: string;
  createdAt: string;
  updatedAt: string;
}

export interface EnemyBaseStatRecord {
  enemyId: string;
  baseHealth: number;
  baseMana: number;
  attack: number;
  defense: number;
  agility: number;
  critChance: number;
  xpReward: number;
  goldReward: number;
  updatedAt: string;
}

export interface AbilityRecord {
  id: string;
  name: string;
  description: string | null;
  abilityType: AbilityType | null;
  cooldownSeconds: number;
  resourceCost: number;
  rangeMeters: number;
  castTimeSeconds: number;
  metadataJson: string;
  createdAt: string;
  updatedAt: string;
}

export interface LevelProgressionRecord {
  level: number;
  xpRequired: number;
  totalXp: number;
  hpGain: number;
  manaGain: number;
  statPoints: number;
  rewardJson: string;
  updatedAt: string;
}

export async function listItems(): Promise<ItemRecord[]> {
  return db.query<ItemRecord[]>(
    `SELECT id,
            name,
            description,
            category,
            rarity,
            stack_limit as stackLimit,
            icon_url as iconUrl,
            metadata_json as metadataJson,
            created_at as createdAt,
            updated_at as updatedAt
     FROM items
     ORDER BY name ASC`
  );
}

export async function getItemsByIds(itemIds: string[]): Promise<ItemRecord[]> {
  if (!itemIds.length) {
    return [];
  }
  const placeholders = itemIds.map(() => '?').join(',');
  return db.query<ItemRecord[]>(
    `SELECT id,
            name,
            description,
            category,
            rarity,
            stack_limit as stackLimit,
            icon_url as iconUrl,
            metadata_json as metadataJson,
            created_at as createdAt,
            updated_at as updatedAt
     FROM items
     WHERE id IN (${placeholders})`,
    itemIds
  );
}

export async function listWeapons(): Promise<WeaponRecord[]> {
  return db.query<WeaponRecord[]>(
    `SELECT item_id as itemId,
            weapon_type as weaponType,
            handedness,
            min_damage as minDamage,
            max_damage as maxDamage,
            attack_speed as attackSpeed,
            range_meters as rangeMeters,
            required_level as requiredLevel,
            required_class_id as requiredClassId,
            metadata_json as metadataJson
     FROM weapons
     ORDER BY item_id ASC`
  );
}

export async function listArmor(): Promise<ArmorRecord[]> {
  return db.query<ArmorRecord[]>(
    `SELECT item_id as itemId,
            slot,
            armor_type as armorType,
            defense,
            resistances_json as resistancesJson,
            required_level as requiredLevel,
            required_class_id as requiredClassId,
            metadata_json as metadataJson
     FROM armor
     ORDER BY item_id ASC`
  );
}

export async function listClasses(): Promise<ClassRecord[]> {
  return db.query<ClassRecord[]>(
    `SELECT id,
            name,
            description,
            role,
            resource_type as resourceType,
            starting_level as startingLevel,
            metadata_json as metadataJson,
            created_at as createdAt,
            updated_at as updatedAt
     FROM classes
     ORDER BY name ASC`
  );
}

export async function getClassById(classId: string): Promise<ClassRecord | undefined> {
  const rows = await db.query<ClassRecord[]>(
    `SELECT id,
            name,
            description,
            role,
            resource_type as resourceType,
            starting_level as startingLevel,
            metadata_json as metadataJson,
            created_at as createdAt,
            updated_at as updatedAt
     FROM classes
     WHERE id = ?`,
    [classId],
  );
  return rows[0];
}

export async function listClassBaseStats(): Promise<ClassBaseStatRecord[]> {
  return db.query<ClassBaseStatRecord[]>(
    `SELECT class_id as classId,
            base_health as baseHealth,
            base_mana as baseMana,
            strength,
            agility,
            intelligence,
            vitality,
            defense,
            crit_chance as critChance,
            speed,
            updated_at as updatedAt
     FROM class_base_stats
     ORDER BY class_id ASC`
  );
}

export async function getClassBaseStatsById(
  classId: string
): Promise<ClassBaseStatRecord | undefined> {
  const rows = await db.query<ClassBaseStatRecord[]>(
    `SELECT class_id as classId,
            base_health as baseHealth,
            base_mana as baseMana,
            strength,
            agility,
            intelligence,
            vitality,
            defense,
            crit_chance as critChance,
            speed,
            updated_at as updatedAt
     FROM class_base_stats
     WHERE class_id = ?`,
    [classId]
  );
  return rows[0];
}

export async function listEnemies(): Promise<EnemyRecord[]> {
  return db.query<EnemyRecord[]>(
    `SELECT id,
            name,
            description,
            enemy_type as enemyType,
            level,
            faction,
            is_boss as isBoss,
            metadata_json as metadataJson,
            created_at as createdAt,
            updated_at as updatedAt
     FROM enemies
     ORDER BY name ASC`
  );
}

export async function listEnemyBaseStats(): Promise<EnemyBaseStatRecord[]> {
  return db.query<EnemyBaseStatRecord[]>(
    `SELECT enemy_id as enemyId,
            base_health as baseHealth,
            base_mana as baseMana,
            attack,
            defense,
            agility,
            crit_chance as critChance,
            xp_reward as xpReward,
            gold_reward as goldReward,
            updated_at as updatedAt
     FROM enemy_base_stats
     ORDER BY enemy_id ASC`
  );
}

export async function listAbilities(): Promise<AbilityRecord[]> {
  return db.query<AbilityRecord[]>(
    `SELECT id,
            name,
            description,
            ability_type as abilityType,
            cooldown_seconds as cooldownSeconds,
            resource_cost as resourceCost,
            range_meters as rangeMeters,
            cast_time_seconds as castTimeSeconds,
            metadata_json as metadataJson,
            created_at as createdAt,
            updated_at as updatedAt
     FROM abilities
     ORDER BY name ASC`
  );
}

export async function getAbilityById(abilityId: string): Promise<AbilityRecord | undefined> {
  const rows = await db.query<AbilityRecord[]>(
    `SELECT id,
            name,
            description,
            ability_type as abilityType,
            cooldown_seconds as cooldownSeconds,
            resource_cost as resourceCost,
            range_meters as rangeMeters,
            cast_time_seconds as castTimeSeconds,
            metadata_json as metadataJson,
            created_at as createdAt,
            updated_at as updatedAt
     FROM abilities
     WHERE id = ?`,
    [abilityId]
  );
  return rows[0];
}

export async function listLevelProgression(): Promise<LevelProgressionRecord[]> {
  return db.query<LevelProgressionRecord[]>(
    `SELECT level,
            xp_required as xpRequired,
            total_xp as totalXp,
            hp_gain as hpGain,
            mana_gain as manaGain,
            stat_points as statPoints,
            reward_json as rewardJson,
            updated_at as updatedAt
     FROM level_progression
     ORDER BY level ASC`
  );
}
