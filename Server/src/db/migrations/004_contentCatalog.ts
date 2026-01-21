import { DbExecutor } from '../database';

export async function up(db: DbExecutor): Promise<void> {
  await db.execute(`
    CREATE TABLE IF NOT EXISTS content_catalog_snapshots (
      id VARCHAR(36) PRIMARY KEY,
      version VARCHAR(64) NOT NULL,
      catalog_json LONGTEXT NOT NULL,
      created_at DATETIME NOT NULL,
      UNIQUE KEY uniq_content_catalog_version (version)
    )
  `);

  await db.execute(`
    CREATE TABLE IF NOT EXISTS content_catalog_state (
      id INT PRIMARY KEY,
      active_version VARCHAR(64),
      updated_at DATETIME NOT NULL
    )
  `);
}

export const id = '004_content_catalog_snapshot';
export const name = 'Add content catalog snapshot storage';
