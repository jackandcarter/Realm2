import mysql, { Pool, PoolConnection, ResultSetHeader, RowDataPacket } from 'mysql2/promise';
import { env } from '../config/env';
import { runMigrations } from './migrationRunner';
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

export async function initializeDatabase(): Promise<void> {
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

  await runMigrations(db);
  await seedRealms();
  logger.info('Database connection established');
}

export { db };

export async function resetDatabase(): Promise<void> {
  await initializeDatabase();
  await db.execute('DELETE FROM refresh_tokens');
  await db.execute('DELETE FROM chunk_change_log');
  await db.execute('DELETE FROM chunk_structures');
  await db.execute('DELETE FROM chunk_plots');
  await db.execute('DELETE FROM realm_chunks');
  await db.execute('DELETE FROM realm_resource_wallets');
  await db.execute('DELETE FROM character_build_states');
  await db.execute('DELETE FROM character_quest_states');
  await db.execute('DELETE FROM character_quest_state_meta');
  await db.execute('DELETE FROM character_equipment_items');
  await db.execute('DELETE FROM character_equipment_state');
  await db.execute('DELETE FROM character_inventory_items');
  await db.execute('DELETE FROM character_inventory_state');
  await db.execute('DELETE FROM character_class_unlocks');
  await db.execute('DELETE FROM character_class_unlock_state');
  await db.execute('DELETE FROM character_progression');
  await db.execute('DELETE FROM characters');
  await db.execute('DELETE FROM realm_memberships');
  await db.execute('DELETE FROM realms');
  await db.execute('DELETE FROM users');
  await seedRealms();
}
