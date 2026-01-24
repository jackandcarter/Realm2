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
    `CREATE TABLE IF NOT EXISTS terrain_regions (
      id VARCHAR(64) PRIMARY KEY,
      realm_id VARCHAR(36) NOT NULL,
      name VARCHAR(120) NOT NULL,
      bounds_json LONGTEXT NOT NULL DEFAULT '{}',
      terrain_count INT NOT NULL DEFAULT 0,
      payload_json LONGTEXT NOT NULL DEFAULT '{}',
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL
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
      UNIQUE KEY uniq_realm_chunk (realm_id, chunk_x, chunk_z)
    ) ENGINE=InnoDB;`
  );

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
      FOREIGN KEY (chunk_id) REFERENCES realm_chunks(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

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
      FOREIGN KEY (chunk_id) REFERENCES realm_chunks(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS chunk_change_log (
      id VARCHAR(36) PRIMARY KEY,
      realm_id VARCHAR(36) NOT NULL,
      chunk_id VARCHAR(36) NOT NULL,
      change_type VARCHAR(64) NOT NULL,
      payload_json LONGTEXT NOT NULL DEFAULT '{}',
      created_at VARCHAR(32) NOT NULL,
      FOREIGN KEY (chunk_id) REFERENCES realm_chunks(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS plot_ownerships (
      plot_id VARCHAR(36) PRIMARY KEY,
      realm_id VARCHAR(36) NOT NULL,
      owner_user_id VARCHAR(36),
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL,
      FOREIGN KEY (plot_id) REFERENCES chunk_plots(id) ON DELETE CASCADE
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
      FOREIGN KEY (plot_id) REFERENCES chunk_plots(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  if (!(await indexExists(db, 'realm_chunks', 'idx_realm_chunks_realm'))) {
    await db.execute('CREATE INDEX idx_realm_chunks_realm ON realm_chunks(realm_id, updated_at DESC)');
  }

  if (!(await indexExists(db, 'terrain_regions', 'idx_terrain_regions_realm'))) {
    await db.execute('CREATE INDEX idx_terrain_regions_realm ON terrain_regions(realm_id, updated_at DESC)');
  }

  if (!(await indexExists(db, 'chunk_structures', 'idx_chunk_structures_chunk'))) {
    await db.execute(
      'CREATE INDEX idx_chunk_structures_chunk ON chunk_structures(chunk_id, updated_at DESC)'
    );
  }

  if (!(await indexExists(db, 'chunk_plots', 'idx_chunk_plots_chunk'))) {
    await db.execute('CREATE INDEX idx_chunk_plots_chunk ON chunk_plots(chunk_id, updated_at DESC)');
  }

  if (!(await indexExists(db, 'chunk_change_log', 'idx_chunk_change_log_realm'))) {
    await db.execute(
      'CREATE INDEX idx_chunk_change_log_realm ON chunk_change_log(realm_id, created_at DESC)'
    );
  }

  if (!(await indexExists(db, 'plot_permissions', 'idx_plot_permissions_plot'))) {
    await db.execute('CREATE INDEX idx_plot_permissions_plot ON plot_permissions(plot_id)');
  }
}

export const id = '300_terrain_schema';
export const name = 'Create terrain schema (chunks + edits + plots)';
