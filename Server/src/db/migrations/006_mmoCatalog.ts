import type { DbExecutor } from '../database';

async function indexExists(db: DbExecutor, table: string, index: string): Promise<boolean> {
  const rows = await db.query<{ total: number }[]>(
    `SELECT COUNT(*) as total
     FROM information_schema.statistics
     WHERE table_schema = DATABASE() AND table_name = ? AND index_name = ?`,
    [table, index]
  );
  return (rows[0]?.total ?? 0) > 0;
}

export async function up(db: DbExecutor): Promise<void> {
  await db.execute(
    `CREATE TABLE IF NOT EXISTS items (
      id VARCHAR(64) PRIMARY KEY,
      name VARCHAR(120) NOT NULL,
      description TEXT,
      category VARCHAR(32) NOT NULL,
      rarity VARCHAR(32) NOT NULL DEFAULT 'common',
      stack_limit INT NOT NULL DEFAULT 1,
      icon_url VARCHAR(255),
      metadata_json LONGTEXT NOT NULL DEFAULT '{}',
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL
    ) ENGINE=InnoDB;`
  );

  if (!(await indexExists(db, 'items', 'idx_items_category'))) {
    await db.execute('CREATE INDEX idx_items_category ON items(category)');
  }

  await db.execute(
    `CREATE TABLE IF NOT EXISTS weapons (
      item_id VARCHAR(64) PRIMARY KEY,
      weapon_type VARCHAR(64) NOT NULL,
      handedness VARCHAR(32) NOT NULL DEFAULT 'one-hand',
      min_damage INT NOT NULL DEFAULT 0,
      max_damage INT NOT NULL DEFAULT 0,
      attack_speed FLOAT NOT NULL DEFAULT 1,
      range_meters FLOAT NOT NULL DEFAULT 1,
      required_level INT NOT NULL DEFAULT 1,
      required_class_id VARCHAR(64),
      metadata_json LONGTEXT NOT NULL DEFAULT '{}',
      FOREIGN KEY (item_id) REFERENCES items(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  if (!(await indexExists(db, 'weapons', 'idx_weapons_type'))) {
    await db.execute('CREATE INDEX idx_weapons_type ON weapons(weapon_type)');
  }

  await db.execute(
    `CREATE TABLE IF NOT EXISTS armor (
      item_id VARCHAR(64) PRIMARY KEY,
      slot VARCHAR(32) NOT NULL,
      armor_type VARCHAR(64) NOT NULL,
      defense INT NOT NULL DEFAULT 0,
      resistances_json LONGTEXT NOT NULL DEFAULT '{}',
      required_level INT NOT NULL DEFAULT 1,
      required_class_id VARCHAR(64),
      metadata_json LONGTEXT NOT NULL DEFAULT '{}',
      FOREIGN KEY (item_id) REFERENCES items(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  if (!(await indexExists(db, 'armor', 'idx_armor_slot'))) {
    await db.execute('CREATE INDEX idx_armor_slot ON armor(slot)');
  }

  await db.execute(
    `CREATE TABLE IF NOT EXISTS classes (
      id VARCHAR(64) PRIMARY KEY,
      name VARCHAR(120) NOT NULL,
      description TEXT,
      role VARCHAR(64),
      resource_type VARCHAR(64),
      starting_level INT NOT NULL DEFAULT 1,
      metadata_json LONGTEXT NOT NULL DEFAULT '{}',
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL
    ) ENGINE=InnoDB;`
  );

  if (!(await indexExists(db, 'classes', 'idx_classes_role'))) {
    await db.execute('CREATE INDEX idx_classes_role ON classes(role)');
  }

  await db.execute(
    `CREATE TABLE IF NOT EXISTS class_base_stats (
      class_id VARCHAR(64) PRIMARY KEY,
      base_health INT NOT NULL DEFAULT 0,
      base_mana INT NOT NULL DEFAULT 0,
      strength INT NOT NULL DEFAULT 0,
      agility INT NOT NULL DEFAULT 0,
      intelligence INT NOT NULL DEFAULT 0,
      vitality INT NOT NULL DEFAULT 0,
      defense INT NOT NULL DEFAULT 0,
      crit_chance FLOAT NOT NULL DEFAULT 0,
      speed FLOAT NOT NULL DEFAULT 0,
      updated_at VARCHAR(32) NOT NULL,
      FOREIGN KEY (class_id) REFERENCES classes(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS enemies (
      id VARCHAR(64) PRIMARY KEY,
      name VARCHAR(120) NOT NULL,
      description TEXT,
      enemy_type VARCHAR(64),
      level INT NOT NULL DEFAULT 1,
      faction VARCHAR(64),
      is_boss TINYINT(1) NOT NULL DEFAULT 0,
      metadata_json LONGTEXT NOT NULL DEFAULT '{}',
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL
    ) ENGINE=InnoDB;`
  );

  if (!(await indexExists(db, 'enemies', 'idx_enemies_type'))) {
    await db.execute('CREATE INDEX idx_enemies_type ON enemies(enemy_type)');
  }

  await db.execute(
    `CREATE TABLE IF NOT EXISTS enemy_base_stats (
      enemy_id VARCHAR(64) PRIMARY KEY,
      base_health INT NOT NULL DEFAULT 0,
      base_mana INT NOT NULL DEFAULT 0,
      attack INT NOT NULL DEFAULT 0,
      defense INT NOT NULL DEFAULT 0,
      agility INT NOT NULL DEFAULT 0,
      crit_chance FLOAT NOT NULL DEFAULT 0,
      xp_reward INT NOT NULL DEFAULT 0,
      gold_reward INT NOT NULL DEFAULT 0,
      updated_at VARCHAR(32) NOT NULL,
      FOREIGN KEY (enemy_id) REFERENCES enemies(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS abilities (
      id VARCHAR(64) PRIMARY KEY,
      name VARCHAR(120) NOT NULL,
      description TEXT,
      ability_type VARCHAR(64),
      cooldown_seconds FLOAT NOT NULL DEFAULT 0,
      resource_cost INT NOT NULL DEFAULT 0,
      range_meters FLOAT NOT NULL DEFAULT 0,
      cast_time_seconds FLOAT NOT NULL DEFAULT 0,
      metadata_json LONGTEXT NOT NULL DEFAULT '{}',
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL
    ) ENGINE=InnoDB;`
  );

  if (!(await indexExists(db, 'abilities', 'idx_abilities_type'))) {
    await db.execute('CREATE INDEX idx_abilities_type ON abilities(ability_type)');
  }

  await db.execute(
    `CREATE TABLE IF NOT EXISTS level_progression (
      level INT PRIMARY KEY,
      xp_required INT NOT NULL DEFAULT 0,
      total_xp INT NOT NULL DEFAULT 0,
      hp_gain INT NOT NULL DEFAULT 0,
      mana_gain INT NOT NULL DEFAULT 0,
      stat_points INT NOT NULL DEFAULT 0,
      reward_json LONGTEXT NOT NULL DEFAULT '{}',
      updated_at VARCHAR(32) NOT NULL
    ) ENGINE=InnoDB;`
  );
}

export const id = '006_mmo_catalog';
export const name = 'Add MMO catalog and progression tables';
