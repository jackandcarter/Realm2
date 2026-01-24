import type { Database } from './database';
import { logger } from '../observability/logger';
import {
  id as terrainSchemaId,
  name as terrainSchemaName,
  up as terrainSchemaUp,
} from './migrations/300_terrainSchema';

const migrations = [
  { id: terrainSchemaId, name: terrainSchemaName, up: terrainSchemaUp },
];

export async function runTerrainMigrations(db: Database): Promise<void> {
  await db.execute(
    `CREATE TABLE IF NOT EXISTS schema_migrations (
      id VARCHAR(64) PRIMARY KEY,
      name VARCHAR(255) NOT NULL,
      applied_at VARCHAR(32) NOT NULL
    ) ENGINE=InnoDB;`
  );

  const appliedRows = await db.query<{ id: string }[]>(`SELECT id FROM schema_migrations`);
  const appliedIds = new Set(appliedRows.map((row) => row.id));

  for (const migration of migrations) {
    if (appliedIds.has(migration.id)) {
      continue;
    }
    logger.info({ migration: migration.id }, 'Applying terrain database migration');
    await migration.up(db);
    await db.execute(
      'INSERT INTO schema_migrations (id, name, applied_at) VALUES (?, ?, ?)',
      [migration.id, migration.name, new Date().toISOString()]
    );
  }
}
