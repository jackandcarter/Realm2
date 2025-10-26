import { IncomingMessage } from 'http';
import jwt from 'jsonwebtoken';
import { WebSocket, WebSocketServer } from 'ws';
import { env } from '../config/env';
import { findUserById } from '../db/userRepository';
import { AuthPayload } from '../middleware/authMiddleware';
import {
  ChunkUpdateInput,
  PlotUpdateInput,
  StructureUpdateInput,
  assertChunkAccess,
  recordChunkChange,
} from './chunkService';
import { addChunkListener, removeChunkListener } from './chunkStreamService';
import { ChunkChangeDTO } from '../types/chunk';
import { HttpError } from '../utils/errors';

interface SocketContext {
  userId: string;
  socket: WebSocket;
  subscriptions: Map<string, (change: ChunkChangeDTO) => void>;
}

interface SubscribeMessage {
  type: 'subscribe';
  realmId?: string;
}

interface UnsubscribeMessage {
  type: 'unsubscribe';
  realmId?: string;
}

interface MutationMessage {
  type: 'mutation';
  realmId?: string;
  chunkId?: string;
  requestId?: string;
  changeType?: string;
  chunk?: ChunkUpdateInput;
  structures?: StructureUpdateInput[];
  plots?: PlotUpdateInput[];
}

interface PingMessage {
  type: 'ping';
}

interface BaseServerMessage {
  type: string;
  realmId?: string;
  requestId?: string;
}

interface ChangeServerMessage extends BaseServerMessage {
  type: 'change';
  change: ChunkChangeDTO;
}

interface MutationAckServerMessage extends BaseServerMessage {
  type: 'mutationAck';
  change: ChunkChangeDTO;
}

interface MutationRejectedServerMessage extends BaseServerMessage {
  type: 'mutationRejected';
  error: string;
}

interface SimpleServerMessage extends BaseServerMessage {
  type: 'ready' | 'subscribed' | 'unsubscribed';
}

interface ErrorServerMessage extends BaseServerMessage {
  type: 'error';
  error: string;
}

type ClientMessage = SubscribeMessage | UnsubscribeMessage | MutationMessage | PingMessage;
type ServerMessage =
  | ChangeServerMessage
  | MutationAckServerMessage
  | MutationRejectedServerMessage
  | SimpleServerMessage
  | ErrorServerMessage;

export function registerChunkSocketHandlers(server: WebSocketServer): void {
  server.on('connection', (socket, request) => {
    let context: SocketContext | undefined;
    try {
      const userId = authenticateSocket(request);
      context = { userId, socket, subscriptions: new Map() };
      send(socket, { type: 'ready' });
      socket.on('message', (raw) => handleMessage(context!, raw));
      socket.on('close', () => {
        cleanupContext(context!);
      });
      socket.on('error', () => {
        cleanupContext(context!);
      });
    } catch (error) {
      const reason = error instanceof HttpError ? error.message : 'Unauthorized';
      socket.close(1008, reason);
    }
  });
}

function handleMessage(context: SocketContext, raw: WebSocket.RawData): void {
  let message: ClientMessage;
  try {
    const text = typeof raw === 'string' ? raw : raw.toString();
    message = JSON.parse(text) as ClientMessage;
  } catch (_error) {
    sendError(context.socket, 'Invalid message payload');
    return;
  }

  if (!message || typeof message !== 'object' || typeof (message as { type?: string }).type !== 'string') {
    sendError(context.socket, 'Unsupported message format');
    return;
  }

  switch (message.type) {
    case 'subscribe':
      handleSubscribe(context, message);
      break;
    case 'unsubscribe':
      handleUnsubscribe(context, message);
      break;
    case 'mutation':
      handleMutation(context, message);
      break;
    case 'ping':
      send(context.socket, { type: 'pong' });
      break;
    default:
      sendError(context.socket, `Unsupported message type: ${(message as { type: string }).type}`);
  }
}

function handleSubscribe(context: SocketContext, message: SubscribeMessage): void {
  const realmId = (message.realmId ?? '').trim();
  if (!realmId) {
    sendError(context.socket, 'realmId is required to subscribe');
    return;
  }

  try {
    assertChunkAccess(context.userId, realmId);
  } catch (error) {
    const reason = error instanceof HttpError ? error.message : 'Failed to subscribe to realm updates';
    send(context.socket, { type: 'error', realmId, error: reason });
    return;
  }

  const existingListener = context.subscriptions.get(realmId);
  if (existingListener) {
    removeChunkListener(realmId, existingListener);
  }

  const listener = (change: ChunkChangeDTO) => {
    send(context.socket, { type: 'change', realmId, change });
  };
  context.subscriptions.set(realmId, listener);
  addChunkListener(realmId, listener);
  send(context.socket, { type: 'subscribed', realmId });
}

function handleUnsubscribe(context: SocketContext, message: UnsubscribeMessage): void {
  const realmId = (message.realmId ?? '').trim();
  if (!realmId) {
    sendError(context.socket, 'realmId is required to unsubscribe');
    return;
  }

  const listener = context.subscriptions.get(realmId);
  if (!listener) {
    send(context.socket, { type: 'unsubscribed', realmId });
    return;
  }

  removeChunkListener(realmId, listener);
  context.subscriptions.delete(realmId);
  send(context.socket, { type: 'unsubscribed', realmId });
}

function handleMutation(context: SocketContext, message: MutationMessage): void {
  const realmId = (message.realmId ?? '').trim();
  const chunkId = (message.chunkId ?? '').trim();
  const requestId = message.requestId ?? undefined;

  if (!realmId || !chunkId) {
    sendMutationRejection(context.socket, realmId || undefined, requestId, 'realmId and chunkId are required');
    return;
  }

  try {
    const change = recordChunkChange(
      context.userId,
      realmId,
      chunkId,
      message.changeType,
      message.chunk,
      message.structures,
      message.plots
    );
    send(context.socket, { type: 'mutationAck', realmId, requestId, change });
  } catch (error) {
    const reason = error instanceof HttpError ? error.message : 'Failed to process mutation';
    sendMutationRejection(context.socket, realmId, requestId, reason);
  }
}

function cleanupContext(context: SocketContext): void {
  for (const [realmId, listener] of context.subscriptions.entries()) {
    removeChunkListener(realmId, listener);
  }
  context.subscriptions.clear();
}

function authenticateSocket(request: IncomingMessage): string {
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

  return user.id;
}

function send(socket: WebSocket, message: ServerMessage | { type: 'pong' }): void {
  if (socket.readyState === WebSocket.OPEN) {
    socket.send(JSON.stringify(message));
  }
}

function sendError(socket: WebSocket, error: string): void {
  send(socket, { type: 'error', error });
}

function sendMutationRejection(
  socket: WebSocket,
  realmId: string | undefined,
  requestId: string | undefined,
  error: string
): void {
  send(socket, { type: 'mutationRejected', realmId, requestId, error });
}
