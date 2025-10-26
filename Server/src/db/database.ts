import Database, { Database as DatabaseInstance } from 'better-sqlite3';
import { env } from '../config/env';

const db: DatabaseInstance = new Database(env.databasePath);
db.pragma('foreign_keys = ON');
db.pragma('journal_mode = WAL');

db.exec(`
  CREATE TABLE IF NOT EXISTS users (
    id TEXT PRIMARY KEY,
    email TEXT NOT NULL UNIQUE,
    username TEXT NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    created_at TEXT NOT NULL
  );
`);

const userColumns = db.prepare("PRAGMA table_info(users)").all();
const hasUsernameColumn = userColumns.some((column) => column.name === 'username');
if (!hasUsernameColumn) {
  db.exec("ALTER TABLE users ADD COLUMN username TEXT NOT NULL DEFAULT ''");

  const users = db.prepare('SELECT id, email FROM users').all() as { id: string; email: string }[];
  const updateStmt = db.prepare('UPDATE users SET username = @username WHERE id = @id');
  const existingUsernames = new Set<string>();

  for (const user of users) {
    const base = user.email.split('@')[0] || 'adventurer';
    let candidate = base;
    let counter = 1;
    while (existingUsernames.has(candidate)) {
      candidate = `${base}${counter}`;
      counter += 1;
    }
    existingUsernames.add(candidate);
    updateStmt.run({ id: user.id, username: candidate });
  }

  db.exec('CREATE UNIQUE INDEX IF NOT EXISTS idx_users_username ON users(username)');
}

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
    race_id TEXT NOT NULL DEFAULT 'human',
    appearance_json TEXT NOT NULL DEFAULT '{}',
    class_id TEXT,
    class_states_json TEXT NOT NULL DEFAULT '[]',
    last_location TEXT,
    created_at TEXT NOT NULL,
    UNIQUE(user_id, realm_id, name),
    FOREIGN KEY(user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY(realm_id) REFERENCES realms(id) ON DELETE CASCADE
  );
`);

db.exec(`
  CREATE TABLE IF NOT EXISTS character_progression (
    character_id TEXT PRIMARY KEY,
    level INTEGER NOT NULL DEFAULT 1,
    xp INTEGER NOT NULL DEFAULT 0,
    version INTEGER NOT NULL DEFAULT 0,
    updated_at TEXT NOT NULL,
    FOREIGN KEY(character_id) REFERENCES characters(id) ON DELETE CASCADE
  );
`);

db.exec(`
  CREATE TABLE IF NOT EXISTS character_class_unlocks (
    id TEXT PRIMARY KEY,
    character_id TEXT NOT NULL,
    class_id TEXT NOT NULL,
    unlocked INTEGER NOT NULL DEFAULT 0,
    unlocked_at TEXT,
    FOREIGN KEY(character_id) REFERENCES characters(id) ON DELETE CASCADE,
    UNIQUE(character_id, class_id)
  );
`);

db.exec(`
  CREATE TABLE IF NOT EXISTS character_class_unlock_state (
    character_id TEXT PRIMARY KEY,
    version INTEGER NOT NULL DEFAULT 0,
    updated_at TEXT NOT NULL,
    FOREIGN KEY(character_id) REFERENCES characters(id) ON DELETE CASCADE
  );
`);

db.exec(`
  CREATE TABLE IF NOT EXISTS character_inventory_items (
    id TEXT PRIMARY KEY,
    character_id TEXT NOT NULL,
    item_id TEXT NOT NULL,
    quantity INTEGER NOT NULL DEFAULT 1,
    metadata_json TEXT NOT NULL DEFAULT '{}',
    FOREIGN KEY(character_id) REFERENCES characters(id) ON DELETE CASCADE,
    UNIQUE(character_id, item_id)
  );
`);

db.exec(`
  CREATE TABLE IF NOT EXISTS character_inventory_state (
    character_id TEXT PRIMARY KEY,
    version INTEGER NOT NULL DEFAULT 0,
    updated_at TEXT NOT NULL,
    FOREIGN KEY(character_id) REFERENCES characters(id) ON DELETE CASCADE
  );
`);

db.exec(`
  CREATE TABLE IF NOT EXISTS character_quest_states (
    id TEXT PRIMARY KEY,
    character_id TEXT NOT NULL,
    quest_id TEXT NOT NULL,
    status TEXT NOT NULL,
    progress_json TEXT NOT NULL DEFAULT '{}',
    updated_at TEXT NOT NULL,
    FOREIGN KEY(character_id) REFERENCES characters(id) ON DELETE CASCADE,
    UNIQUE(character_id, quest_id)
  );
`);

db.exec(`
  CREATE TABLE IF NOT EXISTS character_quest_state_meta (
    character_id TEXT PRIMARY KEY,
    version INTEGER NOT NULL DEFAULT 0,
    updated_at TEXT NOT NULL,
    FOREIGN KEY(character_id) REFERENCES characters(id) ON DELETE CASCADE
  );
`);

const characterColumns = db.prepare("PRAGMA table_info(characters)").all();
const hasRaceIdColumn = characterColumns.some((column) => column.name === 'race_id');
if (!hasRaceIdColumn) {
  db.exec("ALTER TABLE characters ADD COLUMN race_id TEXT NOT NULL DEFAULT 'human'");
}
db.exec("UPDATE characters SET race_id = 'human' WHERE race_id IS NULL OR race_id = ''");

const hasAppearanceColumn = characterColumns.some((column) => column.name === 'appearance_json');
if (!hasAppearanceColumn) {
  db.exec("ALTER TABLE characters ADD COLUMN appearance_json TEXT NOT NULL DEFAULT '{}'");
}
db.exec("UPDATE characters SET appearance_json = '{}' WHERE appearance_json IS NULL OR appearance_json = ''");

const hasClassIdColumn = characterColumns.some((column) => column.name === 'class_id');
if (!hasClassIdColumn) {
  db.exec('ALTER TABLE characters ADD COLUMN class_id TEXT');
}

const hasClassStatesColumn = characterColumns.some((column) => column.name === 'class_states_json');
if (!hasClassStatesColumn) {
  db.exec("ALTER TABLE characters ADD COLUMN class_states_json TEXT NOT NULL DEFAULT '[]'");
}
db.exec("UPDATE characters SET class_states_json = '[]' WHERE class_states_json IS NULL OR class_states_json = ''");

const hasLastLocationColumn = characterColumns.some((column) => column.name === 'last_location');
if (!hasLastLocationColumn) {
  db.exec('ALTER TABLE characters ADD COLUMN last_location TEXT');
}

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
