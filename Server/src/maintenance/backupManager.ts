import fs from 'fs';
import fsPromises from 'fs/promises';
import path from 'path';
import type { Database as DatabaseInstance } from 'better-sqlite3';
import { env } from '../config/env';
import { logger } from '../observability/logger';

type SnapshotReason = 'manual' | 'scheduled' | 'pre-deploy';

let boundDb: DatabaseInstance | undefined;
let scheduler: NodeJS.Timeout | undefined;

export function registerBackupDatabase(db: DatabaseInstance): void {
  boundDb = db;
}

async function ensureBackupDir(): Promise<void> {
  await fsPromises.mkdir(env.databaseBackupDir, { recursive: true });
}

function resolveSnapshotPath(filename: string): string {
  if (path.isAbsolute(filename)) {
    return filename;
  }
  return path.join(env.databaseBackupDir, filename);
}

async function pruneOldBackups(): Promise<void> {
  if (env.databaseBackupRetentionDays <= 0) {
    return;
  }
  const entries = await fsPromises.readdir(env.databaseBackupDir);
  const threshold = Date.now() - env.databaseBackupRetentionDays * 24 * 60 * 60 * 1000;
  await Promise.all(
    entries.map(async (entry) => {
      const fullPath = path.join(env.databaseBackupDir, entry);
      const stats = await fsPromises.stat(fullPath);
      if (stats.isFile() && stats.mtimeMs < threshold) {
        await fsPromises.unlink(fullPath).catch(() => undefined);
      }
    })
  );
}

async function copyIfExists(source: string, destination: string): Promise<void> {
  if (fs.existsSync(source)) {
    await fsPromises.copyFile(source, destination);
  }
}

export async function createBackupSnapshot(
  reason: SnapshotReason = 'manual',
  db: DatabaseInstance | undefined = boundDb
): Promise<string> {
  await ensureBackupDir();
  const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
  const baseName = `app-${timestamp}`;
  const dbTarget = path.join(env.databaseBackupDir, `${baseName}.db`);
  const walTarget = path.join(env.databaseBackupDir, `${baseName}.wal`);
  const shmTarget = path.join(env.databaseBackupDir, `${baseName}.shm`);

  if (db) {
    try {
      db.pragma('wal_checkpoint(FULL)');
    } catch (error) {
      logger.warn({ error }, 'Failed to checkpoint WAL before snapshot');
    }
  }

  await fsPromises.copyFile(env.databasePath, dbTarget);
  await copyIfExists(`${env.databasePath}-wal`, walTarget);
  await copyIfExists(`${env.databasePath}-shm`, shmTarget);

  await pruneOldBackups();
  logger.info({ snapshot: baseName, reason }, 'Database snapshot created');
  return dbTarget;
}

export async function restoreFromSnapshot(snapshotName: string): Promise<void> {
  const snapshotPath = resolveSnapshotPath(snapshotName);
  if (!fs.existsSync(snapshotPath)) {
    throw new Error(`Snapshot not found: ${snapshotPath}`);
  }
  const walPath = `${snapshotPath.endsWith('.db') ? snapshotPath.slice(0, -3) : snapshotPath}.wal`;
  const shmPath = `${snapshotPath.endsWith('.db') ? snapshotPath.slice(0, -3) : snapshotPath}.shm`;

  if (boundDb) {
    logger.warn('Closing database connection before restore');
    boundDb.close();
    boundDb = undefined;
  }

  await fsPromises.copyFile(snapshotPath, env.databasePath);
  await copyIfExists(walPath, `${env.databasePath}-wal`);
  await copyIfExists(shmPath, `${env.databasePath}-shm`);

  logger.info({ snapshot: snapshotName }, 'Database restored from snapshot');
}

async function runScheduledBackup(): Promise<void> {
  try {
    await createBackupSnapshot('scheduled');
  } catch (error) {
    logger.error({ err: error }, 'Scheduled backup failed');
  }
}

export function startBackupScheduler(): void {
  if (env.databaseBackupIntervalMinutes <= 0) {
    logger.info('Database backup scheduler disabled');
    return;
  }
  if (scheduler) {
    return;
  }
  const intervalMs = env.databaseBackupIntervalMinutes * 60 * 1000;
  scheduler = setInterval(runScheduledBackup, intervalMs);
  scheduler.unref();
  void runScheduledBackup();
  logger.info({ intervalMinutes: env.databaseBackupIntervalMinutes }, 'Database backup scheduler started');
}
