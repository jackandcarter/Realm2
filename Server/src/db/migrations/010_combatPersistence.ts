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
    `CREATE TABLE IF NOT EXISTS combat_client_times (
      user_id VARCHAR(36) NOT NULL,
      caster_id VARCHAR(36) NOT NULL,
      last_client_time FLOAT NOT NULL DEFAULT 0,
      updated_at VARCHAR(32) NOT NULL,
      PRIMARY KEY (user_id, caster_id),
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
      FOREIGN KEY (caster_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS combat_ability_cooldowns (
      character_id VARCHAR(36) NOT NULL,
      ability_id VARCHAR(64) NOT NULL,
      last_used_at FLOAT NOT NULL DEFAULT 0,
      updated_at VARCHAR(32) NOT NULL,
      PRIMARY KEY (character_id, ability_id),
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE,
      FOREIGN KEY (ability_id) REFERENCES abilities(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS character_resource_state (
      character_id VARCHAR(36) NOT NULL,
      resource_type VARCHAR(64) NOT NULL,
      current_value INT NOT NULL DEFAULT 0,
      max_value INT NOT NULL DEFAULT 0,
      updated_at VARCHAR(32) NOT NULL,
      PRIMARY KEY (character_id, resource_type),
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS combat_event_logs (
      id VARCHAR(36) PRIMARY KEY,
      request_id VARCHAR(64) NOT NULL,
      ability_id VARCHAR(64) NOT NULL,
      caster_id VARCHAR(36) NOT NULL,
      target_id VARCHAR(36) NOT NULL,
      event_kind VARCHAR(32) NOT NULL,
      amount FLOAT,
      state_id VARCHAR(64),
      duration_seconds FLOAT,
      created_at VARCHAR(32) NOT NULL,
      FOREIGN KEY (caster_id) REFERENCES characters(id) ON DELETE CASCADE,
      FOREIGN KEY (target_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  if (!(await indexExists(db, 'combat_event_logs', 'idx_combat_event_caster'))) {
    await db.execute(
      'CREATE INDEX idx_combat_event_caster ON combat_event_logs(caster_id, created_at DESC)'
    );
  }

  if (!(await indexExists(db, 'combat_event_logs', 'idx_combat_event_target'))) {
    await db.execute(
      'CREATE INDEX idx_combat_event_target ON combat_event_logs(target_id, created_at DESC)'
    );
  }

  if (!(await indexExists(db, 'combat_event_logs', 'idx_combat_event_request'))) {
    await db.execute(
      'CREATE INDEX idx_combat_event_request ON combat_event_logs(request_id)'
    );
  }
}

export const id = '010_combat_persistence';
export const name = 'Add combat persistence tables';
