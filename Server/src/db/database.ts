import mysql, { Pool, PoolConnection, ResultSetHeader, RowDataPacket } from 'mysql2/promise';
import { env } from '../config/env';
import { runWorldMigrations } from './worldMigrationRunner';
import { seedCatalogData } from './catalogSeeder';
import { seedRaceCatalogData } from './raceCatalogSeeder';
import { seedReferenceData } from './referenceDataSeeder';
import { seedDefaultCurrencies } from './currencySeeder';
import { measurePersistenceOperationAsync } from '../observability/metrics';
import { logger } from '../observability/logger';

export interface DbExecutor {
  query<T = RowDataPacket[]>(sql: string, params?: unknown[]): Promise<T>;
  execute<T = ResultSetHeader>(sql: string, params?: unknown[]): Promise<T>;
}

export interface Database extends DbExecutor {
  withTransaction<T>(fn: (tx: DbExecutor) => Promise<T>): Promise<T>;
}

function deriveOperationLabel(sql: string): string {
  const normalized = sql.trim().toLowerCase();
  const firstToken = normalized.split(/\s+/)[0] ?? 'raw';
  const tableMatch =
    normalized.match(/into\s+([^\s(]+)/) ??
    normalized.match(/from\s+([^\s(]+)/) ??
    normalized.match(/update\s+([^\s(]+)/) ??
    normalized.match(/table\s+([^\s(]+)/);
  const table = tableMatch ? tableMatch[1] : 'unknown';
  return `${firstToken}.${table}`;
}

function createExecutor(source: Pool | PoolConnection): DbExecutor {
  return {
    async query<T = RowDataPacket[]>(sql: string, params: unknown[] = []) {
      return measurePersistenceOperationAsync(deriveOperationLabel(sql), async () => {
        const [rows] = await source.execute<RowDataPacket[]>(sql, params);
        return rows as T;
      });
    },
    async execute<T = ResultSetHeader>(sql: string, params: unknown[] = []) {
      return measurePersistenceOperationAsync(deriveOperationLabel(sql), async () => {
        const [result] = await source.execute<ResultSetHeader>(sql, params);
        return result as T;
      });
    },
  };
}

const db: Database = {
  async query() {
    throw new Error('Database not initialized');
  },
  async execute() {
    throw new Error('Database not initialized');
  },
  async withTransaction() {
    throw new Error('Database not initialized');
  },
};

let pool: Pool | undefined;

async function seedRealms(): Promise<void> {
  const now = new Date().toISOString();
  const seededRealms = [
    {
      id: 'realm-elysium-nexus',
      name: 'Elysium Nexus',
      narrative:
        'A realm caught between magic and machinery where the Chrono Nexus warps time itself, drawing heroes and traitors alike into its shimmering core.',
    },
    {
      id: 'realm-arcane-haven',
      name: 'Arcane Haven',
      narrative:
        'Deep within the Eldros forests, scholars and rangers safeguard ancient libraries while whispers of Seraphina Frostwind guide seekers of hidden lore.',
    },
    {
      id: 'realm-gearspring',
      name: 'Gearspring Metropolis',
      narrative:
        'Floating platforms and technomagical forges define this Gearling stronghold where innovation is the only currency that matters.',
    },
  ];

  for (const realm of seededRealms) {
    await db.execute(
      `INSERT INTO realms (id, name, narrative, created_at)
       VALUES (?, ?, ?, ?)
       ON DUPLICATE KEY UPDATE name = VALUES(name), narrative = VALUES(narrative)`,
      [realm.id, realm.name, realm.narrative, now]
    );
  }
}

export async function initializeWorldDatabase(): Promise<void> {
  if (pool) {
    return;
  }
  pool = mysql.createPool({
    host: env.databaseHost,
    port: env.databasePort,
    user: env.databaseUser,
    password: env.databasePassword,
    database: env.databaseName,
    waitForConnections: true,
    connectionLimit: env.databaseConnectionLimit,
    ssl: env.databaseSsl ? { rejectUnauthorized: false } : undefined,
  });

  const executor = createExecutor(pool);
  db.query = executor.query;
  db.execute = executor.execute;
  db.withTransaction = async <T>(fn: (tx: DbExecutor) => Promise<T>) => {
    if (!pool) {
      throw new Error('Database pool not initialized');
    }
    const connection = await pool.getConnection();
    const tx = createExecutor(connection);
    try {
      await connection.beginTransaction();
      const result = await fn(tx);
      await connection.commit();
      return result;
    } catch (error) {
      await connection.rollback();
      throw error;
    } finally {
      connection.release();
    }
  };

  await runWorldMigrations(db);
  await seedRealms();
  await seedReferenceData(db);
  await seedCatalogData(db);
  await seedRaceCatalogData(db);
  await seedDefaultCurrencies(db);
  logger.info('Database connection established');
}

export async function initializeDatabase(): Promise<void> {
  await initializeWorldDatabase();
}

export { db };

export async function resetDatabase(): Promise<void> {
  await initializeDatabase();
  await db.execute('DELETE FROM chunk_change_log');
  await db.execute('DELETE FROM chunk_structures');
  await db.execute('DELETE FROM chunk_plots');
  await db.execute('DELETE FROM plot_permissions');
  await db.execute('DELETE FROM plot_ownerships');
  await db.execute('DELETE FROM realm_chunks');
  await db.execute('DELETE FROM realm_build_zones');
  await db.execute('DELETE FROM realm_resource_wallets');
  await db.execute('DELETE FROM character_dock_layouts');
  await db.execute('DELETE FROM character_build_states');
  await db.execute('DELETE FROM character_map_pin_states');
  await db.execute('DELETE FROM character_map_pin_state_meta');
  await db.execute('DELETE FROM character_quest_states');
  await db.execute('DELETE FROM character_quest_state_meta');
  await db.execute('DELETE FROM character_action_requests');
  await db.execute('DELETE FROM character_equipment_items');
  await db.execute('DELETE FROM character_equipment_state');
  await db.execute('DELETE FROM character_inventory_items');
  await db.execute('DELETE FROM character_inventory_state');
  await db.execute('DELETE FROM trade_items');
  await db.execute('DELETE FROM trades');
  await db.execute('DELETE FROM vendor_items');
  await db.execute('DELETE FROM vendors');
  await db.execute('DELETE FROM character_currencies');
  await db.execute('DELETE FROM currencies');
  await db.execute('DELETE FROM chat_messages');
  await db.execute('DELETE FROM chat_channels');
  await db.execute('DELETE FROM mail_items');
  await db.execute('DELETE FROM mail');
  await db.execute('DELETE FROM party_members');
  await db.execute('DELETE FROM parties');
  await db.execute('DELETE FROM friends');
  await db.execute('DELETE FROM guild_members');
  await db.execute('DELETE FROM guilds');
  await db.execute('DELETE FROM character_class_unlocks');
  await db.execute('DELETE FROM character_class_unlock_state');
  await db.execute('DELETE FROM character_progression');
  await db.execute('DELETE FROM class_weapon_proficiencies');
  await db.execute('DELETE FROM race_class_rules');
  await db.execute('DELETE FROM races');
  await db.execute('DELETE FROM ability_types');
  await db.execute('DELETE FROM resource_types');
  await db.execute('DELETE FROM weapon_types');
  await db.execute('DELETE FROM level_progression');
  await db.execute('DELETE FROM abilities');
  await db.execute('DELETE FROM enemy_base_stats');
  await db.execute('DELETE FROM enemies');
  await db.execute('DELETE FROM class_base_stats');
  await db.execute('DELETE FROM classes');
  await db.execute('DELETE FROM armor');
  await db.execute('DELETE FROM weapons');
  await db.execute('DELETE FROM items');
  await db.execute('DELETE FROM characters');
  await db.execute('DELETE FROM realm_memberships');
  await db.execute('DELETE FROM realms');
  await seedRealms();
  await seedDefaultCurrencies(db);
}
