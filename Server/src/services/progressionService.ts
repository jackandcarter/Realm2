import { IncomingMessage } from 'http';
import jwt from 'jsonwebtoken';
import { WebSocket, WebSocketServer } from 'ws';
import type { RawData } from 'ws';
import { env } from '../config/env';
import { findCharacterById } from '../db/characterRepository';
import {
  CharacterProgressionSnapshot,
  ClassUnlockInput,
  InventoryItemInput,
  QuestStateInput,
  VersionConflictError,
  getCharacterProgressionSnapshot,
  initializeCharacterProgressionState,
  replaceClassUnlocks,
  replaceInventory,
  replaceQuestStates,
  updateProgressionLevels,
} from '../db/progressionRepository';
import { findUserById } from '../db/userRepository';
import { AuthPayload } from '../middleware/authMiddleware';
import { HttpError } from '../utils/errors';

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
  quests?: {
    expectedVersion: number;
    quests: QuestStateInput[];
  };
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

export function getCharacterProgressionForUser(
  userId: string,
  characterId: string
): CharacterProgressionSnapshot {
  const character = findCharacterById(characterId);
  if (!character) {
    throw new HttpError(404, 'Character not found');
  }

  if (character.userId !== userId) {
    throw new HttpError(403, 'You do not have access to this character');
  }

  initializeCharacterProgressionState(characterId);
  return getCharacterProgressionSnapshot(characterId);
}

export function updateCharacterProgressionForUser(
  userId: string,
  characterId: string,
  input: ProgressionUpdateInput
): CharacterProgressionSnapshot {
  const character = findCharacterById(characterId);
  if (!character) {
    throw new HttpError(404, 'Character not found');
  }

  if (character.userId !== userId) {
    throw new HttpError(403, 'You do not have access to this character');
  }

  initializeCharacterProgressionState(characterId);

  if (input.progression) {
    const { level, xp, expectedVersion } = input.progression;
    updateProgressionLevels(characterId, level, xp, expectedVersion);
  }

  if (input.classUnlocks) {
    replaceClassUnlocks(characterId, input.classUnlocks.unlocks, input.classUnlocks.expectedVersion);
  }

  if (input.inventory) {
    replaceInventory(characterId, input.inventory.items, input.inventory.expectedVersion);
  }

  if (input.quests) {
    replaceQuestStates(characterId, input.quests.quests, input.quests.expectedVersion);
  }

  const snapshot = getCharacterProgressionSnapshot(characterId);
  broadcastProgression(characterId, snapshot);
  return snapshot;
}

export function registerProgressionSocketHandlers(server: WebSocketServer): void {
  server.on('connection', (socket, request) => {
    try {
      const { userId, characterId } = authenticateSocket(request);
      if (!characterId) {
        socket.close(1008, 'Missing characterId');
        return;
      }

      const character = findCharacterById(characterId);
      if (!character || character.userId !== userId) {
        socket.close(1008, 'Forbidden');
        return;
      }

      initializeCharacterProgressionState(characterId);
      addSubscriber(characterId, socket);
      sendSnapshot(socket, getCharacterProgressionSnapshot(characterId));

      socket.on('message', (data) => {
        handleSocketMessage(socket, userId, characterId, data);
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

function handleSocketMessage(
  socket: WebSocket,
  userId: string,
  defaultCharacterId: string,
  raw: RawData
): void {
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
        const snapshot = updateCharacterProgressionForUser(userId, characterId, message.payload ?? {});
        sendSnapshot(socket, snapshot);
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
        const character = findCharacterById(characterId);
        if (!character || character.userId !== userId) {
          throw new HttpError(403, 'You do not have access to this character');
        }
        addSubscriber(characterId, socket);
        sendSnapshot(socket, getCharacterProgressionSnapshot(characterId));
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
  if (error instanceof HttpError) {
    sendError(socket, error.message);
    return;
  }
  sendError(socket, 'Unexpected error while processing update');
}

function authenticateSocket(request: IncomingMessage): { userId: string; characterId?: string } {
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

  const user = findUserById(payload.sub);
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
