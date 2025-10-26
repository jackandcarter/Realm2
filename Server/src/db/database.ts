import Database, { Database as DatabaseInstance, Statement } from 'better-sqlite3';
import { env } from '../config/env';
import { runMigrations } from './migrationRunner';
import { measurePersistenceOperation } from '../observability/metrics';
import { registerBackupDatabase } from '../maintenance/backupManager';

function deriveOperationLabel(sql: string): string {
  const normalized = sql.trim().toLowerCase();
  const firstToken = normalized.split(/\s+/)[0] ?? 'raw';
  const tableMatch =
    normalized.match(/into\s+([^\s(]+)/) ??
    normalized.match(/from\s+([^\s(]+)/) ??
    normalized.match(/update\s+([^\s(]+)/) ??
    normalized.match(/table\s+([^\s(]+)/);
  const table = tableMatch ? tableMatch[1] : 'unknown';
  return `${firstToken}.${table}`;
}

function instrumentStatement<T extends Statement>(statement: T, sql: string): T {
  const baseOperation = deriveOperationLabel(sql);
  return new Proxy(statement, {
    get(target, property, receiver) {
      if (typeof property === 'string' && ['run', 'get', 'all', 'iterate'].includes(property)) {
        return (...args: unknown[]) =>
          measurePersistenceOperation(`${baseOperation}.${property}`, () =>
            Reflect.apply((target as any)[property], target, args)
          );
      }
      const value = Reflect.get(target, property, receiver);
      return typeof value === 'function' ? value.bind(target) : value;
    },
  }) as T;
}

function instrumentDatabase(db: DatabaseInstance): DatabaseInstance {
  return new Proxy(db, {
    get(target, property, receiver) {
      if (property === 'prepare') {
        return (sql: string) => instrumentStatement(target.prepare(sql), sql);
      }
      if (property === 'exec') {
        return (sql: string) =>
          measurePersistenceOperation(deriveOperationLabel(sql), () => target.exec(sql));
      }
      if (property === 'pragma') {
        return (pragma: string, options?: unknown) =>
          measurePersistenceOperation(`pragma.${pragma.split('(')[0]}`, () =>
            (target as DatabaseInstance).pragma(pragma, options as any)
          );
      }
      const value = Reflect.get(target, property, receiver);
      return typeof value === 'function' ? value.bind(target) : value;
    },
  }) as DatabaseInstance;
}

const rawDb = new Database(env.databasePath);
rawDb.pragma('foreign_keys = ON');
rawDb.pragma('journal_mode = WAL');

const db: DatabaseInstance = instrumentDatabase(rawDb);
runMigrations(db);
registerBackupDatabase(db);

const seededRealms = [
  {
    id: 'realm-elysium-nexus',
    name: 'Elysium Nexus',
    narrative:
      'A realm caught between magic and machinery where the Chrono Nexus warps time itself, drawing heroes and traitors alike into its shimmering core.',
  },
  {
    id: 'realm-arcane-haven',
    name: 'Arcane Haven',
    narrative:
      'Deep within the Eldros forests, scholars and rangers safeguard ancient libraries while whispers of Seraphina Frostwind guide seekers of hidden lore.',
  },
  {
    id: 'realm-gearspring',
    name: 'Gearspring Metropolis',
    narrative:
      'Floating platforms and technomagical forges define this Gearling stronghold where innovation is the only currency that matters.',
  },
];

function seedRealms(): void {
  const stmt = db.prepare(
    `INSERT INTO realms (id, name, narrative, created_at)
     VALUES (@id, @name, @narrative, @createdAt)
     ON CONFLICT(id) DO NOTHING`
  );

  const now = new Date().toISOString();
  for (const realm of seededRealms) {
    stmt.run({ ...realm, createdAt: now });
  }
}

seedRealms();

export { db };

export function resetDatabase(): void {
  measurePersistenceOperation('reset.realms', () => db.exec('DELETE FROM realms;'));
  measurePersistenceOperation('reset.characters', () => db.exec('DELETE FROM characters;'));
  measurePersistenceOperation('reset.memberships', () => db.exec('DELETE FROM realm_memberships;'));
  measurePersistenceOperation('reset.refresh_tokens', () => db.exec('DELETE FROM refresh_tokens;'));
  measurePersistenceOperation('reset.users', () => db.exec('DELETE FROM users;'));
  seedRealms();
}
