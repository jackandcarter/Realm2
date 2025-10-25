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

export function findMembership(userId: string, realmId: string): RealmMembership | undefined {
  const stmt = db.prepare(
    `SELECT id, realm_id as realmId, user_id as userId, role, created_at as createdAt
     FROM realm_memberships
     WHERE user_id = ? AND realm_id = ?`
  );
  return stmt.get(userId, realmId) as RealmMembership | undefined;
}

export function createMembership(
  userId: string,
  realmId: string,
  role: RealmRole = 'player'
): RealmMembership {
  const membership: RealmMembership = {
    id: randomUUID(),
    realmId,
    userId,
    role,
    createdAt: new Date().toISOString(),
  };

  const stmt = db.prepare(
    `INSERT INTO realm_memberships (id, realm_id, user_id, role, created_at)
     VALUES (@id, @realmId, @userId, @role, @createdAt)`
  );
  stmt.run(membership);
  return membership;
}

export function upsertMembership(
  userId: string,
  realmId: string,
  role: RealmRole
): RealmMembership {
  const existing = findMembership(userId, realmId);
  if (existing) {
    const stmt = db.prepare(
      `UPDATE realm_memberships SET role = @role WHERE id = @id`
    );
    stmt.run({ id: existing.id, role });
    return { ...existing, role };
  }
  return createMembership(userId, realmId, role);
}

export function removeMembership(userId: string, realmId: string): void {
  const stmt = db.prepare(
    'DELETE FROM realm_memberships WHERE user_id = ? AND realm_id = ?'
  );
  stmt.run(userId, realmId);
}
