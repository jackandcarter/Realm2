import type { DbExecutor } from '../database';

async function constraintExists(
  db: DbExecutor,
  table: string,
  constraint: string
): Promise<boolean> {
  const rows = await db.query<{ total: number }[]>(
    `SELECT COUNT(*) as total
     FROM information_schema.table_constraints
     WHERE table_schema = DATABASE() AND table_name = ? AND constraint_name = ?`,
    [table, constraint]
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
    `UPDATE characters
     LEFT JOIN classes ON characters.class_id = classes.id
     SET characters.class_id = NULL
     WHERE characters.class_id IS NOT NULL AND classes.id IS NULL`
  );

  await db.execute(
    `DELETE unlocks
     FROM character_class_unlocks unlocks
     LEFT JOIN classes ON unlocks.class_id = classes.id
     WHERE classes.id IS NULL`
  );

  await db.execute(
    `DELETE equipment
     FROM character_equipment_items equipment
     LEFT JOIN items ON equipment.item_id = items.id
     LEFT JOIN classes ON equipment.class_id = classes.id
     WHERE items.id IS NULL OR classes.id IS NULL`
  );

  await db.execute(
    `DELETE inventory
     FROM character_inventory_items inventory
     LEFT JOIN items ON inventory.item_id = items.id
     WHERE items.id IS NULL`
  );

  if (!(await constraintExists(db, 'characters', 'fk_characters_class'))) {
    await db.execute(
      `ALTER TABLE characters
       ADD CONSTRAINT fk_characters_class
       FOREIGN KEY (class_id) REFERENCES classes(id) ON DELETE SET NULL`
    );
  }

  if (!(await constraintExists(db, 'character_class_unlocks', 'fk_character_class_unlocks_class'))) {
    await db.execute(
      `ALTER TABLE character_class_unlocks
       ADD CONSTRAINT fk_character_class_unlocks_class
       FOREIGN KEY (class_id) REFERENCES classes(id) ON DELETE CASCADE`
    );
  }

  if (!(await constraintExists(db, 'character_equipment_items', 'fk_character_equipment_items_class'))) {
    await db.execute(
      `ALTER TABLE character_equipment_items
       ADD CONSTRAINT fk_character_equipment_items_class
       FOREIGN KEY (class_id) REFERENCES classes(id) ON DELETE CASCADE`
    );
  }

  if (!(await constraintExists(db, 'character_equipment_items', 'fk_character_equipment_items_item'))) {
    await db.execute(
      `ALTER TABLE character_equipment_items
       ADD CONSTRAINT fk_character_equipment_items_item
       FOREIGN KEY (item_id) REFERENCES items(id) ON DELETE CASCADE`
    );
  }

  if (!(await constraintExists(db, 'character_inventory_items', 'fk_character_inventory_items_item'))) {
    await db.execute(
      `ALTER TABLE character_inventory_items
       ADD CONSTRAINT fk_character_inventory_items_item
       FOREIGN KEY (item_id) REFERENCES items(id) ON DELETE CASCADE`
    );
  }

  if (!(await indexExists(db, 'character_inventory_items', 'idx_character_inventory_item'))) {
    await db.execute(
      'CREATE INDEX idx_character_inventory_item ON character_inventory_items(item_id)'
    );
  }

  if (!(await indexExists(db, 'character_equipment_items', 'idx_character_equipment_item'))) {
    await db.execute(
      'CREATE INDEX idx_character_equipment_item ON character_equipment_items(item_id)'
    );
  }
}

export const id = '009_character_persistence';
export const name = 'Add character persistence relationships for classes and items';
