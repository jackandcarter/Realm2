import type { DbExecutor } from '../database';

async function constraintExists(
  db: DbExecutor,
  table: string,
  constraint: string
): Promise<boolean> {
  const rows = await db.query<{ total: number }[]>(
    `SELECT COUNT(*) as total
     FROM information_schema.table_constraints
     WHERE table_schema = DATABASE() AND table_name = ? AND constraint_name = ?`,
    [table, constraint]
  );
  return (rows[0]?.total ?? 0) > 0;
}

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
    `CREATE TABLE IF NOT EXISTS weapon_types (
      id VARCHAR(64) PRIMARY KEY,
      display_name VARCHAR(120) NOT NULL,
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS resource_types (
      id VARCHAR(128) PRIMARY KEY,
      display_name VARCHAR(120) NOT NULL,
      category VARCHAR(64) NOT NULL,
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL
    ) ENGINE=InnoDB;`
  );

  if (!(await indexExists(db, 'resource_types', 'idx_resource_types_category'))) {
    await db.execute('CREATE INDEX idx_resource_types_category ON resource_types(category)');
  }

  await db.execute(
    `CREATE TABLE IF NOT EXISTS ability_types (
      id VARCHAR(64) PRIMARY KEY,
      display_name VARCHAR(120) NOT NULL,
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `ALTER TABLE weapons
     MODIFY weapon_type VARCHAR(64) NOT NULL`
  );

  await db.execute(
    `ALTER TABLE abilities
     MODIFY ability_type VARCHAR(64)`
  );

  await db.execute(
    `ALTER TABLE realm_resource_wallets
     MODIFY resource_type VARCHAR(128) NOT NULL`
  );

  await db.execute(
    `ALTER TABLE class_weapon_proficiencies
     MODIFY weapon_type VARCHAR(64) NOT NULL`
  );

  if (!(await constraintExists(db, 'weapons', 'fk_weapons_weapon_type'))) {
    await db.execute(
      `ALTER TABLE weapons
       ADD CONSTRAINT fk_weapons_weapon_type
       FOREIGN KEY (weapon_type) REFERENCES weapon_types(id) ON DELETE RESTRICT`
    );
  }

  if (!(await constraintExists(db, 'abilities', 'fk_abilities_ability_type'))) {
    await db.execute(
      `ALTER TABLE abilities
       ADD CONSTRAINT fk_abilities_ability_type
       FOREIGN KEY (ability_type) REFERENCES ability_types(id) ON DELETE SET NULL`
    );
  }

  if (!(await constraintExists(db, 'realm_resource_wallets', 'fk_resource_wallets_resource_type'))) {
    await db.execute(
      `ALTER TABLE realm_resource_wallets
       ADD CONSTRAINT fk_resource_wallets_resource_type
       FOREIGN KEY (resource_type) REFERENCES resource_types(id) ON DELETE RESTRICT`
    );
  }

  if (!(await constraintExists(db, 'class_weapon_proficiencies', 'fk_class_weapon_type'))) {
    await db.execute(
      `ALTER TABLE class_weapon_proficiencies
       ADD CONSTRAINT fk_class_weapon_type
       FOREIGN KEY (weapon_type) REFERENCES weapon_types(id) ON DELETE RESTRICT`
    );
  }
}

export const id = '013_reference_types';
export const name = 'Add weapon/resource/ability type catalogs';
