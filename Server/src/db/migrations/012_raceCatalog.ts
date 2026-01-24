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
    `CREATE TABLE IF NOT EXISTS races (
      id VARCHAR(64) PRIMARY KEY,
      display_name VARCHAR(120) NOT NULL,
      customization_json LONGTEXT NOT NULL DEFAULT '{}',
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS race_class_rules (
      race_id VARCHAR(64) NOT NULL,
      class_id VARCHAR(64) NOT NULL,
      unlock_method VARCHAR(32) NOT NULL,
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL,
      PRIMARY KEY (race_id, class_id),
      FOREIGN KEY (race_id) REFERENCES races(id) ON DELETE CASCADE,
      FOREIGN KEY (class_id) REFERENCES classes(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  if (!(await indexExists(db, 'race_class_rules', 'idx_race_class_rules_class'))) {
    await db.execute('CREATE INDEX idx_race_class_rules_class ON race_class_rules(class_id)');
  }

  await db.execute(
    `CREATE TABLE IF NOT EXISTS class_weapon_proficiencies (
      class_id VARCHAR(64) NOT NULL,
      weapon_type VARCHAR(64) NOT NULL,
      created_at VARCHAR(32) NOT NULL,
      PRIMARY KEY (class_id, weapon_type),
      FOREIGN KEY (class_id) REFERENCES classes(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  if (!(await indexExists(db, 'class_weapon_proficiencies', 'idx_class_weapon_type'))) {
    await db.execute(
      'CREATE INDEX idx_class_weapon_type ON class_weapon_proficiencies(weapon_type)'
    );
  }
}

export const id = '012_race_catalog';
export const name = 'Add race catalog and class gating tables';
