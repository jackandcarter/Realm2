import Database, { Database as DatabaseInstance } from 'better-sqlite3';
import { env } from '../config/env';

const db: DatabaseInstance = new Database(env.databasePath);
db.pragma('foreign_keys = ON');
db.pragma('journal_mode = WAL');

db.exec(`
  CREATE TABLE IF NOT EXISTS users (
    id TEXT PRIMARY KEY,
    email TEXT NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    created_at TEXT NOT NULL
  );
`);

db.exec(`
  CREATE TABLE IF NOT EXISTS realms (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL UNIQUE,
    narrative TEXT NOT NULL,
    created_at TEXT NOT NULL
  );
`);

db.exec(`
  CREATE TABLE IF NOT EXISTS realm_memberships (
    id TEXT PRIMARY KEY,
    realm_id TEXT NOT NULL,
    user_id TEXT NOT NULL,
    role TEXT NOT NULL CHECK(role IN ('player', 'builder')),
    created_at TEXT NOT NULL,
    UNIQUE(realm_id, user_id),
    FOREIGN KEY(realm_id) REFERENCES realms(id) ON DELETE CASCADE,
    FOREIGN KEY(user_id) REFERENCES users(id) ON DELETE CASCADE
  );
`);

db.exec(`
  CREATE TABLE IF NOT EXISTS characters (
    id TEXT PRIMARY KEY,
    user_id TEXT NOT NULL,
    realm_id TEXT NOT NULL,
    name TEXT NOT NULL,
    bio TEXT,
    created_at TEXT NOT NULL,
    UNIQUE(user_id, realm_id, name),
    FOREIGN KEY(user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY(realm_id) REFERENCES realms(id) ON DELETE CASCADE
  );
`);

db.exec(`
  CREATE TABLE IF NOT EXISTS refresh_tokens (
    id TEXT PRIMARY KEY,
    user_id TEXT NOT NULL,
    token_hash TEXT NOT NULL,
    expires_at TEXT NOT NULL,
    created_at TEXT NOT NULL,
    FOREIGN KEY(user_id) REFERENCES users(id) ON DELETE CASCADE
  );
`);

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
  db.exec('DELETE FROM characters;');
  db.exec('DELETE FROM realm_memberships;');
  db.exec('DELETE FROM refresh_tokens;');
  db.exec('DELETE FROM users;');
  db.exec('DELETE FROM realms;');
  seedRealms();
}
