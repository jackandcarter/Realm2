import { db } from './database';

export interface Realm {
  id: string;
  name: string;
  narrative: string;
  createdAt: string;
}

export function listRealms(): Realm[] {
  const stmt = db.prepare(
    'SELECT id, name, narrative, created_at as createdAt FROM realms ORDER BY name ASC'
  );
  return stmt.all() as Realm[];
}

export function findRealmById(id: string): Realm | undefined {
  const stmt = db.prepare(
    'SELECT id, name, narrative, created_at as createdAt FROM realms WHERE id = ?'
  );
  return stmt.get(id) as Realm | undefined;
}
