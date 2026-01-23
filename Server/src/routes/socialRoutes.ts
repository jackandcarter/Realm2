import { Router } from 'express';
import { requireAuth } from '../middleware/authMiddleware';
import { HttpError } from '../utils/errors';
import {
  acceptFriendRequest,
  addGuildMemberForCharacter,
  addPartyMemberForCharacter,
  createChannelForRealm,
  createGuildForCharacter,
  createPartyForCharacter,
  listChannelMessages,
  listChannelsForRealm,
  listFriends,
  listGuilds,
  listMailForCharacter,
  listMailItemsForCharacter,
  listParties,
  listPartyMembersForCharacter,
  markMailReadForCharacter,
  sendChatMessage,
  sendFriendRequest,
  sendMailFromCharacter,
} from '../services/socialService';

export const socialRouter = Router();

/**
 * @openapi
 * /social/guilds/{characterId}:
 *   get:
 *     summary: List guilds for a character.
 *     tags:
 *       - Social
 */
socialRouter.get('/guilds/:characterId', requireAuth, async (req, res, next) => {
  try {
    const { characterId } = req.params as { characterId: string };
    const guilds = await listGuilds(req.user!.id, characterId);
    res.json({ guilds });
  } catch (error) {
    next(error);
  }
});

/**
 * @openapi
 * /social/guilds:
 *   post:
 *     summary: Create a guild.
 *     tags:
 *       - Social
 */
socialRouter.post('/guilds', requireAuth, async (req, res, next) => {
  try {
    const { realmId, name, leaderCharacterId } = req.body ?? {};
    if (typeof realmId !== 'string' || typeof name !== 'string' || typeof leaderCharacterId !== 'string') {
      throw new HttpError(400, 'realmId, name, and leaderCharacterId are required');
    }
    const guild = await createGuildForCharacter(req.user!.id, realmId, name, leaderCharacterId);
    res.status(201).json({ guild });
  } catch (error) {
    next(error);
  }
});

socialRouter.post('/guilds/:guildId/members', requireAuth, async (req, res, next) => {
  try {
    const { guildId } = req.params as { guildId: string };
    const { actorCharacterId, memberCharacterId, role } = req.body ?? {};
    if (typeof actorCharacterId !== 'string' || typeof memberCharacterId !== 'string') {
      throw new HttpError(400, 'actorCharacterId and memberCharacterId are required');
    }
    const member = await addGuildMemberForCharacter(
      req.user!.id,
      guildId,
      actorCharacterId,
      memberCharacterId,
      typeof role === 'string' ? role : 'member'
    );
    res.json({ member });
  } catch (error) {
    next(error);
  }
});

/**
 * @openapi
 * /social/friends/{characterId}:
 *   get:
 *     summary: List friends for a character.
 *     tags:
 *       - Social
 */
socialRouter.get('/friends/:characterId', requireAuth, async (req, res, next) => {
  try {
    const { characterId } = req.params as { characterId: string };
    const friends = await listFriends(req.user!.id, characterId);
    res.json({ friends });
  } catch (error) {
    next(error);
  }
});

socialRouter.post('/friends/request', requireAuth, async (req, res, next) => {
  try {
    const { characterId, friendCharacterId } = req.body ?? {};
    if (typeof characterId !== 'string' || typeof friendCharacterId !== 'string') {
      throw new HttpError(400, 'characterId and friendCharacterId are required');
    }
    const request = await sendFriendRequest(req.user!.id, characterId, friendCharacterId);
    res.status(201).json({ request });
  } catch (error) {
    next(error);
  }
});

socialRouter.post('/friends/accept', requireAuth, async (req, res, next) => {
  try {
    const { characterId, friendCharacterId } = req.body ?? {};
    if (typeof characterId !== 'string' || typeof friendCharacterId !== 'string') {
      throw new HttpError(400, 'characterId and friendCharacterId are required');
    }
    const friends = await acceptFriendRequest(req.user!.id, characterId, friendCharacterId);
    res.json({ friends });
  } catch (error) {
    next(error);
  }
});

/**
 * @openapi
 * /social/parties/{characterId}:
 *   get:
 *     summary: List parties for a character.
 *     tags:
 *       - Social
 */
socialRouter.get('/parties/:characterId', requireAuth, async (req, res, next) => {
  try {
    const { characterId } = req.params as { characterId: string };
    const parties = await listParties(req.user!.id, characterId);
    res.json({ parties });
  } catch (error) {
    next(error);
  }
});

socialRouter.post('/parties', requireAuth, async (req, res, next) => {
  try {
    const { realmId, leaderCharacterId } = req.body ?? {};
    if (typeof realmId !== 'string' || typeof leaderCharacterId !== 'string') {
      throw new HttpError(400, 'realmId and leaderCharacterId are required');
    }
    const party = await createPartyForCharacter(req.user!.id, realmId, leaderCharacterId);
    res.status(201).json({ party });
  } catch (error) {
    next(error);
  }
});

socialRouter.post('/parties/:partyId/members', requireAuth, async (req, res, next) => {
  try {
    const { partyId } = req.params as { partyId: string };
    const { characterId, role } = req.body ?? {};
    if (typeof characterId !== 'string') {
      throw new HttpError(400, 'characterId is required');
    }
    const member = await addPartyMemberForCharacter(
      req.user!.id,
      partyId,
      characterId,
      typeof role === 'string' ? role : 'member'
    );
    res.json({ member });
  } catch (error) {
    next(error);
  }
});

socialRouter.get('/parties/:partyId/members/:characterId', requireAuth, async (req, res, next) => {
  try {
    const { partyId, characterId } = req.params as { partyId: string; characterId: string };
    const members = await listPartyMembersForCharacter(req.user!.id, partyId, characterId);
    res.json({ members });
  } catch (error) {
    next(error);
  }
});

/**
 * @openapi
 * /social/mail/{characterId}:
 *   get:
 *     summary: List mail for a character.
 *     tags:
 *       - Social
 */
socialRouter.get('/mail/:characterId', requireAuth, async (req, res, next) => {
  try {
    const { characterId } = req.params as { characterId: string };
    const mail = await listMailForCharacter(req.user!.id, characterId);
    res.json({ mail });
  } catch (error) {
    next(error);
  }
});

socialRouter.post('/mail', requireAuth, async (req, res, next) => {
  try {
    const { realmId, fromCharacterId, toCharacterId, subject, body, items } = req.body ?? {};
    if (
      typeof realmId !== 'string' ||
      typeof fromCharacterId !== 'string' ||
      typeof toCharacterId !== 'string' ||
      typeof subject !== 'string'
    ) {
      throw new HttpError(400, 'realmId, fromCharacterId, toCharacterId, and subject are required');
    }
    const attachments = Array.isArray(items) ? items : [];
    const result = await sendMailFromCharacter(
      req.user!.id,
      realmId,
      fromCharacterId,
      toCharacterId,
      subject,
      typeof body === 'string' ? body : undefined,
      attachments
    );
    res.status(201).json(result);
  } catch (error) {
    next(error);
  }
});

socialRouter.post('/mail/:mailId/read', requireAuth, async (req, res, next) => {
  try {
    const { mailId } = req.params as { mailId: string };
    const { characterId } = req.body ?? {};
    if (typeof characterId !== 'string') {
      throw new HttpError(400, 'characterId is required');
    }
    await markMailReadForCharacter(req.user!.id, characterId, mailId);
    res.status(204).send();
  } catch (error) {
    next(error);
  }
});

socialRouter.get('/mail/:mailId/items/:characterId', requireAuth, async (req, res, next) => {
  try {
    const { mailId, characterId } = req.params as { mailId: string; characterId: string };
    const items = await listMailItemsForCharacter(req.user!.id, characterId, mailId);
    res.json({ items });
  } catch (error) {
    next(error);
  }
});

/**
 * @openapi
 * /social/chat/{realmId}/channels:
 *   get:
 *     summary: List chat channels for a realm.
 *     tags:
 *       - Social
 */
socialRouter.get('/chat/:realmId/channels', requireAuth, async (req, res, next) => {
  try {
    const { realmId } = req.params as { realmId: string };
    const channels = await listChannelsForRealm(req.user!.id, realmId);
    res.json({ channels });
  } catch (error) {
    next(error);
  }
});

socialRouter.post('/chat/:realmId/channels', requireAuth, async (req, res, next) => {
  try {
    const { realmId } = req.params as { realmId: string };
    const { name, type } = req.body ?? {};
    if (typeof name !== 'string') {
      throw new HttpError(400, 'name is required');
    }
    const channel = await createChannelForRealm(req.user!.id, realmId, name, typeof type === 'string' ? type : 'global');
    res.status(201).json({ channel });
  } catch (error) {
    next(error);
  }
});

socialRouter.post('/chat/channels/:channelId/messages', requireAuth, async (req, res, next) => {
  try {
    const { channelId } = req.params as { channelId: string };
    const { characterId, message } = req.body ?? {};
    if (typeof characterId !== 'string' || typeof message !== 'string') {
      throw new HttpError(400, 'characterId and message are required');
    }
    const chatMessage = await sendChatMessage(req.user!.id, channelId, characterId, message);
    res.status(201).json({ message: chatMessage });
  } catch (error) {
    next(error);
  }
});

socialRouter.get('/chat/channels/:channelId/messages/:characterId', requireAuth, async (req, res, next) => {
  try {
    const { channelId, characterId } = req.params as { channelId: string; characterId: string };
    const limit = Number(req.query?.limit ?? 50);
    const messages = await listChannelMessages(req.user!.id, channelId, characterId, limit);
    res.json({ messages });
  } catch (error) {
    next(error);
  }
});
