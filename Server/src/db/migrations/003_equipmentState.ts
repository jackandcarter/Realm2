import type { DbExecutor } from '../database';

export async function up(db: DbExecutor): Promise<void> {
  await db.execute(
    `CREATE TABLE IF NOT EXISTS character_equipment_state (
      character_id VARCHAR(36) PRIMARY KEY,
      version INT NOT NULL DEFAULT 0,
      updated_at VARCHAR(32) NOT NULL,
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS character_equipment_items (
      id VARCHAR(36) PRIMARY KEY,
      character_id VARCHAR(36) NOT NULL,
      class_id VARCHAR(64) NOT NULL,
      slot VARCHAR(32) NOT NULL,
      item_id VARCHAR(64) NOT NULL,
      metadata_json LONGTEXT NOT NULL DEFAULT '{}',
      UNIQUE KEY uniq_character_equipment_slot (character_id, class_id, slot),
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );
}

export const id = '003_character_equipment_state';
export const name = 'Add character equipment state storage';
