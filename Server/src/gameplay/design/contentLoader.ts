import { createHash } from 'crypto';
import { promises as fs } from 'fs';
import path from 'path';
import { logger } from '../../observability/logger';
import {
  ContentCatalog,
  applyContentCatalog,
  validateContentCatalog,
} from './contentCatalog';

export async function loadContentCatalogFromDisk(
  catalogPath: string,
): Promise<ContentCatalog | null> {
  if (!catalogPath) {
    logger.info('No content catalog path configured; skipping content sync.');
    return null;
  }

  const resolvedPath = path.isAbsolute(catalogPath)
    ? catalogPath
    : path.resolve(process.cwd(), catalogPath);

  try {
    await fs.stat(resolvedPath);
  } catch (_error) {
    logger.info({ path: resolvedPath }, 'Content catalog not found; skipping content sync.');
    return null;
  }

  const raw = await fs.readFile(resolvedPath, 'utf8');
  const hash = createHash('sha256').update(raw).digest('hex');
  const parsed = JSON.parse(raw) as ContentCatalog;
  if (!parsed.meta) {
    parsed.meta = { generatedAt: new Date().toISOString() };
  }
  parsed.meta.version = hash;

  const issues = validateContentCatalog(parsed);
  if (issues.length > 0) {
    const details = issues.map((issue) => `${issue.level.toUpperCase()}: ${issue.message}`).join('\n');
    throw new Error(`Content catalog validation failed:\n${details}`);
  }

  applyContentCatalog(parsed, hash);
  logger.info({ path: resolvedPath, version: hash }, 'Content catalog loaded.');
  return parsed;
}
