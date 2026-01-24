import { logger } from '../observability/logger';
import { measurePersistenceOperationAsync } from '../observability/metrics';
import type { DbExecutor } from './database';
import { id as worldSchemaId, name as worldSchemaName, up as worldSchemaUp } from './migrations/200_worldSchema';

interface DatabaseMigration {
  id: string;
  name: string;
  up: (db: DbExecutor) => Promise<void>;
}

const migrations: DatabaseMigration[] = [
  { id: worldSchemaId, name: worldSchemaName, up: worldSchemaUp },
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

export async function runWorldMigrations(db: DbExecutor): Promise<void> {
  await ensureMigrationsTable(db);
  const appliedRows = await db.query<{ id: string }[]>(
    'SELECT id FROM schema_migrations ORDER BY applied_at ASC'
  );
  const applied = new Set(appliedRows.map((row) => row.id));
  if (applied.size > 0 && !applied.has(migrations[0]?.id ?? '')) {
    logger.warn(
      { appliedMigrations: Array.from(applied) },
      'Skipping world base migration because schema migrations already exist'
    );
    return;
  }

  for (const migration of migrations) {
    if (applied.has(migration.id)) {
      continue;
    }
    logger.info({ migration: migration.id }, 'Applying world database migration');
    await measurePersistenceOperationAsync(`migration.${migration.id}`, async () => {
      await migration.up(db);
      await db.execute(
        'INSERT INTO schema_migrations (id, name, applied_at) VALUES (?, ?, ?)',
        [migration.id, migration.name, new Date().toISOString().slice(0, 19).replace('T', ' ')]
      );
    });
    logger.info({ migration: migration.id }, 'World migration applied');
  }
}
