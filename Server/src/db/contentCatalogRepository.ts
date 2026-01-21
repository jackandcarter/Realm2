import { randomUUID } from 'crypto';
import { db } from './database';

interface ContentCatalogStateRow {
  activeVersion: string | null;
  updatedAt: string;
}

export async function ensureContentCatalogState(): Promise<void> {
  const rows = await db.query<ContentCatalogStateRow[]>(
    `SELECT active_version as activeVersion, updated_at as updatedAt
     FROM content_catalog_state
     WHERE id = 1`,
  );
  if (rows.length === 0) {
    await db.execute(
      `INSERT INTO content_catalog_state (id, active_version, updated_at)
       VALUES (1, NULL, ?)`,
      [new Date().toISOString()],
    );
  }
}

export async function getActiveContentCatalogVersion(): Promise<string | null> {
  const rows = await db.query<ContentCatalogStateRow[]>(
    `SELECT active_version as activeVersion, updated_at as updatedAt
     FROM content_catalog_state
     WHERE id = 1`,
  );
  return rows[0]?.activeVersion ?? null;
}

export async function upsertContentCatalog(version: string, catalogJson: string): Promise<void> {
  if (!version || !catalogJson) {
    return;
  }

  await ensureContentCatalogState();

  const existing = await getActiveContentCatalogVersion();
  if (existing === version) {
    return;
  }

  const now = new Date().toISOString();
  await db.withTransaction(async (tx) => {
    await tx.execute(
      `INSERT IGNORE INTO content_catalog_snapshots (id, version, catalog_json, created_at)
       VALUES (?, ?, ?, ?)`,
      [randomUUID(), version, catalogJson, now],
    );

    await tx.execute(
      `UPDATE content_catalog_state
       SET active_version = ?, updated_at = ?
       WHERE id = 1`,
      [version, now],
    );
  });
}
