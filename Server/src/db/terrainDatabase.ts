import mysql, { Pool, PoolConnection, ResultSetHeader, RowDataPacket } from 'mysql2/promise';
import { env } from '../config/env';
import { measurePersistenceOperationAsync } from '../observability/metrics';
import { logger } from '../observability/logger';
import type { DbExecutor, Database } from './database';
import { runTerrainMigrations } from './terrainMigrationRunner';

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

const terrainDb: Database = {
  async query() {
    throw new Error('Terrain database not initialized');
  },
  async execute() {
    throw new Error('Terrain database not initialized');
  },
  async withTransaction() {
    throw new Error('Terrain database not initialized');
  },
};

let pool: Pool | undefined;

export async function initializeTerrainDatabase(): Promise<void> {
  if (pool) {
    return;
  }
  pool = mysql.createPool({
    host: env.terrainDatabaseHost,
    port: env.terrainDatabasePort,
    user: env.terrainDatabaseUser,
    password: env.terrainDatabasePassword,
    database: env.terrainDatabaseName,
    waitForConnections: true,
    connectionLimit: env.terrainDatabaseConnectionLimit,
    ssl: env.terrainDatabaseSsl ? { rejectUnauthorized: false } : undefined,
  });

  const executor = createExecutor(pool);
  terrainDb.query = executor.query;
  terrainDb.execute = executor.execute;
  terrainDb.withTransaction = async <T>(fn: (tx: DbExecutor) => Promise<T>) => {
    if (!pool) {
      throw new Error('Terrain database pool not initialized');
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

  await runTerrainMigrations(terrainDb);
  logger.info('Terrain database connection established');
}

export { terrainDb };
