import type { DbExecutor } from '../database';

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
    `CREATE TABLE IF NOT EXISTS plot_ownerships (
      plot_id VARCHAR(36) PRIMARY KEY,
      realm_id VARCHAR(36) NOT NULL,
      owner_user_id VARCHAR(36),
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL,
      FOREIGN KEY (plot_id) REFERENCES chunk_plots(id) ON DELETE CASCADE,
      FOREIGN KEY (realm_id) REFERENCES realms(id) ON DELETE CASCADE,
      FOREIGN KEY (owner_user_id) REFERENCES users(id) ON DELETE SET NULL
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS plot_permissions (
      id VARCHAR(36) PRIMARY KEY,
      plot_id VARCHAR(36) NOT NULL,
      realm_id VARCHAR(36) NOT NULL,
      user_id VARCHAR(36) NOT NULL,
      permission VARCHAR(64) NOT NULL,
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL,
      UNIQUE KEY uniq_plot_permission (plot_id, user_id),
      FOREIGN KEY (plot_id) REFERENCES chunk_plots(id) ON DELETE CASCADE,
      FOREIGN KEY (realm_id) REFERENCES realms(id) ON DELETE CASCADE,
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  if (!(await indexExists(db, 'plot_permissions', 'idx_plot_permissions_plot'))) {
    await db.execute('CREATE INDEX idx_plot_permissions_plot ON plot_permissions(plot_id)');
  }

  await db.execute(
    `CREATE TABLE IF NOT EXISTS realm_build_zones (
      id VARCHAR(36) PRIMARY KEY,
      realm_id VARCHAR(36) NOT NULL,
      label VARCHAR(120),
      center_x FLOAT NOT NULL,
      center_y FLOAT NOT NULL,
      center_z FLOAT NOT NULL,
      size_x FLOAT NOT NULL,
      size_y FLOAT NOT NULL,
      size_z FLOAT NOT NULL,
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL,
      FOREIGN KEY (realm_id) REFERENCES realms(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  if (!(await indexExists(db, 'realm_build_zones', 'idx_build_zones_realm'))) {
    await db.execute('CREATE INDEX idx_build_zones_realm ON realm_build_zones(realm_id)');
  }

  await db.execute(
    `CREATE TABLE IF NOT EXISTS character_dock_layouts (
      id VARCHAR(36) PRIMARY KEY,
      character_id VARCHAR(36) NOT NULL,
      layout_key VARCHAR(120) NOT NULL,
      layout_json LONGTEXT NOT NULL DEFAULT '[]',
      updated_at VARCHAR(32) NOT NULL,
      UNIQUE KEY uniq_character_layout (character_id, layout_key),
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  if (!(await indexExists(db, 'character_dock_layouts', 'idx_character_layout_key'))) {
    await db.execute(
      'CREATE INDEX idx_character_layout_key ON character_dock_layouts(character_id, layout_key)'
    );
  }

  await db.execute(
    `CREATE TABLE IF NOT EXISTS character_map_pin_state_meta (
      character_id VARCHAR(36) PRIMARY KEY,
      version INT NOT NULL DEFAULT 0,
      updated_at VARCHAR(32) NOT NULL,
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS character_map_pin_states (
      id VARCHAR(36) PRIMARY KEY,
      character_id VARCHAR(36) NOT NULL,
      pin_id VARCHAR(128) NOT NULL,
      unlocked TINYINT(1) NOT NULL DEFAULT 0,
      updated_at VARCHAR(32) NOT NULL,
      UNIQUE KEY uniq_character_pin (character_id, pin_id),
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  if (!(await indexExists(db, 'character_map_pin_states', 'idx_map_pin_character'))) {
    await db.execute(
      'CREATE INDEX idx_map_pin_character ON character_map_pin_states(character_id, updated_at DESC)'
    );
  }
}

export const id = '005_world_building';
export const name = 'Add plot permissions, build zones, dock layouts, and map pin progression';
