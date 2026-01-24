import { db } from '../db/database';
import {
  createGuild,
  findGuildById,
  listGuildMembers,
  listGuildsForCharacter,
  upsertGuildMember,
  Guild,
  GuildMember,
} from '../db/guildRepository';
import {
  listFriendsForCharacter,
  upsertFriendRelationship,
  FriendRecord,
} from '../db/friendRepository';
import {
  createParty,
  findPartyById,
  listPartiesForCharacter,
  listPartyMembers,
  upsertPartyMember,
  Party,
  PartyMember,
} from '../db/partyRepository';
import {
  addMailItem,
  listMailForCharacter as listMailForCharacterRecords,
  listMailItems as listMailItemsForMail,
  markMailRead,
  sendMail,
  MailItem,
  MailMessage,
} from '../db/mailRepository';
import {
  addChatMessage,
  createChatChannel,
  findChatChannelById,
  listChatChannels,
  listChatMessages,
  ChatChannel,
  ChatMessage,
} from '../db/chatRepository';
import { requireCharacter, requireOwnedCharacter } from './characterAccessService';
import { ensureMembership } from './realmService';
import { ChatChannelType, chatChannelTypes } from '../config/gameEnums';
import { HttpError } from '../utils/errors';

export async function listGuilds(userId: string, characterId: string): Promise<Guild[]> {
  await requireOwnedCharacter(userId, characterId);
  return listGuildsForCharacter(characterId);
}

export async function createGuildForCharacter(
  userId: string,
  realmId: string,
  name: string,
  leaderCharacterId: string
): Promise<Guild> {
  const leader = await requireOwnedCharacter(userId, leaderCharacterId);
  if (leader.realmId !== realmId) {
    throw new HttpError(400, 'Leader character must be in the requested realm');
  }
  await ensureMembership(userId, realmId);
  const trimmedName = name.trim();
  if (!trimmedName) {
    throw new HttpError(400, 'Guild name is required');
  }

  return createGuild(realmId, trimmedName, leaderCharacterId);
}

export async function addGuildMemberForCharacter(
  userId: string,
  guildId: string,
  actorCharacterId: string,
  memberCharacterId: string,
  role: string
): Promise<GuildMember> {
  const guild = await requireGuild(guildId);
  const actorMembership = await listGuildMembers(guildId);
  const actor = actorMembership.find((member) => member.characterId === actorCharacterId);
  if (!actor) {
    throw new HttpError(403, 'Character is not in the guild');
  }

  const actorCharacter = await requireOwnedCharacter(userId, actorCharacterId);
  if (actorCharacter.realmId !== guild.realmId) {
    throw new HttpError(400, 'Character is not in the same realm as the guild');
  }

  if (!['leader', 'officer'].includes(actor.role)) {
    throw new HttpError(403, 'Only guild leaders or officers can add members');
  }

  const memberCharacter = await requireCharacter(memberCharacterId);
  if (memberCharacter.realmId !== guild.realmId) {
    throw new HttpError(400, 'Member must be in the same realm');
  }

  return upsertGuildMember(guildId, memberCharacter.id, role || 'member');
}

export async function listFriends(userId: string, characterId: string): Promise<FriendRecord[]> {
  await requireOwnedCharacter(userId, characterId);
  return listFriendsForCharacter(characterId);
}

export async function sendFriendRequest(
  userId: string,
  characterId: string,
  friendCharacterId: string
): Promise<FriendRecord> {
  await requireOwnedCharacter(userId, characterId);
  await requireCharacter(friendCharacterId);
  if (characterId === friendCharacterId) {
    throw new HttpError(400, 'Cannot add yourself as a friend');
  }

  return upsertFriendRelationship(characterId, friendCharacterId, 'pending');
}

export async function acceptFriendRequest(
  userId: string,
  characterId: string,
  friendCharacterId: string
): Promise<FriendRecord[]> {
  await requireOwnedCharacter(userId, characterId);
  await requireCharacter(friendCharacterId);

  const now = await db.withTransaction(async (tx) => {
    const accepted = await upsertFriendRelationship(
      characterId,
      friendCharacterId,
      'accepted',
      tx
    );
    const reciprocal = await upsertFriendRelationship(
      friendCharacterId,
      characterId,
      'accepted',
      tx
    );
    return [accepted, reciprocal];
  });

  return now;
}

export async function createPartyForCharacter(
  userId: string,
  realmId: string,
  leaderCharacterId: string
): Promise<Party> {
  const leader = await requireOwnedCharacter(userId, leaderCharacterId);
  if (leader.realmId !== realmId) {
    throw new HttpError(400, 'Leader character must be in the requested realm');
  }
  await ensureMembership(userId, realmId);
  return createParty(realmId, leaderCharacterId);
}

export async function addPartyMemberForCharacter(
  userId: string,
  partyId: string,
  characterId: string,
  role: string
): Promise<PartyMember> {
  const party = await requireParty(partyId);
  const leader = await requireOwnedCharacter(userId, party.leaderCharacterId);
  if (leader.realmId !== party.realmId) {
    throw new HttpError(400, 'Party leader mismatch');
  }

  const member = await requireCharacter(characterId);
  if (member.realmId !== party.realmId) {
    throw new HttpError(400, 'Party member must be in the same realm');
  }

  return upsertPartyMember(partyId, characterId, role || 'member');
}

export async function listParties(userId: string, characterId: string): Promise<Party[]> {
  await requireOwnedCharacter(userId, characterId);
  return listPartiesForCharacter(characterId);
}

export async function listPartyMembersForCharacter(
  userId: string,
  partyId: string,
  characterId: string
): Promise<PartyMember[]> {
  await requireOwnedCharacter(userId, characterId);
  const party = await requireParty(partyId);
  const members = await listPartyMembers(partyId);
  if (!members.some((member) => member.characterId === characterId)) {
    throw new HttpError(403, 'Character is not in this party');
  }
  return members;
}

export async function sendMailFromCharacter(
  userId: string,
  realmId: string,
  fromCharacterId: string,
  toCharacterId: string,
  subject: string,
  body: string | undefined,
  items: { itemId: string; quantity: number; metadataJson?: string }[]
): Promise<{ mail: MailMessage; items: MailItem[] }> {
  const fromCharacter = await requireOwnedCharacter(userId, fromCharacterId);
  const toCharacter = await requireCharacter(toCharacterId);

  if (fromCharacter.realmId !== realmId || toCharacter.realmId !== realmId) {
    throw new HttpError(400, 'Mail participants must be in the same realm');
  }

  const trimmedSubject = subject.trim();
  if (!trimmedSubject) {
    throw new HttpError(400, 'Subject is required');
  }

  const mail = await sendMail(
    realmId,
    fromCharacterId,
    toCharacterId,
    trimmedSubject,
    body?.trim() || null
  );

  const attached: MailItem[] = [];
  for (const item of items) {
    if (!item.itemId.trim() || !Number.isFinite(item.quantity) || item.quantity <= 0) {
      continue;
    }
    attached.push(await addMailItem(mail.id, item.itemId, item.quantity, item.metadataJson));
  }

  return { mail, items: attached };
}

export async function listMailForCharacter(
  userId: string,
  characterId: string
): Promise<MailMessage[]> {
  await requireOwnedCharacter(userId, characterId);
  return listMailForCharacterRecords(characterId);
}

export async function markMailReadForCharacter(
  userId: string,
  characterId: string,
  mailId: string
): Promise<void> {
  await requireOwnedCharacter(userId, characterId);
  const mailbox = await listMailForCharacterRecords(characterId);
  if (!mailbox.find((mail) => mail.id === mailId)) {
    throw new HttpError(404, 'Mail not found');
  }
  await markMailRead(mailId);
}

export async function listMailItemsForCharacter(
  userId: string,
  characterId: string,
  mailId: string
): Promise<MailItem[]> {
  await requireOwnedCharacter(userId, characterId);
  const mailbox = await listMailForCharacterRecords(characterId);
  if (!mailbox.find((mail) => mail.id === mailId)) {
    throw new HttpError(404, 'Mail not found');
  }
  return listMailItemsForMail(mailId);
}

export async function listChannelsForRealm(
  userId: string,
  realmId: string
): Promise<ChatChannel[]> {
  await ensureMembership(userId, realmId);
  return listChatChannels(realmId);
}

export async function createChannelForRealm(
  userId: string,
  realmId: string,
  name: string,
  type: string
): Promise<ChatChannel> {
  await ensureMembership(userId, realmId);
  const trimmedName = name.trim();
  if (!trimmedName) {
    throw new HttpError(400, 'Channel name is required');
  }
  const normalizedType = (type?.trim() || 'global') as ChatChannelType;
  if (!chatChannelTypes.includes(normalizedType)) {
    throw new HttpError(400, `Channel type must be one of: ${chatChannelTypes.join(', ')}`);
  }
  return createChatChannel(realmId, trimmedName, normalizedType);
}

export async function sendChatMessage(
  userId: string,
  channelId: string,
  characterId: string,
  message: string
): Promise<ChatMessage> {
  const channel = await findChatChannelById(channelId);
  if (!channel) {
    throw new HttpError(404, 'Channel not found');
  }

  const character = await requireOwnedCharacter(userId, characterId);
  if (character.realmId !== channel.realmId) {
    throw new HttpError(400, 'Character is not in the same realm as the channel');
  }

  const trimmedMessage = message.trim();
  if (!trimmedMessage) {
    throw new HttpError(400, 'Message is required');
  }

  return addChatMessage(channelId, characterId, trimmedMessage);
}

export async function listChannelMessages(
  userId: string,
  channelId: string,
  characterId: string,
  limit: number
): Promise<ChatMessage[]> {
  const channel = await findChatChannelById(channelId);
  if (!channel) {
    throw new HttpError(404, 'Channel not found');
  }

  const character = await requireOwnedCharacter(userId, characterId);
  if (character.realmId !== channel.realmId) {
    throw new HttpError(400, 'Character is not in the same realm as the channel');
  }

  return listChatMessages(channelId, limit);
}

async function requireGuild(guildId: string): Promise<Guild> {
  const guild = await findGuildById(guildId);
  if (!guild) {
    throw new HttpError(404, 'Guild not found');
  }
  return guild;
}

async function requireParty(partyId: string): Promise<Party> {
  const party = await findPartyById(partyId);
  if (!party) {
    throw new HttpError(404, 'Party not found');
  }
  return party;
}
