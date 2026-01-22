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
    `CREATE TABLE IF NOT EXISTS character_action_requests (
      id CHAR(36) PRIMARY KEY,
      character_id CHAR(36) NOT NULL,
      realm_id CHAR(36),
      requested_by CHAR(36) NOT NULL,
      request_type VARCHAR(64) NOT NULL,
      payload_json LONGTEXT NOT NULL,
      status ENUM('pending', 'processing', 'completed', 'rejected') NOT NULL DEFAULT 'pending',
      error_message TEXT,
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL,
      resolved_at VARCHAR(32),
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE,
      FOREIGN KEY (requested_by) REFERENCES users(id) ON DELETE CASCADE,
      FOREIGN KEY (realm_id) REFERENCES realms(id) ON DELETE SET NULL
    ) ENGINE=InnoDB;`
  );

  if (!(await indexExists(db, 'character_action_requests', 'idx_action_requests_character'))) {
    await db.execute(
      'CREATE INDEX idx_action_requests_character ON character_action_requests(character_id)'
    );
  }

  if (!(await indexExists(db, 'character_action_requests', 'idx_action_requests_status'))) {
    await db.execute(
      'CREATE INDEX idx_action_requests_status ON character_action_requests(status)'
    );
  }

  if (!(await indexExists(db, 'character_action_requests', 'idx_action_requests_type'))) {
    await db.execute(
      'CREATE INDEX idx_action_requests_type ON character_action_requests(request_type)'
    );
  }
}

export const id = '007_action_requests';
export const name = 'Add action request queue for client intents';
