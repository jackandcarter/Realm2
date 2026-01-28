import type { DbExecutor } from '../database';

async function columnExists(db: DbExecutor, table: string, column: string): Promise<boolean> {
  const rows = await db.query<{ total: number }[]>(
    `SELECT COUNT(*) as total
     FROM information_schema.columns
     WHERE table_schema = DATABASE() AND table_name = ? AND column_name = ?`,
    [table, column]
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

async function primaryKeyColumns(db: DbExecutor, table: string): Promise<string[]> {
  const rows = await db.query<{ column_name: string }[]>(
    `SELECT column_name
     FROM information_schema.key_column_usage
     WHERE table_schema = DATABASE() AND table_name = ? AND constraint_name = 'PRIMARY'`,
    [table]
  );
  return rows.map((row) => row.column_name);
}

export async function up(db: DbExecutor): Promise<void> {
  await db.execute(
    `CREATE TABLE IF NOT EXISTS character_build_states (
      id VARCHAR(36) PRIMARY KEY,
      realm_id VARCHAR(36) NOT NULL,
      character_id VARCHAR(36) NOT NULL,
      plots_json LONGTEXT NOT NULL DEFAULT '[]',
      constructions_json LONGTEXT NOT NULL DEFAULT '[]',
      updated_at VARCHAR(32) NOT NULL,
      UNIQUE KEY uniq_character_realm (character_id, realm_id),
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  if (!(await columnExists(db, 'character_build_states', 'id'))) {
    await db.execute('ALTER TABLE character_build_states ADD COLUMN id VARCHAR(36) NULL');
  }

  if (!(await columnExists(db, 'character_build_states', 'realm_id'))) {
    await db.execute('ALTER TABLE character_build_states ADD COLUMN realm_id VARCHAR(36) NULL');
  }

  if (!(await columnExists(db, 'character_build_states', 'plots_json'))) {
    await db.execute(
      "ALTER TABLE character_build_states ADD COLUMN plots_json LONGTEXT NOT NULL DEFAULT '[]'"
    );
  }

  if (!(await columnExists(db, 'character_build_states', 'constructions_json'))) {
    await db.execute(
      "ALTER TABLE character_build_states ADD COLUMN constructions_json LONGTEXT NOT NULL DEFAULT '[]'"
    );
  }

  await db.execute(
    "UPDATE character_build_states SET id = UUID() WHERE id IS NULL OR id = ''"
  );

  if (await columnExists(db, 'character_build_states', 'realm_id')) {
    await db.execute(
      `UPDATE character_build_states bs
       JOIN characters c ON bs.character_id = c.id
       SET bs.realm_id = c.realm_id
       WHERE bs.realm_id IS NULL OR bs.realm_id = ''`
    );
  }

  await db.execute(
    "UPDATE character_build_states SET plots_json = '[]' WHERE plots_json IS NULL OR plots_json = ''"
  );
  await db.execute(
    "UPDATE character_build_states SET constructions_json = '[]' WHERE constructions_json IS NULL OR constructions_json = ''"
  );

  await db.execute('ALTER TABLE character_build_states MODIFY id VARCHAR(36) NOT NULL');
  await db.execute('ALTER TABLE character_build_states MODIFY realm_id VARCHAR(36) NOT NULL');

  const primaryKeys = await primaryKeyColumns(db, 'character_build_states');
  if (primaryKeys.length > 0 && !primaryKeys.includes('id')) {
    await db.execute('ALTER TABLE character_build_states DROP PRIMARY KEY');
  }

  if (!primaryKeys.includes('id')) {
    await db.execute('ALTER TABLE character_build_states ADD PRIMARY KEY (id)');
  }

  if (!(await indexExists(db, 'character_build_states', 'uniq_character_realm'))) {
    await db.execute(
      'CREATE UNIQUE INDEX uniq_character_realm ON character_build_states(character_id, realm_id)'
    );
  }
}

export const id = '210_build_state_schema';
export const name = 'Align build state schema with plot/construction payloads';
