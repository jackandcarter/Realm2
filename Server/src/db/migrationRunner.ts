import { logger } from '../observability/logger';
import { measurePersistenceOperationAsync } from '../observability/metrics';
import { id as initialId, name as initialName, up as initialUp } from './migrations/001_initialSchema';
import { id as buildStateId, name as buildStateName, up as buildStateUp } from './migrations/002_buildStates';
import { id as equipmentId, name as equipmentName, up as equipmentUp } from './migrations/003_equipmentState';
import {
  id as contentCatalogId,
  name as contentCatalogName,
  up as contentCatalogUp,
} from './migrations/004_contentCatalog';
import { id as worldBuildingId, name as worldBuildingName, up as worldBuildingUp } from './migrations/005_worldBuilding';
import type { DbExecutor } from './database';

interface DatabaseMigration {
  id: string;
  name: string;
  up: (db: DbExecutor) => Promise<void>;
}

const migrations: DatabaseMigration[] = [
  { id: initialId, name: initialName, up: initialUp },
  { id: buildStateId, name: buildStateName, up: buildStateUp },
  { id: equipmentId, name: equipmentName, up: equipmentUp },
  { id: contentCatalogId, name: contentCatalogName, up: contentCatalogUp },
  { id: worldBuildingId, name: worldBuildingName, up: worldBuildingUp },
];

async function ensureMigrationsTable(db: DbExecutor): Promise<void> {
  await measurePersistenceOperationAsync('migration.ensure_table', () =>
    db.execute(`
      CREATE TABLE IF NOT EXISTS schema_migrations (
        id VARCHAR(255) PRIMARY KEY,
        name VARCHAR(255) NOT NULL,
        applied_at DATETIME NOT NULL
      );
    `)
  );
}

export async function runMigrations(db: DbExecutor): Promise<void> {
  await ensureMigrationsTable(db);
  const appliedRows = await db.query<{ id: string }[]>(
    'SELECT id FROM schema_migrations ORDER BY applied_at ASC'
  );
  const applied = new Set(appliedRows.map((row) => row.id));

  for (const migration of migrations) {
    if (applied.has(migration.id)) {
      continue;
    }
    logger.info({ migration: migration.id }, 'Applying database migration');
    await measurePersistenceOperationAsync(`migration.${migration.id}`, async () => {
      await migration.up(db);
      await db.execute(
        'INSERT INTO schema_migrations (id, name, applied_at) VALUES (?, ?, ?)',
        [migration.id, migration.name, new Date().toISOString().slice(0, 19).replace('T', ' ')]
      );
    });
    logger.info({ migration: migration.id }, 'Migration applied');
  }
}

export function listMigrations(): DatabaseMigration[] {
  return migrations.slice();
}
