import type { DbExecutor } from '../database';

async function columnExists(db: DbExecutor, table: string, column: string): Promise<boolean> {
  const rows = await db.query<{ total: number }[]>(
    `SELECT COUNT(*) as total
     FROM information_schema.columns
     WHERE table_schema = DATABASE() AND table_name = ? AND column_name = ?`,
    [table, column]
  );
  return (rows[0]?.total ?? 0) > 0;
}

async function indexExists(db: DbExecutor, table: string, index: string): Promise<boolean> {
  const rows = await db.query<{ total: number }[]>(
    `SELECT COUNT(*) as total
     FROM information_schema.statistics
     WHERE table_schema = DATABASE() AND table_name = ? AND index_name = ?`,
    [table, index]
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
      created_at VARCHAR(32) NOT NULL
    ) ENGINE=InnoDB;`
  );

  if (!(await columnExists(db, 'users', 'username'))) {
    await db.execute(`ALTER TABLE users ADD COLUMN username VARCHAR(64) NOT NULL DEFAULT ''`);
    const users = await db.query<{ id: string; email: string }[]>(
      'SELECT id, email FROM users'
    );
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
      await db.execute('UPDATE users SET username = ? WHERE id = ?', [candidate, user.id]);
    }

    if (!(await indexExists(db, 'users', 'idx_users_username'))) {
      await db.execute('CREATE UNIQUE INDEX idx_users_username ON users(username)');
    }
  }

  await db.execute(
    `CREATE TABLE IF NOT EXISTS realms (
      id VARCHAR(36) PRIMARY KEY,
      name VARCHAR(120) NOT NULL UNIQUE,
      narrative TEXT NOT NULL,
      created_at VARCHAR(32) NOT NULL
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS realm_memberships (
      id VARCHAR(36) PRIMARY KEY,
      realm_id VARCHAR(36) NOT NULL,
      user_id VARCHAR(36) NOT NULL,
      role ENUM('player', 'builder') NOT NULL,
      created_at VARCHAR(32) NOT NULL,
      UNIQUE KEY uniq_realm_membership (realm_id, user_id),
      FOREIGN KEY (realm_id) REFERENCES realms(id) ON DELETE CASCADE,
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS realm_chunks (
      id VARCHAR(36) PRIMARY KEY,
      realm_id VARCHAR(36) NOT NULL,
      chunk_x INT NOT NULL,
      chunk_z INT NOT NULL,
      payload_json LONGTEXT NOT NULL DEFAULT '{}',
      is_deleted TINYINT(1) NOT NULL DEFAULT 0,
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL,
      UNIQUE KEY uniq_realm_chunk (realm_id, chunk_x, chunk_z),
      FOREIGN KEY (realm_id) REFERENCES realms(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  if (!(await indexExists(db, 'realm_chunks', 'idx_realm_chunks_realm'))) {
    await db.execute(
      'CREATE INDEX idx_realm_chunks_realm ON realm_chunks(realm_id, updated_at DESC)'
    );
  }

  await db.execute(
    `CREATE TABLE IF NOT EXISTS chunk_structures (
      id VARCHAR(36) PRIMARY KEY,
      realm_id VARCHAR(36) NOT NULL,
      chunk_id VARCHAR(36) NOT NULL,
      structure_type VARCHAR(64) NOT NULL,
      data_json LONGTEXT NOT NULL DEFAULT '{}',
      is_deleted TINYINT(1) NOT NULL DEFAULT 0,
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL,
      FOREIGN KEY (realm_id) REFERENCES realms(id) ON DELETE CASCADE,
      FOREIGN KEY (chunk_id) REFERENCES realm_chunks(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  if (!(await indexExists(db, 'chunk_structures', 'idx_chunk_structures_chunk'))) {
    await db.execute(
      'CREATE INDEX idx_chunk_structures_chunk ON chunk_structures(chunk_id, updated_at DESC)'
    );
  }

  await db.execute(
    `CREATE TABLE IF NOT EXISTS chunk_plots (
      id VARCHAR(36) PRIMARY KEY,
      realm_id VARCHAR(36) NOT NULL,
      chunk_id VARCHAR(36) NOT NULL,
      plot_identifier VARCHAR(128) NOT NULL,
      owner_user_id VARCHAR(36),
      data_json LONGTEXT NOT NULL DEFAULT '{}',
      is_deleted TINYINT(1) NOT NULL DEFAULT 0,
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL,
      UNIQUE KEY uniq_chunk_plot (chunk_id, plot_identifier),
      FOREIGN KEY (realm_id) REFERENCES realms(id) ON DELETE CASCADE,
      FOREIGN KEY (chunk_id) REFERENCES realm_chunks(id) ON DELETE CASCADE,
      FOREIGN KEY (owner_user_id) REFERENCES users(id) ON DELETE SET NULL
    ) ENGINE=InnoDB;`
  );

  if (!(await indexExists(db, 'chunk_plots', 'idx_chunk_plots_chunk'))) {
    await db.execute(
      'CREATE INDEX idx_chunk_plots_chunk ON chunk_plots(chunk_id, updated_at DESC)'
    );
  }

  await db.execute(
    `CREATE TABLE IF NOT EXISTS realm_resource_wallets (
      id VARCHAR(36) PRIMARY KEY,
      realm_id VARCHAR(36) NOT NULL,
      user_id VARCHAR(36) NOT NULL,
      resource_type VARCHAR(64) NOT NULL,
      quantity INT NOT NULL DEFAULT 0,
      updated_at VARCHAR(32) NOT NULL,
      UNIQUE KEY uniq_resource_wallet (realm_id, user_id, resource_type),
      FOREIGN KEY (realm_id) REFERENCES realms(id) ON DELETE CASCADE,
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  if (!(await indexExists(db, 'realm_resource_wallets', 'idx_resource_wallet_lookup'))) {
    await db.execute(
      'CREATE INDEX idx_resource_wallet_lookup ON realm_resource_wallets(realm_id, user_id, resource_type)'
    );
  }

  await db.execute(
    `CREATE TABLE IF NOT EXISTS chunk_change_log (
      id VARCHAR(36) PRIMARY KEY,
      realm_id VARCHAR(36) NOT NULL,
      chunk_id VARCHAR(36) NOT NULL,
      change_type VARCHAR(64) NOT NULL,
      payload_json LONGTEXT NOT NULL DEFAULT '{}',
      created_at VARCHAR(32) NOT NULL,
      FOREIGN KEY (realm_id) REFERENCES realms(id) ON DELETE CASCADE,
      FOREIGN KEY (chunk_id) REFERENCES realm_chunks(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  if (!(await indexExists(db, 'chunk_change_log', 'idx_chunk_change_log_realm'))) {
    await db.execute(
      'CREATE INDEX idx_chunk_change_log_realm ON chunk_change_log(realm_id, created_at DESC)'
    );
  }

  await db.execute(
    `CREATE TABLE IF NOT EXISTS characters (
      id VARCHAR(36) PRIMARY KEY,
      user_id VARCHAR(36) NOT NULL,
      realm_id VARCHAR(36) NOT NULL,
      name VARCHAR(120) NOT NULL,
      bio TEXT,
      race_id VARCHAR(64) NOT NULL DEFAULT 'human',
      appearance_json LONGTEXT NOT NULL DEFAULT '{}',
      class_id VARCHAR(64),
      class_states_json LONGTEXT NOT NULL DEFAULT '[]',
      last_location TEXT,
      created_at VARCHAR(32) NOT NULL,
      UNIQUE KEY uniq_character_name (user_id, realm_id, name),
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
      FOREIGN KEY (realm_id) REFERENCES realms(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  if (!(await columnExists(db, 'characters', 'race_id'))) {
    await db.execute(
      `ALTER TABLE characters ADD COLUMN race_id VARCHAR(64) NOT NULL DEFAULT 'human'`
    );
    await db.execute(
      `UPDATE characters SET race_id = 'human' WHERE race_id IS NULL OR race_id = ''`
    );
  }
  if (!(await columnExists(db, 'characters', 'appearance_json'))) {
    await db.execute(
      `ALTER TABLE characters ADD COLUMN appearance_json LONGTEXT NOT NULL DEFAULT '{}'`
    );
    await db.execute(
      `UPDATE characters SET appearance_json = '{}' WHERE appearance_json IS NULL`
    );
  }
  if (!(await columnExists(db, 'characters', 'class_id'))) {
    await db.execute(`ALTER TABLE characters ADD COLUMN class_id VARCHAR(64)`);
  }
  if (!(await columnExists(db, 'characters', 'class_states_json'))) {
    await db.execute(
      `ALTER TABLE characters ADD COLUMN class_states_json LONGTEXT NOT NULL DEFAULT '[]'`
    );
    await db.execute(
      `UPDATE characters SET class_states_json = '[]' WHERE class_states_json IS NULL`
    );
  }
  if (!(await columnExists(db, 'characters', 'last_location'))) {
    await db.execute(`ALTER TABLE characters ADD COLUMN last_location TEXT`);
  }

  await db.execute(
    `CREATE TABLE IF NOT EXISTS character_progression (
      character_id VARCHAR(36) PRIMARY KEY,
      level INT NOT NULL DEFAULT 1,
      xp INT NOT NULL DEFAULT 0,
      version INT NOT NULL DEFAULT 0,
      updated_at VARCHAR(32) NOT NULL,
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS character_class_unlocks (
      id VARCHAR(36) PRIMARY KEY,
      character_id VARCHAR(36) NOT NULL,
      class_id VARCHAR(64) NOT NULL,
      unlocked TINYINT(1) NOT NULL DEFAULT 0,
      unlocked_at VARCHAR(32),
      UNIQUE KEY uniq_character_class (character_id, class_id),
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS character_class_unlock_state (
      character_id VARCHAR(36) PRIMARY KEY,
      version INT NOT NULL DEFAULT 0,
      updated_at VARCHAR(32) NOT NULL,
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS character_inventory_items (
      id VARCHAR(36) PRIMARY KEY,
      character_id VARCHAR(36) NOT NULL,
      item_id VARCHAR(64) NOT NULL,
      quantity INT NOT NULL DEFAULT 1,
      metadata_json LONGTEXT NOT NULL DEFAULT '{}',
      UNIQUE KEY uniq_character_item (character_id, item_id),
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS character_inventory_state (
      character_id VARCHAR(36) PRIMARY KEY,
      version INT NOT NULL DEFAULT 0,
      updated_at VARCHAR(32) NOT NULL,
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS character_quest_states (
      id VARCHAR(36) PRIMARY KEY,
      character_id VARCHAR(36) NOT NULL,
      quest_id VARCHAR(64) NOT NULL,
      status VARCHAR(32) NOT NULL,
      progress_json LONGTEXT NOT NULL DEFAULT '{}',
      updated_at VARCHAR(32) NOT NULL,
      UNIQUE KEY uniq_character_quest (character_id, quest_id),
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS character_quest_state_meta (
      character_id VARCHAR(36) PRIMARY KEY,
      version INT NOT NULL DEFAULT 0,
      updated_at VARCHAR(32) NOT NULL,
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS refresh_tokens (
      id VARCHAR(36) PRIMARY KEY,
      user_id VARCHAR(36) NOT NULL,
      token_hash VARCHAR(255) NOT NULL,
      expires_at VARCHAR(32) NOT NULL,
      created_at VARCHAR(32) NOT NULL,
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );
}

export const id = '001_initial_schema';
export const name = 'Initialize world state schema';
