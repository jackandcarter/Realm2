import {
  AbilityRecord,
  ArmorRecord,
  ClassBaseStatRecord,
  ClassRecord,
  EnemyBaseStatRecord,
  EnemyRecord,
  ItemRecord,
  LevelProgressionRecord,
  WeaponRecord,
  listAbilities,
  listArmor,
  listClassBaseStats,
  listClasses,
  listEnemies,
  listEnemyBaseStats,
  listItems,
  listLevelProgression,
  listWeapons,
} from '../db/catalogRepository';

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

export interface CatalogSnapshot {
  items: Array<ItemRecord & { metadata: unknown }>;
  weapons: Array<WeaponRecord & { metadata: unknown }>;
  armor: Array<ArmorRecord & { resistances: unknown; metadata: unknown }>;
  classes: Array<ClassRecord & { metadata: unknown }>;
  classBaseStats: ClassBaseStatRecord[];
  enemies: Array<EnemyRecord & { metadata: unknown }>;
  enemyBaseStats: EnemyBaseStatRecord[];
  abilities: Array<AbilityRecord & { metadata: unknown }>;
  levelProgression: Array<LevelProgressionRecord & { reward: unknown }>;
}

export async function getCatalogSnapshot(): Promise<CatalogSnapshot> {
  const [items, weapons, armor, classes, classBaseStats, enemies, enemyBaseStats, abilities, levels] =
    await Promise.all([
      listItems(),
      listWeapons(),
      listArmor(),
      listClasses(),
      listClassBaseStats(),
      listEnemies(),
      listEnemyBaseStats(),
      listAbilities(),
      listLevelProgression(),
    ]);

  return {
    items: items.map((item) => ({
      ...item,
      metadata: parseJson(item.metadataJson),
    })),
    weapons: weapons.map((weapon) => ({
      ...weapon,
      metadata: parseJson(weapon.metadataJson),
    })),
    armor: armor.map((entry) => ({
      ...entry,
      resistances: parseJson(entry.resistancesJson),
      metadata: parseJson(entry.metadataJson),
    })),
    classes: classes.map((entry) => ({
      ...entry,
      metadata: parseJson(entry.metadataJson),
    })),
    classBaseStats: classBaseStats,
    enemies: enemies.map((entry) => ({
      ...entry,
      metadata: parseJson(entry.metadataJson),
    })),
    enemyBaseStats,
    abilities: abilities.map((entry) => ({
      ...entry,
      metadata: parseJson(entry.metadataJson),
    })),
    levelProgression: levels.map((entry) => ({
      ...entry,
      reward: parseJson(entry.rewardJson),
    })),
  };
}
