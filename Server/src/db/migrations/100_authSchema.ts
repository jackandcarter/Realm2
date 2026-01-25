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

async function columnExists(db: DbExecutor, table: string, column: string): Promise<boolean> {
  const rows = await db.query<{ total: number }[]>(
    `SELECT COUNT(*) as total
     FROM information_schema.columns
     WHERE table_schema = DATABASE() AND table_name = ? AND column_name = ?`,
    [table, column]
  );
  return (rows[0]?.total ?? 0) > 0;
}

export async function up(db: DbExecutor): Promise<void> {
  await db.execute(
    `CREATE TABLE IF NOT EXISTS users (
      id VARCHAR(36) PRIMARY KEY,
      email VARCHAR(255) NOT NULL UNIQUE,
      username VARCHAR(64) NOT NULL UNIQUE,
      password_hash VARCHAR(255) NOT NULL,
      created_at VARCHAR(32) NOT NULL,
      last_realm_id VARCHAR(36),
      last_character_id VARCHAR(36),
      last_realm_selected_at VARCHAR(32),
      last_character_selected_at VARCHAR(32)
    ) ENGINE=InnoDB;`
  );

  if (!(await indexExists(db, 'users', 'idx_users_username'))) {
    await db.execute('CREATE UNIQUE INDEX idx_users_username ON users(username)');
  }

  if (!(await columnExists(db, 'users', 'last_realm_id'))) {
    await db.execute('ALTER TABLE users ADD COLUMN last_realm_id VARCHAR(36)');
  }

  if (!(await columnExists(db, 'users', 'last_character_id'))) {
    await db.execute('ALTER TABLE users ADD COLUMN last_character_id VARCHAR(36)');
  }

  if (!(await columnExists(db, 'users', 'last_realm_selected_at'))) {
    await db.execute('ALTER TABLE users ADD COLUMN last_realm_selected_at VARCHAR(32)');
  }

  if (!(await columnExists(db, 'users', 'last_character_selected_at'))) {
    await db.execute('ALTER TABLE users ADD COLUMN last_character_selected_at VARCHAR(32)');
  }

  await db.execute(
    `CREATE TABLE IF NOT EXISTS refresh_tokens (
      id VARCHAR(36) PRIMARY KEY,
      user_id VARCHAR(36) NOT NULL,
      token_hash VARCHAR(255) NOT NULL UNIQUE,
      expires_at VARCHAR(32) NOT NULL,
      created_at VARCHAR(32) NOT NULL,
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );
}

export const id = '100_auth_schema';
export const name = 'Create auth schema (users + refresh tokens)';
