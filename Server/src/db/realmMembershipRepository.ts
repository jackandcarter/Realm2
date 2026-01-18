import { randomUUID } from 'crypto';
import { db } from './database';

export type RealmRole = 'player' | 'builder';

export interface RealmMembership {
  id: string;
  realmId: string;
  userId: string;
  role: RealmRole;
  createdAt: string;
}

export async function findMembership(
  userId: string,
  realmId: string
): Promise<RealmMembership | undefined> {
  const rows = await db.query<RealmMembership[]>(
    `SELECT id, realm_id as realmId, user_id as userId, role, created_at as createdAt
     FROM realm_memberships
     WHERE user_id = ? AND realm_id = ?`,
    [userId, realmId]
  );
  return rows[0];
}

export async function createMembership(
  userId: string,
  realmId: string,
  role: RealmRole = 'player'
): Promise<RealmMembership> {
  const membership: RealmMembership = {
    id: randomUUID(),
    realmId,
    userId,
    role,
    createdAt: new Date().toISOString(),
  };

  await db.execute(
    `INSERT INTO realm_memberships (id, realm_id, user_id, role, created_at)
     VALUES (?, ?, ?, ?, ?)`,
    [membership.id, membership.realmId, membership.userId, membership.role, membership.createdAt]
  );
  return membership;
}

export async function upsertMembership(
  userId: string,
  realmId: string,
  role: RealmRole
): Promise<RealmMembership> {
  const existing = await findMembership(userId, realmId);
  if (existing) {
    await db.execute('UPDATE realm_memberships SET role = ? WHERE id = ?', [
      role,
      existing.id,
    ]);
    return { ...existing, role };
  }
  return createMembership(userId, realmId, role);
}

export async function removeMembership(userId: string, realmId: string): Promise<void> {
  await db.execute('DELETE FROM realm_memberships WHERE user_id = ? AND realm_id = ?', [
    userId,
    realmId,
  ]);
}
