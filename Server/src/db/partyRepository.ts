import { randomUUID } from 'crypto';
import { db, DbExecutor } from './database';
import { PartyRole } from '../config/gameEnums';

export interface Party {
  id: string;
  realmId: string;
  leaderCharacterId: string;
  createdAt: string;
  updatedAt: string;
}

export interface PartyMember {
  id: string;
  partyId: string;
  characterId: string;
  role: PartyRole;
  joinedAt: string;
}

function mapPartyRow(row: any): Party {
  return {
    id: row.id,
    realmId: row.realm_id,
    leaderCharacterId: row.leader_character_id,
    createdAt: row.created_at,
    updatedAt: row.updated_at,
  };
}

function mapPartyMemberRow(row: any): PartyMember {
  return {
    id: row.id,
    partyId: row.party_id,
    characterId: row.character_id,
    role: row.role,
    joinedAt: row.joined_at,
  };
}

export async function createParty(
  realmId: string,
  leaderCharacterId: string,
  executor: DbExecutor = db
): Promise<Party> {
  const now = new Date().toISOString();
  const party: Party = {
    id: randomUUID(),
    realmId,
    leaderCharacterId,
    createdAt: now,
    updatedAt: now,
  };

  await executor.execute(
    `INSERT INTO parties (id, realm_id, leader_character_id, created_at, updated_at)
     VALUES (?, ?, ?, ?, ?)`,
    [party.id, party.realmId, party.leaderCharacterId, party.createdAt, party.updatedAt]
  );

  await executor.execute(
    `INSERT INTO party_members (id, party_id, character_id, role, joined_at)
     VALUES (?, ?, ?, ?, ?)`,
    [randomUUID(), party.id, leaderCharacterId, 'leader', now]
  );

  return party;
}

export async function findPartyById(
  partyId: string,
  executor: DbExecutor = db
): Promise<Party | undefined> {
  const rows = await executor.query(
    `SELECT id, realm_id, leader_character_id, created_at, updated_at
     FROM parties
     WHERE id = ?`,
    [partyId]
  );
  const row = rows[0];
  return row ? mapPartyRow(row) : undefined;
}

export async function listPartyMembers(
  partyId: string,
  executor: DbExecutor = db
): Promise<PartyMember[]> {
  const rows = await executor.query(
    `SELECT id, party_id, character_id, role, joined_at
     FROM party_members
     WHERE party_id = ?
     ORDER BY joined_at ASC`,
    [partyId]
  );
  return rows.map(mapPartyMemberRow);
}

export async function upsertPartyMember(
  partyId: string,
  characterId: string,
  role: PartyRole,
  executor: DbExecutor = db
): Promise<PartyMember> {
  const now = new Date().toISOString();
  const existingRows = await executor.query(
    `SELECT id, party_id, character_id, role, joined_at
     FROM party_members
     WHERE party_id = ? AND character_id = ?`,
    [partyId, characterId]
  );
  const existing = existingRows[0];

  if (existing) {
    await executor.execute(
      `UPDATE party_members
       SET role = ?
       WHERE id = ?`,
      [role, existing.id]
    );
    return {
      id: existing.id,
      partyId,
      characterId,
      role,
      joinedAt: existing.joined_at,
    };
  }

  const member: PartyMember = {
    id: randomUUID(),
    partyId,
    characterId,
    role,
    joinedAt: now,
  };

  await executor.execute(
    `INSERT INTO party_members (id, party_id, character_id, role, joined_at)
     VALUES (?, ?, ?, ?, ?)`,
    [member.id, member.partyId, member.characterId, member.role, member.joinedAt]
  );

  return member;
}

export async function listPartiesForCharacter(
  characterId: string,
  executor: DbExecutor = db
): Promise<Party[]> {
  const rows = await executor.query(
    `SELECT p.id, p.realm_id, p.leader_character_id, p.created_at, p.updated_at
     FROM parties p
     INNER JOIN party_members pm ON pm.party_id = p.id
     WHERE pm.character_id = ?
     ORDER BY p.created_at DESC`,
    [characterId]
  );
  return rows.map(mapPartyRow);
}
