import { db } from './database';

export interface Realm {
  id: string;
  name: string;
  narrative: string;
  createdAt: string;
}

export async function listRealms(): Promise<Realm[]> {
  return db.query<Realm[]>(
    'SELECT id, name, narrative, created_at as createdAt FROM realms ORDER BY name ASC'
  );
}

export async function findRealmById(id: string): Promise<Realm | undefined> {
  const rows = await db.query<Realm[]>(
    'SELECT id, name, narrative, created_at as createdAt FROM realms WHERE id = ?',
    [id]
  );
  return rows[0];
}
