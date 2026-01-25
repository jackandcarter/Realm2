import { IncomingMessage } from 'http';
import jwt from 'jsonwebtoken';
import { WebSocket, WebSocketServer } from 'ws';
import type { RawData } from 'ws';
import { env } from '../config/env';
import { findCharacterById } from '../db/characterRepository';
import { ActionRequestStatus, createActionRequest } from '../db/actionRequestRepository';
import {
  CharacterProgressionSnapshot,
  ClassUnlockInput,
  InventoryItemInput,
  EquipmentItemInput,
  ForbiddenClassEquipmentError,
  InvalidEquipmentCatalogError,
  InvalidInventoryCatalogError,
  QuestStateInput,
  ForbiddenClassUnlockError,
  VersionConflictError,
  getCharacterProgressionSnapshot,
  initializeCharacterProgressionState,
  grantProgressionExperience,
  grantInventoryItems,
  consumeInventoryItems,
  upsertQuestState,
  replaceClassUnlocks,
  replaceEquipment,
  replaceInventory,
  replaceQuestStates,
  updateProgressionLevels,
} from '../db/progressionRepository';
import { findUserById } from '../db/userRepository';
import { AuthPayload } from '../middleware/authMiddleware';
import { JsonValue } from '../types/characterCustomization';
import { HttpError } from '../utils/errors';
import { resolveQuestCompletionHandler } from '../gameplay/quests/questRegistry';

export interface ProgressionUpdateInput {
  progression?: {
    level: number;
    xp: number;
    expectedVersion: number;
  };
  classUnlocks?: {
    expectedVersion: number;
    unlocks: ClassUnlockInput[];
  };
  inventory?: {
    expectedVersion: number;
    items: InventoryItemInput[];
  };
  equipment?: {
    expectedVersion: number;
    items: EquipmentItemInput[];
  };
  quests?: {
    expectedVersion: number;
    quests: QuestStateInput[];
  };
}

export interface ProgressionIntentResponse {
  requestId: string;
  status: ActionRequestStatus;
  createdAt: string;
}

export interface InventoryItemGrant {
  itemId: string;
  quantity: number;
  metadata?: JsonValue | undefined;
}

export interface InventoryItemConsumption {
  itemId: string;
  quantity: number;
}

export interface QuestCompletionInput {
  questId: string;
  progress?: JsonValue | undefined;
}

interface ProgressionSocketUpdate {
  type: 'update';
  payload: ProgressionUpdateInput & { characterId?: string };
}

interface ProgressionSocketSubscribe {
  type: 'subscribe';
  payload?: { characterId?: string };
}

type ProgressionSocketMessage = ProgressionSocketUpdate | ProgressionSocketSubscribe;

type SocketRegistry = Map<string, Set<WebSocket>>;

const subscribers: SocketRegistry = new Map();

export async function getCharacterProgressionForUser(
  userId: string,
  characterId: string
): Promise<CharacterProgressionSnapshot> {
  const character = await findCharacterById(characterId);
  if (!character) {
    throw new HttpError(404, 'Character not found');
  }

  if (character.userId !== userId) {
    throw new HttpError(403, 'You do not have access to this character');
  }

  await initializeCharacterProgressionState(characterId);
  return getCharacterProgressionSnapshot(characterId);
}

export async function submitProgressionIntentForUser(
  userId: string,
  characterId: string,
  input: ProgressionUpdateInput
): Promise<ProgressionIntentResponse> {
  const character = await findCharacterById(characterId);
  if (!character) {
    throw new HttpError(404, 'Character not found');
  }

  if (character.userId !== userId) {
    throw new HttpError(403, 'You do not have access to this character');
  }

  if (!hasProgressionIntent(input)) {
    throw new HttpError(400, 'Progression update payload is empty');
  }

  const actionRequest = await createActionRequest({
    characterId,
    realmId: character.realmId,
    requestedBy: userId,
    requestType: 'progression.update',
    payload: input,
  });

  return {
    requestId: actionRequest.id,
    status: actionRequest.status,
    createdAt: actionRequest.createdAt,
  };
}

export async function submitQuestCompletionForUser(
  userId: string,
  characterId: string,
  input: QuestCompletionInput
): Promise<ProgressionIntentResponse> {
  const character = await findCharacterById(characterId);
  if (!character) {
    throw new HttpError(404, 'Character not found');
  }

  if (character.userId !== userId) {
    throw new HttpError(403, 'You do not have access to this character');
  }

  if (!input.questId?.trim()) {
    throw new HttpError(400, 'questId is required');
  }

  const actionRequest = await createActionRequest({
    characterId,
    realmId: character.realmId,
    requestedBy: userId,
    requestType: 'quest.complete',
    payload: input,
  });

  return {
    requestId: actionRequest.id,
    status: actionRequest.status,
    createdAt: actionRequest.createdAt,
  };
}

export async function applyProgressionUpdate(
  characterId: string,
  input: ProgressionUpdateInput
): Promise<CharacterProgressionSnapshot> {
  const character = await findCharacterById(characterId);
  if (!character) {
    throw new HttpError(404, 'Character not found');
  }

  await initializeCharacterProgressionState(characterId);

  if (input.progression) {
    const { level, xp, expectedVersion } = input.progression;
    await updateProgressionLevels(characterId, level, xp, expectedVersion);
  }

  if (input.classUnlocks) {
    try {
      await replaceClassUnlocks(
        characterId,
        input.classUnlocks.unlocks,
        input.classUnlocks.expectedVersion
      );
    } catch (error) {
      if (error instanceof ForbiddenClassUnlockError) {
        throw new HttpError(400, error.message);
      }
      throw error;
    }
  }

  if (input.inventory) {
    try {
      await replaceInventory(characterId, input.inventory.items, input.inventory.expectedVersion);
    } catch (error) {
      if (error instanceof InvalidInventoryCatalogError) {
        throw new HttpError(400, error.message);
      }
      throw error;
    }
  }

  if (input.equipment) {
    try {
      await replaceEquipment(characterId, input.equipment.items, input.equipment.expectedVersion);
    } catch (error) {
      if (
        error instanceof ForbiddenClassEquipmentError ||
        error instanceof InvalidEquipmentCatalogError
      ) {
        throw new HttpError(400, error.message);
      }
      throw error;
    }
  }

  if (input.quests) {
    await replaceQuestStates(characterId, input.quests.quests, input.quests.expectedVersion);
  }

  const snapshot = await getCharacterProgressionSnapshot(characterId);
  broadcastProgression(characterId, snapshot);
  return snapshot;
}

export async function applyProgressionXpGrant(
  characterId: string,
  amount: number
): Promise<CharacterProgressionSnapshot> {
  if (!Number.isFinite(amount) || amount <= 0) {
    throw new HttpError(400, 'XP grant amount must be positive');
  }

  await initializeCharacterProgressionState(characterId);
  await grantProgressionExperience(characterId, amount);

  const snapshot = await getCharacterProgressionSnapshot(characterId);
  broadcastProgression(characterId, snapshot);
  return snapshot;
}

export async function applyInventoryGrant(
  characterId: string,
  item: InventoryItemGrant
): Promise<CharacterProgressionSnapshot> {
  if (!item.itemId?.trim()) {
    throw new HttpError(400, 'itemId is required');
  }
  if (!Number.isFinite(item.quantity) || item.quantity <= 0) {
    throw new HttpError(400, 'quantity must be positive');
  }

  await initializeCharacterProgressionState(characterId);
  await grantInventoryItems(characterId, [
    { itemId: item.itemId.trim(), quantity: item.quantity, metadata: item.metadata },
  ]);

  const snapshot = await getCharacterProgressionSnapshot(characterId);
  broadcastProgression(characterId, snapshot);
  return snapshot;
}

export async function applyInventoryConsumption(
  characterId: string,
  item: InventoryItemConsumption
): Promise<CharacterProgressionSnapshot> {
  if (!item.itemId?.trim()) {
    throw new HttpError(400, 'itemId is required');
  }
  if (!Number.isFinite(item.quantity) || item.quantity <= 0) {
    throw new HttpError(400, 'quantity must be positive');
  }

  await initializeCharacterProgressionState(characterId);
  await consumeInventoryItems(characterId, [{ itemId: item.itemId.trim(), quantity: item.quantity }]);

  const snapshot = await getCharacterProgressionSnapshot(characterId);
  broadcastProgression(characterId, snapshot);
  return snapshot;
}

export async function applyQuestCompletion(
  characterId: string,
  quest: QuestCompletionInput
): Promise<CharacterProgressionSnapshot> {
  if (!quest.questId?.trim()) {
    throw new HttpError(400, 'questId is required');
  }

  await initializeCharacterProgressionState(characterId);
  const handler = resolveQuestCompletionHandler(quest.questId);
  await handler(characterId);
  if (quest.progress) {
    await upsertQuestState(characterId, {
      questId: quest.questId.trim(),
      status: 'completed',
      progress: quest.progress,
    });
  }

  const snapshot = await getCharacterProgressionSnapshot(characterId);
  broadcastProgression(characterId, snapshot);
  return snapshot;
}

export function registerProgressionSocketHandlers(server: WebSocketServer): void {
  server.on('connection', async (socket, request) => {
    try {
      const { userId, characterId } = await authenticateSocket(request);
      if (!characterId) {
        socket.close(1008, 'Missing characterId');
        return;
      }

      const character = await findCharacterById(characterId);
      if (!character || character.userId !== userId) {
        socket.close(1008, 'Forbidden');
        return;
      }

      await initializeCharacterProgressionState(characterId);
      addSubscriber(characterId, socket);
      sendSnapshot(socket, await getCharacterProgressionSnapshot(characterId));

      socket.on('message', (data) => {
        void handleSocketMessage(socket, userId, characterId, data);
      });

      socket.on('close', () => {
        removeSubscriber(characterId, socket);
      });
    } catch (error) {
      if (error instanceof HttpError) {
        socket.close(1008, error.message);
        return;
      }
      socket.close(1008, 'Unauthorized');
    }
  });
}

async function handleSocketMessage(
  socket: WebSocket,
  userId: string,
  defaultCharacterId: string,
  raw: RawData
): Promise<void> {
  let message: ProgressionSocketMessage;
  try {
    message = JSON.parse(raw.toString()) as ProgressionSocketMessage;
  } catch (_error) {
    sendError(socket, 'Invalid message payload');
    return;
  }

  if (!message || typeof message !== 'object') {
    sendError(socket, 'Unsupported message');
    return;
  }

  switch (message.type) {
    case 'update': {
      const characterId = message.payload?.characterId ?? defaultCharacterId;
      if (!characterId) {
        sendError(socket, 'CharacterId is required');
        return;
      }
      try {
        const intent = await submitProgressionIntentForUser(
          userId,
          characterId,
          message.payload ?? {}
        );
        sendIntentAck(socket, intent);
      } catch (error) {
        handleSocketError(socket, error);
      }
      break;
    }
    case 'subscribe': {
      const characterId = message.payload?.characterId ?? defaultCharacterId;
      if (!characterId) {
        sendError(socket, 'CharacterId is required');
        return;
      }

      try {
        const character = await findCharacterById(characterId);
        if (!character || character.userId !== userId) {
          throw new HttpError(403, 'You do not have access to this character');
        }
        addSubscriber(characterId, socket);
        sendSnapshot(socket, await getCharacterProgressionSnapshot(characterId));
      } catch (error) {
        handleSocketError(socket, error);
      }
      break;
    }
    default:
      sendError(socket, 'Unsupported message type');
  }
}

function handleSocketError(socket: WebSocket, error: unknown): void {
  if (error instanceof VersionConflictError) {
    sendError(socket, `${error.entity} update failed due to version mismatch`);
    return;
  }
  if (error instanceof ForbiddenClassUnlockError) {
    sendError(socket, error.message);
    return;
  }
  if (error instanceof HttpError) {
    sendError(socket, error.message);
    return;
  }
  sendError(socket, 'Unexpected error while processing update');
}

async function authenticateSocket(
  request: IncomingMessage
): Promise<{ userId: string; characterId?: string }> {
  const url = new URL(request.url ?? '/', 'http://localhost');
  const token = url.searchParams.get('token');
  if (!token) {
    throw new HttpError(401, 'Missing token');
  }

  let payload: AuthPayload;
  try {
    payload = jwt.verify(token, env.jwtSecret) as AuthPayload;
  } catch (_error) {
    throw new HttpError(401, 'Invalid token');
  }

  const user = await findUserById(payload.sub);
  if (!user) {
    throw new HttpError(401, 'Invalid token');
  }

  const characterId = url.searchParams.get('characterId') ?? undefined;
  return { userId: user.id, characterId };
}

function addSubscriber(characterId: string, socket: WebSocket): void {
  let sockets = subscribers.get(characterId);
  if (!sockets) {
    sockets = new Set();
    subscribers.set(characterId, sockets);
  }
  sockets.add(socket);
}

function removeSubscriber(characterId: string, socket: WebSocket): void {
  const sockets = subscribers.get(characterId);
  if (!sockets) {
    return;
  }
  sockets.delete(socket);
  if (sockets.size === 0) {
    subscribers.delete(characterId);
  }
}

function sendSnapshot(socket: WebSocket, snapshot: CharacterProgressionSnapshot): void {
  if (socket.readyState === WebSocket.OPEN) {
    socket.send(JSON.stringify({ type: 'progression', payload: snapshot }));
  }
}

function sendIntentAck(socket: WebSocket, intent: ProgressionIntentResponse): void {
  if (socket.readyState === WebSocket.OPEN) {
    socket.send(JSON.stringify({ type: 'progression-intent', payload: intent }));
  }
}

function sendError(socket: WebSocket, message: string): void {
  if (socket.readyState === WebSocket.OPEN) {
    socket.send(JSON.stringify({ type: 'error', message }));
  }
}

function broadcastProgression(
  characterId: string,
  snapshot: CharacterProgressionSnapshot
): void {
  const sockets = subscribers.get(characterId);
  if (!sockets || sockets.size === 0) {
    return;
  }

  const payload = JSON.stringify({ type: 'progression', payload: snapshot });
  for (const socket of sockets) {
    if (socket.readyState === WebSocket.OPEN) {
      socket.send(payload);
    }
  }
}

function hasProgressionIntent(input: ProgressionUpdateInput): boolean {
  return Boolean(
    input.progression ||
      input.classUnlocks ||
      input.inventory ||
      input.equipment ||
      input.quests
  );
}
