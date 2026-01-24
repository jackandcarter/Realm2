import type { DbExecutor } from './database';
import {
  AbilityType,
  ArmorType,
  ClassRole,
  EquipmentSlot,
  ItemCategory,
  ItemRarity,
  WeaponHandedness,
  WeaponType,
} from '../config/gameEnums';
import { coreClassDefinitions, equipmentCatalog } from '../gameplay/design/systemFoundations';
import { generatedAbilityDefinitions } from '../gameplay/combat/generated/abilityRegistry';

interface ItemSeed {
  id: string;
  name: string;
  description: string | null;
  category: ItemCategory;
  rarity: ItemRarity;
  stackLimit: number;
  iconUrl: string | null;
  metadata: Record<string, unknown>;
}

interface WeaponSeed {
  itemId: string;
  weaponType: WeaponType;
  handedness: WeaponHandedness;
  minDamage: number;
  maxDamage: number;
  attackSpeed: number;
  rangeMeters: number;
  requiredLevel: number;
  requiredClassId: string | null;
  metadata: Record<string, unknown>;
}

interface ArmorSeed {
  itemId: string;
  slot: EquipmentSlot;
  armorType: ArmorType;
  defense: number;
  resistances: Record<string, number>;
  requiredLevel: number;
  requiredClassId: string | null;
  metadata: Record<string, unknown>;
}

interface ClassSeed {
  id: string;
  name: string;
  role: ClassRole | null;
  metadata: Record<string, unknown>;
}

interface AbilitySeed {
  id: string;
  name: string;
  description: string | null;
  abilityType: AbilityType;
  rangeMeters: number;
  metadata: Record<string, unknown>;
}

interface LevelSeed {
  level: number;
  xpRequired: number;
  totalXp: number;
  hpGain: number;
  manaGain: number;
  statPoints: number;
  reward: Record<string, unknown>;
}

function serialize(value: unknown): string {
  return JSON.stringify(value ?? {});
}

function buildItemSeeds(): { items: ItemSeed[]; weapons: WeaponSeed[]; armor: ArmorSeed[] } {
  const items: ItemSeed[] = [];
  const weapons: WeaponSeed[] = [];
  const armor: ArmorSeed[] = [];

  for (const entry of equipmentCatalog) {
    items.push({
      id: entry.id,
      name: entry.name,
      description: null,
      category: entry.category,
      rarity: entry.tier,
      stackLimit: entry.category === 'consumable' ? 20 : 1,
      iconUrl: null,
      metadata: {
        baseStats: entry.baseStats,
        slot: entry.slot,
        subtype: entry.subtype,
        requiredClassIds: entry.requiredClassIds ?? [],
        tier: entry.tier,
      },
    });

    if (entry.category === 'weapon') {
      weapons.push({
        itemId: entry.id,
        weaponType: (entry.subtype as WeaponType) ?? 'unknown',
        handedness: 'one-hand',
        minDamage: 0,
        maxDamage: 0,
        attackSpeed: 1,
        rangeMeters: 1,
        requiredLevel: 1,
        requiredClassId:
          entry.requiredClassIds && entry.requiredClassIds.length === 1
            ? entry.requiredClassIds[0] ?? null
            : null,
        metadata: {
          baseStats: entry.baseStats,
          requiredClassIds: entry.requiredClassIds ?? [],
          tier: entry.tier,
        },
      });
    }

    if (entry.category === 'armor') {
      armor.push({
        itemId: entry.id,
        slot: entry.slot,
        armorType: (entry.subtype as ArmorType) ?? 'cloth',
        defense: 0,
        resistances: {},
        requiredLevel: 1,
        requiredClassId:
          entry.requiredClassIds && entry.requiredClassIds.length === 1
            ? entry.requiredClassIds[0] ?? null
            : null,
        metadata: {
          baseStats: entry.baseStats,
          requiredClassIds: entry.requiredClassIds ?? [],
          tier: entry.tier,
        },
      });
    }
  }

  return { items, weapons, armor };
}

function buildClassSeeds(): { classes: ClassSeed[] } {
  return {
    classes: coreClassDefinitions.map((entry) => ({
      id: entry.id,
      name: entry.name,
      role: entry.role,
      metadata: {
        primaryStats: entry.primaryStats,
        weaponProficiencies: entry.weaponProficiencies,
        signatureAbilities: entry.signatureAbilities,
        unlockQuestId: entry.unlockQuestId ?? null,
      },
    })),
  };
}

function buildAbilitySeeds(): { abilities: AbilitySeed[] } {
  return {
    abilities: generatedAbilityDefinitions.map((entry) => ({
      id: entry.id,
      name: entry.name,
      description: entry.summary ?? null,
      abilityType: 'combat',
      rangeMeters: entry.delivery?.rangeMeters ?? 0,
      metadata: { ...entry } as Record<string, unknown>,
    })),
  };
}

function buildLevelSeeds(): LevelSeed[] {
  const levelCap = 60;
  const seeds: LevelSeed[] = [];
  let totalXp = 0;

  for (let level = 1; level <= levelCap; level += 1) {
    const xpRequired = level === 1 ? 0 : Math.round(75 * Math.pow(level, 2));
    totalXp += xpRequired;
    const hpGain = level === 1 ? 0 : Math.round(8 + level * 1.6);
    const manaGain = level === 1 ? 0 : Math.round(5 + level * 1.2);
    const statPoints = level === 1 ? 0 : 2 + Math.floor(level / 10);

    seeds.push({
      level,
      xpRequired,
      totalXp,
      hpGain,
      manaGain,
      statPoints,
      reward: {},
    });
  }

  return seeds;
}

export async function seedCatalogData(db: DbExecutor): Promise<void> {
  const now = new Date().toISOString();
  const { items, weapons, armor } = buildItemSeeds();
  const { classes } = buildClassSeeds();
  const { abilities } = buildAbilitySeeds();
  const levelSeeds = buildLevelSeeds();

  for (const item of items) {
    await db.execute(
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
        item.id,
        item.name,
        item.description,
        item.category,
        item.rarity,
        item.stackLimit,
        item.iconUrl,
        serialize(item.metadata),
        now,
        now,
      ]
    );
  }

  for (const weapon of weapons) {
    await db.execute(
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
        weapon.itemId,
        weapon.weaponType,
        weapon.handedness,
        weapon.minDamage,
        weapon.maxDamage,
        weapon.attackSpeed,
        weapon.rangeMeters,
        weapon.requiredLevel,
        weapon.requiredClassId,
        serialize(weapon.metadata),
      ]
    );
  }

  for (const item of armor) {
    await db.execute(
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
        item.itemId,
        item.slot,
        item.armorType,
        item.defense,
        serialize(item.resistances),
        item.requiredLevel,
        item.requiredClassId,
        serialize(item.metadata),
      ]
    );
  }

  for (const entry of classes) {
    await db.execute(
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
        entry.id,
        entry.name,
        null,
        entry.role,
        null,
        1,
        serialize(entry.metadata),
        now,
        now,
      ]
    );

    await db.execute(
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
      [entry.id, 0, 0, 0, 0, 0, 0, 0, 0, 0, now]
    );
  }

  for (const ability of abilities) {
    await db.execute(
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
        ability.id,
        ability.name,
        ability.description,
        ability.abilityType,
        0,
        0,
        ability.rangeMeters,
        0,
        serialize(ability.metadata),
        now,
        now,
      ]
    );
  }

  for (const level of levelSeeds) {
    await db.execute(
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
        level.level,
        level.xpRequired,
        level.totalXp,
        level.hpGain,
        level.manaGain,
        level.statPoints,
        serialize(level.reward),
        now,
      ]
    );
  }
}
