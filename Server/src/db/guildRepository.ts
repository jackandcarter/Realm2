import { randomUUID } from 'crypto';
import { db, DbExecutor } from './database';
import { GuildRole } from '../config/gameEnums';

export interface Guild {
  id: string;
  realmId: string;
  name: string;
  createdAt: string;
}

export interface GuildMember {
  id: string;
  guildId: string;
  characterId: string;
  role: GuildRole;
  joinedAt: string;
}

function mapGuildRow(row: any): Guild {
  return {
    id: row.id,
    realmId: row.realm_id,
    name: row.name,
    createdAt: row.created_at,
  };
}

function mapGuildMemberRow(row: any): GuildMember {
  return {
    id: row.id,
    guildId: row.guild_id,
    characterId: row.character_id,
    role: row.role,
    joinedAt: row.joined_at,
  };
}

export async function createGuild(
  realmId: string,
  name: string,
  leaderCharacterId: string,
  executor: DbExecutor = db
): Promise<Guild> {
  const now = new Date().toISOString();
  const guild: Guild = {
    id: randomUUID(),
    realmId,
    name,
    createdAt: now,
  };

  await executor.execute(
    `INSERT INTO guilds (id, realm_id, name, created_at)
     VALUES (?, ?, ?, ?)`,
    [guild.id, guild.realmId, guild.name, guild.createdAt]
  );

  await executor.execute(
    `INSERT INTO guild_members (id, guild_id, character_id, role, joined_at)
     VALUES (?, ?, ?, ?, ?)`,
    [randomUUID(), guild.id, leaderCharacterId, 'leader', now]
  );

  return guild;
}

export async function findGuildById(
  guildId: string,
  executor: DbExecutor = db
): Promise<Guild | undefined> {
  const rows = await executor.query(
    `SELECT id, realm_id, name, created_at
     FROM guilds
     WHERE id = ?`,
    [guildId]
  );
  const row = rows[0];
  return row ? mapGuildRow(row) : undefined;
}

export async function listGuildsForCharacter(
  characterId: string,
  executor: DbExecutor = db
): Promise<Guild[]> {
  const rows = await executor.query(
    `SELECT g.id, g.realm_id, g.name, g.created_at
     FROM guilds g
     INNER JOIN guild_members gm ON gm.guild_id = g.id
     WHERE gm.character_id = ?
     ORDER BY g.name ASC`,
    [characterId]
  );
  return rows.map(mapGuildRow);
}

export async function listGuildMembers(
  guildId: string,
  executor: DbExecutor = db
): Promise<GuildMember[]> {
  const rows = await executor.query(
    `SELECT id, guild_id, character_id, role, joined_at
     FROM guild_members
     WHERE guild_id = ?
     ORDER BY joined_at ASC`,
    [guildId]
  );
  return rows.map(mapGuildMemberRow);
}

export async function upsertGuildMember(
  guildId: string,
  characterId: string,
  role: GuildRole,
  executor: DbExecutor = db
): Promise<GuildMember> {
  const now = new Date().toISOString();
  const existingRows = await executor.query(
    `SELECT id, guild_id, character_id, role, joined_at
     FROM guild_members
     WHERE guild_id = ? AND character_id = ?`,
    [guildId, characterId]
  );
  const existing = existingRows[0];

  if (existing) {
    await executor.execute(
      `UPDATE guild_members
       SET role = ?
       WHERE id = ?`,
      [role, existing.id]
    );
    return {
      id: existing.id,
      guildId,
      characterId,
      role,
      joinedAt: existing.joined_at,
    };
  }

  const member: GuildMember = {
    id: randomUUID(),
    guildId,
    characterId,
    role,
    joinedAt: now,
  };

  await executor.execute(
    `INSERT INTO guild_members (id, guild_id, character_id, role, joined_at)
     VALUES (?, ?, ?, ?, ?)`,
    [member.id, member.guildId, member.characterId, member.role, member.joinedAt]
  );

  return member;
}
