import {
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
  items: Array<Record<string, unknown>>;
  weapons: Array<Record<string, unknown>>;
  armor: Array<Record<string, unknown>>;
  classes: Array<Record<string, unknown>>;
  classBaseStats: Array<Record<string, unknown>>;
  enemies: Array<Record<string, unknown>>;
  enemyBaseStats: Array<Record<string, unknown>>;
  abilities: Array<Record<string, unknown>>;
  levelProgression: Array<Record<string, unknown>>;
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
