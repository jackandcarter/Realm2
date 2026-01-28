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
      FOREIGN KEY (realm_id) REFERENCES realms(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS realm_resource_wallets (
      id VARCHAR(36) PRIMARY KEY,
      realm_id VARCHAR(36) NOT NULL,
      user_id VARCHAR(36) NOT NULL,
      resource_type VARCHAR(128) NOT NULL,
      quantity INT NOT NULL DEFAULT 0,
      updated_at VARCHAR(32) NOT NULL,
      UNIQUE KEY uniq_resource_wallet (realm_id, user_id, resource_type),
      FOREIGN KEY (realm_id) REFERENCES realms(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS characters (
      id VARCHAR(36) PRIMARY KEY,
      user_id VARCHAR(36) NOT NULL,
      realm_id VARCHAR(36) NOT NULL,
      name VARCHAR(120) NOT NULL,
      bio TEXT,
      race_id ENUM('human', 'felarian', 'crystallian', 'revenant', 'gearling') NOT NULL DEFAULT 'human',
      appearance_json LONGTEXT NOT NULL DEFAULT '{}',
      class_id VARCHAR(64),
      class_states_json LONGTEXT NOT NULL DEFAULT '[]',
      last_location TEXT,
      created_at VARCHAR(32) NOT NULL,
      UNIQUE KEY uniq_character_name (user_id, realm_id, name),
      FOREIGN KEY (realm_id) REFERENCES realms(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

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
    `CREATE TABLE IF NOT EXISTS character_inventory_state (
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
    `CREATE TABLE IF NOT EXISTS character_equipment_state (
      character_id VARCHAR(36) PRIMARY KEY,
      class_id VARCHAR(64) NOT NULL,
      version INT NOT NULL DEFAULT 0,
      updated_at VARCHAR(32) NOT NULL,
      UNIQUE KEY uniq_character_equipment (character_id, class_id),
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS character_equipment_items (
      id VARCHAR(36) PRIMARY KEY,
      character_id VARCHAR(36) NOT NULL,
      class_id VARCHAR(64) NOT NULL,
      slot ENUM('weapon', 'head', 'chest', 'legs', 'hands', 'feet', 'accessory', 'tool') NOT NULL,
      item_id VARCHAR(64) NOT NULL,
      metadata_json LONGTEXT NOT NULL DEFAULT '{}',
      UNIQUE KEY uniq_character_equipment_item (character_id, class_id, slot),
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS character_build_states (
      id VARCHAR(36) PRIMARY KEY,
      realm_id VARCHAR(36) NOT NULL,
      character_id VARCHAR(36) NOT NULL,
      plots_json LONGTEXT NOT NULL DEFAULT '[]',
      constructions_json LONGTEXT NOT NULL DEFAULT '[]',
      updated_at VARCHAR(32) NOT NULL,
      UNIQUE KEY uniq_character_realm (character_id, realm_id),
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

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

  await db.execute(
    `CREATE TABLE IF NOT EXISTS character_quest_state_meta (
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
      quest_id VARCHAR(128) NOT NULL,
      status ENUM('active', 'completed', 'failed') NOT NULL DEFAULT 'active',
      progress_json LONGTEXT NOT NULL DEFAULT '{}',
      updated_at VARCHAR(32) NOT NULL,
      UNIQUE KEY uniq_character_quest (character_id, quest_id),
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

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

  await db.execute(
    `CREATE TABLE IF NOT EXISTS character_action_requests (
      id CHAR(36) PRIMARY KEY,
      character_id CHAR(36) NOT NULL,
      realm_id CHAR(36),
      requested_by CHAR(36) NOT NULL,
      request_type ENUM('progression.update', 'quest.complete') NOT NULL,
      payload_json LONGTEXT NOT NULL,
      status ENUM('pending', 'processing', 'completed', 'rejected') NOT NULL DEFAULT 'pending',
      error_message TEXT,
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL,
      resolved_at VARCHAR(32),
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE,
      FOREIGN KEY (realm_id) REFERENCES realms(id) ON DELETE SET NULL
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS currencies (
      id VARCHAR(64) PRIMARY KEY,
      name VARCHAR(120) NOT NULL,
      description TEXT,
      is_premium TINYINT(1) NOT NULL DEFAULT 0,
      icon_url VARCHAR(255),
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS character_currencies (
      id VARCHAR(36) PRIMARY KEY,
      character_id VARCHAR(36) NOT NULL,
      currency_id VARCHAR(64) NOT NULL,
      balance INT NOT NULL DEFAULT 0,
      updated_at VARCHAR(32) NOT NULL,
      UNIQUE KEY uniq_character_currency (character_id, currency_id),
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE,
      FOREIGN KEY (currency_id) REFERENCES currencies(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS vendors (
      id VARCHAR(64) PRIMARY KEY,
      realm_id VARCHAR(36) NOT NULL,
      name VARCHAR(120) NOT NULL,
      metadata_json LONGTEXT NOT NULL DEFAULT '{}',
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL,
      FOREIGN KEY (realm_id) REFERENCES realms(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS vendor_items (
      id VARCHAR(36) PRIMARY KEY,
      vendor_id VARCHAR(64) NOT NULL,
      item_id VARCHAR(64) NOT NULL,
      price INT NOT NULL DEFAULT 0,
      currency_id VARCHAR(64),
      stock INT NOT NULL DEFAULT 0,
      metadata_json LONGTEXT NOT NULL DEFAULT '{}',
      UNIQUE KEY uniq_vendor_item (vendor_id, item_id),
      FOREIGN KEY (vendor_id) REFERENCES vendors(id) ON DELETE CASCADE,
      FOREIGN KEY (currency_id) REFERENCES currencies(id) ON DELETE SET NULL
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS trades (
      id VARCHAR(36) PRIMARY KEY,
      realm_id VARCHAR(36) NOT NULL,
      initiator_character_id VARCHAR(36) NOT NULL,
      target_character_id VARCHAR(36) NOT NULL,
      initiator_accepted TINYINT(1) NOT NULL DEFAULT 0,
      target_accepted TINYINT(1) NOT NULL DEFAULT 0,
      status ENUM('pending', 'accepted', 'cancelled', 'completed') NOT NULL DEFAULT 'pending',
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL,
      FOREIGN KEY (realm_id) REFERENCES realms(id) ON DELETE CASCADE,
      FOREIGN KEY (initiator_character_id) REFERENCES characters(id) ON DELETE CASCADE,
      FOREIGN KEY (target_character_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS trade_items (
      id VARCHAR(36) PRIMARY KEY,
      trade_id VARCHAR(36) NOT NULL,
      character_id VARCHAR(36) NOT NULL,
      item_id VARCHAR(64) NOT NULL,
      quantity INT NOT NULL DEFAULT 1,
      metadata_json LONGTEXT NOT NULL DEFAULT '{}',
      UNIQUE KEY uniq_trade_item (trade_id, character_id, item_id),
      FOREIGN KEY (trade_id) REFERENCES trades(id) ON DELETE CASCADE,
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS guilds (
      id VARCHAR(36) PRIMARY KEY,
      realm_id VARCHAR(36) NOT NULL,
      name VARCHAR(120) NOT NULL,
      created_at VARCHAR(32) NOT NULL,
      UNIQUE KEY uniq_guild_name (realm_id, name),
      FOREIGN KEY (realm_id) REFERENCES realms(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS guild_members (
      id VARCHAR(36) PRIMARY KEY,
      guild_id VARCHAR(36) NOT NULL,
      character_id VARCHAR(36) NOT NULL,
      role ENUM('leader', 'officer', 'member') NOT NULL DEFAULT 'member',
      joined_at VARCHAR(32) NOT NULL,
      UNIQUE KEY uniq_guild_member (guild_id, character_id),
      FOREIGN KEY (guild_id) REFERENCES guilds(id) ON DELETE CASCADE,
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS friends (
      id VARCHAR(36) PRIMARY KEY,
      character_id VARCHAR(36) NOT NULL,
      friend_character_id VARCHAR(36) NOT NULL,
      status ENUM('pending', 'accepted', 'blocked') NOT NULL DEFAULT 'pending',
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL,
      UNIQUE KEY uniq_friend_pair (character_id, friend_character_id),
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE,
      FOREIGN KEY (friend_character_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS parties (
      id VARCHAR(36) PRIMARY KEY,
      realm_id VARCHAR(36) NOT NULL,
      leader_character_id VARCHAR(36) NOT NULL,
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL,
      FOREIGN KEY (realm_id) REFERENCES realms(id) ON DELETE CASCADE,
      FOREIGN KEY (leader_character_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS party_members (
      id VARCHAR(36) PRIMARY KEY,
      party_id VARCHAR(36) NOT NULL,
      character_id VARCHAR(36) NOT NULL,
      role ENUM('leader', 'member') NOT NULL DEFAULT 'member',
      joined_at VARCHAR(32) NOT NULL,
      UNIQUE KEY uniq_party_member (party_id, character_id),
      FOREIGN KEY (party_id) REFERENCES parties(id) ON DELETE CASCADE,
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS mail (
      id VARCHAR(36) PRIMARY KEY,
      realm_id VARCHAR(36) NOT NULL,
      from_character_id VARCHAR(36) NOT NULL,
      to_character_id VARCHAR(36) NOT NULL,
      subject VARCHAR(120) NOT NULL,
      body TEXT,
      sent_at VARCHAR(32) NOT NULL,
      read_at VARCHAR(32),
      FOREIGN KEY (realm_id) REFERENCES realms(id) ON DELETE CASCADE,
      FOREIGN KEY (from_character_id) REFERENCES characters(id) ON DELETE CASCADE,
      FOREIGN KEY (to_character_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS mail_items (
      id VARCHAR(36) PRIMARY KEY,
      mail_id VARCHAR(36) NOT NULL,
      item_id VARCHAR(64) NOT NULL,
      quantity INT NOT NULL DEFAULT 1,
      metadata_json LONGTEXT NOT NULL DEFAULT '{}',
      FOREIGN KEY (mail_id) REFERENCES mail(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS chat_channels (
      id VARCHAR(36) PRIMARY KEY,
      realm_id VARCHAR(36) NOT NULL,
      name VARCHAR(120) NOT NULL,
      type ENUM('global', 'party', 'guild', 'direct', 'system') NOT NULL DEFAULT 'global',
      created_at VARCHAR(32) NOT NULL,
      UNIQUE KEY uniq_chat_channel (realm_id, name, type),
      FOREIGN KEY (realm_id) REFERENCES realms(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS chat_messages (
      id VARCHAR(36) PRIMARY KEY,
      channel_id VARCHAR(36) NOT NULL,
      character_id VARCHAR(36) NOT NULL,
      message TEXT NOT NULL,
      sent_at VARCHAR(32) NOT NULL,
      FOREIGN KEY (channel_id) REFERENCES chat_channels(id) ON DELETE CASCADE,
      FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS items (
      id VARCHAR(64) PRIMARY KEY,
      name VARCHAR(120) NOT NULL,
      description TEXT,
      category ENUM('weapon', 'armor', 'consumable', 'key-item') NOT NULL,
      rarity ENUM('common', 'starter', 'standard', 'rare', 'legendary') NOT NULL DEFAULT 'common',
      stack_limit INT NOT NULL DEFAULT 1,
      icon_url VARCHAR(255),
      metadata_json LONGTEXT NOT NULL DEFAULT '{}',
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS weapon_types (
      id VARCHAR(64) PRIMARY KEY,
      display_name VARCHAR(120) NOT NULL,
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS weapons (
      item_id VARCHAR(64) PRIMARY KEY,
      weapon_type VARCHAR(64) NOT NULL,
      handedness ENUM('one-hand', 'two-hand', 'off-hand') NOT NULL DEFAULT 'one-hand',
      min_damage INT NOT NULL DEFAULT 0,
      max_damage INT NOT NULL DEFAULT 0,
      attack_speed FLOAT NOT NULL DEFAULT 1,
      range_meters FLOAT NOT NULL DEFAULT 1,
      required_level INT NOT NULL DEFAULT 1,
      required_class_id VARCHAR(64),
      metadata_json LONGTEXT NOT NULL DEFAULT '{}',
      FOREIGN KEY (item_id) REFERENCES items(id) ON DELETE CASCADE,
      FOREIGN KEY (weapon_type) REFERENCES weapon_types(id) ON DELETE RESTRICT
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS armor (
      item_id VARCHAR(64) PRIMARY KEY,
      slot ENUM('weapon', 'head', 'chest', 'legs', 'hands', 'feet', 'accessory', 'tool') NOT NULL,
      armor_type ENUM('cloth', 'leather', 'plate') NOT NULL,
      defense INT NOT NULL DEFAULT 0,
      resistances_json LONGTEXT NOT NULL DEFAULT '{}',
      required_level INT NOT NULL DEFAULT 1,
      required_class_id VARCHAR(64),
      metadata_json LONGTEXT NOT NULL DEFAULT '{}',
      FOREIGN KEY (item_id) REFERENCES items(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS classes (
      id VARCHAR(64) PRIMARY KEY,
      name VARCHAR(120) NOT NULL,
      description TEXT,
      role ENUM('tank', 'damage', 'support', 'builder'),
      resource_type ENUM('mana', 'stamina', 'energy'),
      starting_level INT NOT NULL DEFAULT 1,
      metadata_json LONGTEXT NOT NULL DEFAULT '{}',
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS class_base_stats (
      class_id VARCHAR(64) PRIMARY KEY,
      base_health INT NOT NULL DEFAULT 0,
      base_mana INT NOT NULL DEFAULT 0,
      strength INT NOT NULL DEFAULT 0,
      agility INT NOT NULL DEFAULT 0,
      intelligence INT NOT NULL DEFAULT 0,
      vitality INT NOT NULL DEFAULT 0,
      defense INT NOT NULL DEFAULT 0,
      crit_chance FLOAT NOT NULL DEFAULT 0,
      speed FLOAT NOT NULL DEFAULT 0,
      updated_at VARCHAR(32) NOT NULL,
      FOREIGN KEY (class_id) REFERENCES classes(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS enemies (
      id VARCHAR(64) PRIMARY KEY,
      name VARCHAR(120) NOT NULL,
      description TEXT,
      enemy_type VARCHAR(64),
      level INT NOT NULL DEFAULT 1,
      faction VARCHAR(64),
      is_boss TINYINT(1) NOT NULL DEFAULT 0,
      metadata_json LONGTEXT NOT NULL DEFAULT '{}',
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS enemy_base_stats (
      enemy_id VARCHAR(64) PRIMARY KEY,
      base_health INT NOT NULL DEFAULT 0,
      base_mana INT NOT NULL DEFAULT 0,
      attack INT NOT NULL DEFAULT 0,
      defense INT NOT NULL DEFAULT 0,
      agility INT NOT NULL DEFAULT 0,
      crit_chance FLOAT NOT NULL DEFAULT 0,
      xp_reward INT NOT NULL DEFAULT 0,
      gold_reward INT NOT NULL DEFAULT 0,
      updated_at VARCHAR(32) NOT NULL,
      FOREIGN KEY (enemy_id) REFERENCES enemies(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS ability_types (
      id VARCHAR(64) PRIMARY KEY,
      display_name VARCHAR(120) NOT NULL,
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS abilities (
      id VARCHAR(64) PRIMARY KEY,
      name VARCHAR(120) NOT NULL,
      description TEXT,
      ability_type VARCHAR(64),
      cooldown_seconds FLOAT NOT NULL DEFAULT 0,
      resource_cost INT NOT NULL DEFAULT 0,
      range_meters FLOAT NOT NULL DEFAULT 0,
      cast_time_seconds FLOAT NOT NULL DEFAULT 0,
      metadata_json LONGTEXT NOT NULL DEFAULT '{}',
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL,
      FOREIGN KEY (ability_type) REFERENCES ability_types(id) ON DELETE SET NULL
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS level_progression (
      level INT PRIMARY KEY,
      xp_required INT NOT NULL DEFAULT 0,
      total_xp INT NOT NULL DEFAULT 0,
      hp_gain INT NOT NULL DEFAULT 0,
      mana_gain INT NOT NULL DEFAULT 0,
      stat_points INT NOT NULL DEFAULT 0,
      reward_json LONGTEXT NOT NULL DEFAULT '{}',
      updated_at VARCHAR(32) NOT NULL
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS races (
      id VARCHAR(64) PRIMARY KEY,
      display_name VARCHAR(120) NOT NULL,
      customization_json LONGTEXT NOT NULL DEFAULT '{}',
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS race_class_rules (
      race_id VARCHAR(64) NOT NULL,
      class_id VARCHAR(64) NOT NULL,
      unlock_method VARCHAR(32) NOT NULL,
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL,
      PRIMARY KEY (race_id, class_id),
      FOREIGN KEY (race_id) REFERENCES races(id) ON DELETE CASCADE,
      FOREIGN KEY (class_id) REFERENCES classes(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  await db.execute(
    `CREATE TABLE IF NOT EXISTS class_weapon_proficiencies (
      class_id VARCHAR(64) NOT NULL,
      weapon_type VARCHAR(64) NOT NULL,
      created_at VARCHAR(32) NOT NULL,
      PRIMARY KEY (class_id, weapon_type),
      FOREIGN KEY (class_id) REFERENCES classes(id) ON DELETE CASCADE,
      FOREIGN KEY (weapon_type) REFERENCES weapon_types(id) ON DELETE RESTRICT
    ) ENGINE=InnoDB;`
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

  await db.execute(
    `CREATE TABLE IF NOT EXISTS resource_types (
      id VARCHAR(128) PRIMARY KEY,
      display_name VARCHAR(120) NOT NULL,
      category VARCHAR(64) NOT NULL,
      created_at VARCHAR(32) NOT NULL,
      updated_at VARCHAR(32) NOT NULL
    ) ENGINE=InnoDB;`
  );

  if (!(await constraintExists(db, 'realm_resource_wallets', 'fk_resource_wallets_resource_type'))) {
    await db.execute(
      `ALTER TABLE realm_resource_wallets
       ADD CONSTRAINT fk_resource_wallets_resource_type
       FOREIGN KEY (resource_type) REFERENCES resource_types(id) ON DELETE RESTRICT`
    );
  }

  await db.execute(
    `CREATE TABLE IF NOT EXISTS combat_client_times (
      user_id VARCHAR(36) NOT NULL,
      caster_id VARCHAR(36) NOT NULL,
      last_client_time FLOAT NOT NULL DEFAULT 0,
      updated_at VARCHAR(32) NOT NULL,
      PRIMARY KEY (user_id, caster_id),
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
      resource_type ENUM('mana', 'stamina', 'energy') NOT NULL,
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
      event_kind ENUM('damage', 'heal', 'stateApplied') NOT NULL,
      amount FLOAT,
      state_id VARCHAR(64),
      duration_seconds FLOAT,
      created_at VARCHAR(32) NOT NULL,
      FOREIGN KEY (caster_id) REFERENCES characters(id) ON DELETE CASCADE,
      FOREIGN KEY (target_id) REFERENCES characters(id) ON DELETE CASCADE
    ) ENGINE=InnoDB;`
  );

  if (!(await indexExists(db, 'realm_resource_wallets', 'idx_resource_wallet_lookup'))) {
    await db.execute(
      'CREATE INDEX idx_resource_wallet_lookup ON realm_resource_wallets(realm_id, user_id, resource_type)'
    );
  }

  if (!(await indexExists(db, 'items', 'idx_items_category'))) {
    await db.execute('CREATE INDEX idx_items_category ON items(category)');
  }

  if (!(await indexExists(db, 'weapons', 'idx_weapons_type'))) {
    await db.execute('CREATE INDEX idx_weapons_type ON weapons(weapon_type)');
  }

  if (!(await indexExists(db, 'armor', 'idx_armor_slot'))) {
    await db.execute('CREATE INDEX idx_armor_slot ON armor(slot)');
  }

  if (!(await indexExists(db, 'classes', 'idx_classes_role'))) {
    await db.execute('CREATE INDEX idx_classes_role ON classes(role)');
  }

  if (!(await indexExists(db, 'enemies', 'idx_enemies_type'))) {
    await db.execute('CREATE INDEX idx_enemies_type ON enemies(enemy_type)');
  }

  if (!(await indexExists(db, 'abilities', 'idx_abilities_type'))) {
    await db.execute('CREATE INDEX idx_abilities_type ON abilities(ability_type)');
  }

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

  if (!(await indexExists(db, 'realm_build_zones', 'idx_build_zones_realm'))) {
    await db.execute('CREATE INDEX idx_build_zones_realm ON realm_build_zones(realm_id)');
  }

  if (!(await indexExists(db, 'character_dock_layouts', 'idx_character_layout_key'))) {
    await db.execute(
      'CREATE INDEX idx_character_layout_key ON character_dock_layouts(character_id, layout_key)'
    );
  }

  if (!(await indexExists(db, 'character_map_pin_states', 'idx_map_pin_character'))) {
    await db.execute(
      'CREATE INDEX idx_map_pin_character ON character_map_pin_states(character_id, updated_at DESC)'
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

  if (!(await indexExists(db, 'currencies', 'idx_character_currencies_character'))) {
    await db.execute(
      'CREATE INDEX idx_character_currencies_character ON character_currencies(character_id, currency_id)'
    );
  }

  if (!(await indexExists(db, 'trades', 'idx_trades_realm'))) {
    await db.execute('CREATE INDEX idx_trades_realm ON trades(realm_id, updated_at DESC)');
  }

  if (!(await indexExists(db, 'trade_items', 'idx_trade_items_trade'))) {
    await db.execute('CREATE INDEX idx_trade_items_trade ON trade_items(trade_id)');
  }

  if (!(await indexExists(db, 'guild_members', 'idx_guild_members_character'))) {
    await db.execute('CREATE INDEX idx_guild_members_character ON guild_members(character_id)');
  }

  if (!(await indexExists(db, 'friends', 'idx_friends_character'))) {
    await db.execute('CREATE INDEX idx_friends_character ON friends(character_id)');
  }

  if (!(await indexExists(db, 'party_members', 'idx_party_members_character'))) {
    await db.execute('CREATE INDEX idx_party_members_character ON party_members(character_id)');
  }

  if (!(await indexExists(db, 'mail', 'idx_mail_recipient'))) {
    await db.execute('CREATE INDEX idx_mail_recipient ON mail(to_character_id, sent_at DESC)');
  }

  if (!(await indexExists(db, 'chat_messages', 'idx_chat_messages_channel'))) {
    await db.execute(
      'CREATE INDEX idx_chat_messages_channel ON chat_messages(channel_id, sent_at DESC)'
    );
  }

  if (!(await indexExists(db, 'race_class_rules', 'idx_race_class_rules_class'))) {
    await db.execute('CREATE INDEX idx_race_class_rules_class ON race_class_rules(class_id)');
  }

  if (!(await indexExists(db, 'class_weapon_proficiencies', 'idx_class_weapon_type'))) {
    await db.execute(
      'CREATE INDEX idx_class_weapon_type ON class_weapon_proficiencies(weapon_type)'
    );
  }

  if (!(await indexExists(db, 'resource_types', 'idx_resource_types_category'))) {
    await db.execute('CREATE INDEX idx_resource_types_category ON resource_types(category)');
  }

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
    await db.execute('CREATE INDEX idx_combat_event_request ON combat_event_logs(request_id)');
  }
}

export const id = '200_world_schema';
export const name = 'Create world schema (base install)';
