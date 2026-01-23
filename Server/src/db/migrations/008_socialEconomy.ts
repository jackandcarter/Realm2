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
      status VARCHAR(32) NOT NULL DEFAULT 'pending',
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
      role VARCHAR(32) NOT NULL DEFAULT 'member',
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
      status VARCHAR(32) NOT NULL DEFAULT 'pending',
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
      role VARCHAR(32) NOT NULL DEFAULT 'member',
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
      type VARCHAR(32) NOT NULL DEFAULT 'global',
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

  if (!(await indexExists(db, 'character_currencies', 'idx_character_currencies_character'))) {
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
    await db.execute('CREATE INDEX idx_chat_messages_channel ON chat_messages(channel_id, sent_at DESC)');
  }
}

export const id = '008_social_economy';
export const name = 'Add social and economy core tables';
