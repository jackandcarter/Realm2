import type { Database as DatabaseInstance } from 'better-sqlite3';
import { measurePersistenceOperation } from '../../observability/metrics';

function exec(db: DatabaseInstance, operation: string, sql: string): void {
  measurePersistenceOperation(operation, () => db.exec(sql));
}

export function up(db: DatabaseInstance): void {
  exec(
    db,
    'migration.001.create_users',
    `CREATE TABLE IF NOT EXISTS users (
      id TEXT PRIMARY KEY,
      email TEXT NOT NULL UNIQUE,
      username TEXT NOT NULL UNIQUE,
      password_hash TEXT NOT NULL,
      created_at TEXT NOT NULL
    );`
  );

  const userColumns = db.prepare("PRAGMA table_info(users)").all() as { name: string }[];
  if (!userColumns.some((column) => column.name === 'username')) {
    exec(db, 'migration.001.users_add_username', "ALTER TABLE users ADD COLUMN username TEXT NOT NULL DEFAULT ''");

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

    exec(db, 'migration.001.users_username_index', 'CREATE UNIQUE INDEX IF NOT EXISTS idx_users_username ON users(username)');
  }

  exec(
    db,
    'migration.001.create_realms',
    `CREATE TABLE IF NOT EXISTS realms (
      id TEXT PRIMARY KEY,
      name TEXT NOT NULL UNIQUE,
      narrative TEXT NOT NULL,
      created_at TEXT NOT NULL
    );`
  );

  exec(
    db,
    'migration.001.create_realm_memberships',
    `CREATE TABLE IF NOT EXISTS realm_memberships (
      id TEXT PRIMARY KEY,
      realm_id TEXT NOT NULL,
      user_id TEXT NOT NULL,
      role TEXT NOT NULL CHECK(role IN ('player', 'builder')),
      created_at TEXT NOT NULL,
      UNIQUE(realm_id, user_id),
      FOREIGN KEY(realm_id) REFERENCES realms(id) ON DELETE CASCADE,
      FOREIGN KEY(user_id) REFERENCES users(id) ON DELETE CASCADE
    );`
  );

  exec(
    db,
    'migration.001.create_realm_chunks',
    `CREATE TABLE IF NOT EXISTS realm_chunks (
      id TEXT PRIMARY KEY,
      realm_id TEXT NOT NULL,
      chunk_x INTEGER NOT NULL,
      chunk_z INTEGER NOT NULL,
      payload_json TEXT NOT NULL DEFAULT '{}',
      is_deleted INTEGER NOT NULL DEFAULT 0,
      created_at TEXT NOT NULL,
      updated_at TEXT NOT NULL,
      UNIQUE(realm_id, chunk_x, chunk_z),
      FOREIGN KEY(realm_id) REFERENCES realms(id) ON DELETE CASCADE
    );`
  );

  exec(
    db,
    'migration.001.create_realm_chunks_index',
    `CREATE INDEX IF NOT EXISTS idx_realm_chunks_realm
      ON realm_chunks(realm_id, updated_at DESC);`
  );

  exec(
    db,
    'migration.001.create_chunk_structures',
    `CREATE TABLE IF NOT EXISTS chunk_structures (
      id TEXT PRIMARY KEY,
      realm_id TEXT NOT NULL,
      chunk_id TEXT NOT NULL,
      structure_type TEXT NOT NULL,
      data_json TEXT NOT NULL DEFAULT '{}',
      is_deleted INTEGER NOT NULL DEFAULT 0,
      created_at TEXT NOT NULL,
      updated_at TEXT NOT NULL,
      FOREIGN KEY(realm_id) REFERENCES realms(id) ON DELETE CASCADE,
      FOREIGN KEY(chunk_id) REFERENCES realm_chunks(id) ON DELETE CASCADE
    );`
  );

  exec(
    db,
    'migration.001.create_chunk_structures_index',
    `CREATE INDEX IF NOT EXISTS idx_chunk_structures_chunk
      ON chunk_structures(chunk_id, updated_at DESC);`
  );

  exec(
    db,
    'migration.001.create_chunk_plots',
    `CREATE TABLE IF NOT EXISTS chunk_plots (
      id TEXT PRIMARY KEY,
      realm_id TEXT NOT NULL,
      chunk_id TEXT NOT NULL,
      plot_identifier TEXT NOT NULL,
      owner_user_id TEXT,
      data_json TEXT NOT NULL DEFAULT '{}',
      is_deleted INTEGER NOT NULL DEFAULT 0,
      created_at TEXT NOT NULL,
      updated_at TEXT NOT NULL,
      UNIQUE(chunk_id, plot_identifier),
      FOREIGN KEY(realm_id) REFERENCES realms(id) ON DELETE CASCADE,
      FOREIGN KEY(chunk_id) REFERENCES realm_chunks(id) ON DELETE CASCADE,
      FOREIGN KEY(owner_user_id) REFERENCES users(id) ON DELETE SET NULL
    );`
  );

  exec(
    db,
    'migration.001.create_chunk_plots_index',
    `CREATE INDEX IF NOT EXISTS idx_chunk_plots_chunk
      ON chunk_plots(chunk_id, updated_at DESC);`
  );

  exec(
    db,
    'migration.001.create_chunk_change_log',
    `CREATE TABLE IF NOT EXISTS chunk_change_log (
      id TEXT PRIMARY KEY,
      realm_id TEXT NOT NULL,
      chunk_id TEXT NOT NULL,
      change_type TEXT NOT NULL,
      payload_json TEXT NOT NULL DEFAULT '{}',
      created_at TEXT NOT NULL,
      FOREIGN KEY(realm_id) REFERENCES realms(id) ON DELETE CASCADE,
      FOREIGN KEY(chunk_id) REFERENCES realm_chunks(id) ON DELETE CASCADE
    );`
  );

  exec(
    db,
    'migration.001.create_chunk_change_log_index',
    `CREATE INDEX IF NOT EXISTS idx_chunk_change_log_realm
      ON chunk_change_log(realm_id, created_at DESC);`
  );

  exec(
    db,
    'migration.001.create_characters',
    `CREATE TABLE IF NOT EXISTS characters (
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
    );`
  );

  const characterColumns = db.prepare('PRAGMA table_info(characters)').all() as { name: string }[];
  if (!characterColumns.some((column) => column.name === 'race_id')) {
    exec(db, 'migration.001.characters_add_race_id', "ALTER TABLE characters ADD COLUMN race_id TEXT NOT NULL DEFAULT 'human'");
    exec(db, 'migration.001.characters_backfill_race_id', "UPDATE characters SET race_id = 'human' WHERE race_id IS NULL OR race_id = ''");
  }
  if (!characterColumns.some((column) => column.name === 'appearance_json')) {
    exec(db, 'migration.001.characters_add_appearance', "ALTER TABLE characters ADD COLUMN appearance_json TEXT NOT NULL DEFAULT '{}'");
    exec(db, 'migration.001.characters_backfill_appearance', "UPDATE characters SET appearance_json = '{}' WHERE appearance_json IS NULL OR appearance_json = ''");
  }
  if (!characterColumns.some((column) => column.name === 'class_id')) {
    exec(db, 'migration.001.characters_add_class_id', 'ALTER TABLE characters ADD COLUMN class_id TEXT');
  }
  if (!characterColumns.some((column) => column.name === 'class_states_json')) {
    exec(db, 'migration.001.characters_add_class_states', "ALTER TABLE characters ADD COLUMN class_states_json TEXT NOT NULL DEFAULT '[]'");
    exec(db, 'migration.001.characters_backfill_class_states', "UPDATE characters SET class_states_json = '[]' WHERE class_states_json IS NULL OR class_states_json = ''");
  }
  if (!characterColumns.some((column) => column.name === 'last_location')) {
    exec(db, 'migration.001.characters_add_last_location', 'ALTER TABLE characters ADD COLUMN last_location TEXT');
  }

  exec(
    db,
    'migration.001.create_character_progression',
    `CREATE TABLE IF NOT EXISTS character_progression (
      character_id TEXT PRIMARY KEY,
      level INTEGER NOT NULL DEFAULT 1,
      xp INTEGER NOT NULL DEFAULT 0,
      version INTEGER NOT NULL DEFAULT 0,
      updated_at TEXT NOT NULL,
      FOREIGN KEY(character_id) REFERENCES characters(id) ON DELETE CASCADE
    );`
  );

  exec(
    db,
    'migration.001.create_character_class_unlocks',
    `CREATE TABLE IF NOT EXISTS character_class_unlocks (
      id TEXT PRIMARY KEY,
      character_id TEXT NOT NULL,
      class_id TEXT NOT NULL,
      unlocked INTEGER NOT NULL DEFAULT 0,
      unlocked_at TEXT,
      FOREIGN KEY(character_id) REFERENCES characters(id) ON DELETE CASCADE,
      UNIQUE(character_id, class_id)
    );`
  );

  exec(
    db,
    'migration.001.create_character_class_unlock_state',
    `CREATE TABLE IF NOT EXISTS character_class_unlock_state (
      character_id TEXT PRIMARY KEY,
      version INTEGER NOT NULL DEFAULT 0,
      updated_at TEXT NOT NULL,
      FOREIGN KEY(character_id) REFERENCES characters(id) ON DELETE CASCADE
    );`
  );

  exec(
    db,
    'migration.001.create_character_inventory_items',
    `CREATE TABLE IF NOT EXISTS character_inventory_items (
      id TEXT PRIMARY KEY,
      character_id TEXT NOT NULL,
      item_id TEXT NOT NULL,
      quantity INTEGER NOT NULL DEFAULT 1,
      metadata_json TEXT NOT NULL DEFAULT '{}',
      FOREIGN KEY(character_id) REFERENCES characters(id) ON DELETE CASCADE,
      UNIQUE(character_id, item_id)
    );`
  );

  exec(
    db,
    'migration.001.create_character_inventory_state',
    `CREATE TABLE IF NOT EXISTS character_inventory_state (
      character_id TEXT PRIMARY KEY,
      version INTEGER NOT NULL DEFAULT 0,
      updated_at TEXT NOT NULL,
      FOREIGN KEY(character_id) REFERENCES characters(id) ON DELETE CASCADE
    );`
  );

  exec(
    db,
    'migration.001.create_character_quest_states',
    `CREATE TABLE IF NOT EXISTS character_quest_states (
      id TEXT PRIMARY KEY,
      character_id TEXT NOT NULL,
      quest_id TEXT NOT NULL,
      status TEXT NOT NULL,
      progress_json TEXT NOT NULL DEFAULT '{}',
      updated_at TEXT NOT NULL,
      FOREIGN KEY(character_id) REFERENCES characters(id) ON DELETE CASCADE,
      UNIQUE(character_id, quest_id)
    );`
  );

  exec(
    db,
    'migration.001.create_character_quest_state_meta',
    `CREATE TABLE IF NOT EXISTS character_quest_state_meta (
      character_id TEXT PRIMARY KEY,
      version INTEGER NOT NULL DEFAULT 0,
      updated_at TEXT NOT NULL,
      FOREIGN KEY(character_id) REFERENCES characters(id) ON DELETE CASCADE
    );`
  );

  exec(
    db,
    'migration.001.create_refresh_tokens',
    `CREATE TABLE IF NOT EXISTS refresh_tokens (
      id TEXT PRIMARY KEY,
      user_id TEXT NOT NULL,
      token_hash TEXT NOT NULL,
      expires_at TEXT NOT NULL,
      created_at TEXT NOT NULL,
      FOREIGN KEY(user_id) REFERENCES users(id) ON DELETE CASCADE
    );`
  );
}

export const id = '001_initial_schema';
export const name = 'Initialize world state schema';
