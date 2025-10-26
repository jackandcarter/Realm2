import type { Database as DatabaseInstance } from 'better-sqlite3';
import { logger } from '../observability/logger';
import { measurePersistenceOperation } from '../observability/metrics';
import { id as initialId, name as initialName, up as initialUp } from './migrations/001_initialSchema';

interface DatabaseMigration {
  id: string;
  name: string;
  up: (db: DatabaseInstance) => void;
}

const migrations: DatabaseMigration[] = [
  { id: initialId, name: initialName, up: initialUp },
];

function ensureMigrationsTable(db: DatabaseInstance): void {
  measurePersistenceOperation('migration.ensure_table', () =>
    db.exec(`
      CREATE TABLE IF NOT EXISTS schema_migrations (
        id TEXT PRIMARY KEY,
        name TEXT NOT NULL,
        applied_at TEXT NOT NULL
      );
    `)
  );
}

export function runMigrations(db: DatabaseInstance): void {
  ensureMigrationsTable(db);
  const appliedRows = db
    .prepare('SELECT id FROM schema_migrations ORDER BY applied_at ASC')
    .all() as { id: string }[];
  const applied = new Set(appliedRows.map((row) => row.id));

  const insertStmt = db.prepare(
    'INSERT INTO schema_migrations (id, name, applied_at) VALUES (?, ?, ?)' 
  );

  for (const migration of migrations) {
    if (applied.has(migration.id)) {
      continue;
    }
    logger.info({ migration: migration.id }, 'Applying database migration');
    measurePersistenceOperation(`migration.${migration.id}`, () => {
      migration.up(db);
      insertStmt.run(migration.id, migration.name, new Date().toISOString());
    });
    logger.info({ migration: migration.id }, 'Migration applied');
  }
}

export function listMigrations(): DatabaseMigration[] {
  return migrations.slice();
}
