import type { DbExecutor } from '../database';

export async function up(db: DbExecutor): Promise<void> {
  await db.execute(
    `CREATE TABLE IF NOT EXISTS character_build_states (
      id VARCHAR(36) PRIMARY KEY,
      realm_id VARCHAR(36) NOT NULL,
      character_id VARCHAR(36) NOT NULL,
      plots_json LONGTEXT NOT NULL DEFAULT '[]',
      constructions_json LONGTEXT NOT NULL DEFAULT '[]',
      updated_at VARCHAR(32) NOT NULL,
      UNIQUE KEY uniq_character_build_state (realm_id, character_id),
      FOREIGN KEY (realm_id) REFERENCES realms(id) ON DELETE CASCADE,
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );
}

export const id = '002_character_build_states';
export const name = 'Add character build state storage';
