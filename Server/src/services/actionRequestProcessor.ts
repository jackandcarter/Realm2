import { logger } from '../observability/logger';
import {
  listPendingActionRequests,
  markActionRequestProcessing,
  resolveActionRequest,
} from '../db/actionRequestRepository';
import {
  applyInventoryConsumption,
  applyInventoryGrant,
  applyProgressionUpdate,
  applyProgressionXpGrant,
  applyQuestCompletion,
  ProgressionUpdateInput,
} from './progressionService';
import { HttpError } from '../utils/errors';
import { JsonValue, isJsonValue } from '../types/characterCustomization';

const PROCESSOR_POLL_MS = 2000;
const PROCESSOR_BATCH_SIZE = 25;

let processorTimer: NodeJS.Timeout | undefined;

export function startActionRequestProcessor(): void {
  if (processorTimer) {
    return;
  }
  processorTimer = setInterval(() => {
    void processActionRequests();
  }, PROCESSOR_POLL_MS);
}

export async function processActionRequests(): Promise<void> {
  const pending = await listPendingActionRequests(PROCESSOR_BATCH_SIZE);
  for (const request of pending) {
    const claimed = await markActionRequestProcessing(request.id);
    if (!claimed) {
      continue;
    }
    try {
      await handleActionRequest(request.requestType, request.characterId, request.payloadJson);
      await resolveActionRequest(request.id, 'completed');
    } catch (error) {
      const message =
        error instanceof HttpError
          ? error.message
          : error instanceof Error
          ? error.message
          : 'Unknown error';
      await resolveActionRequest(request.id, 'rejected', message);
      logger.warn({ err: error, requestId: request.id }, 'Action request failed');
    }
  }
}

async function handleActionRequest(
  requestType: string,
  characterId: string,
  payloadJson: string
): Promise<void> {
  switch (requestType) {
    case 'progression.update': {
      const payload = parsePayload<ProgressionUpdateInput>(payloadJson);
      await applyProgressionUpdate(characterId, payload);
      return;
    }
    case 'progression.grantXp': {
      const payload = parsePayload<{ amount?: unknown }>(payloadJson);
      const amount = ensurePositiveNumber(payload.amount, 'amount');
      await applyProgressionXpGrant(characterId, amount);
      return;
    }
    case 'inventory.grantItem': {
      const payload = parsePayload<{ itemId?: unknown; quantity?: unknown; metadata?: unknown }>(
        payloadJson
      );
      const itemId = ensureNonEmptyString(payload.itemId, 'itemId');
      const quantity = ensurePositiveNumber(payload.quantity, 'quantity');
      const metadata = normalizeJsonValue(payload.metadata, 'metadata');
      await applyInventoryGrant(characterId, { itemId, quantity, metadata });
      return;
    }
    case 'inventory.consumeItem': {
      const payload = parsePayload<{ itemId?: unknown; quantity?: unknown }>(payloadJson);
      const itemId = ensureNonEmptyString(payload.itemId, 'itemId');
      const quantity = ensurePositiveNumber(payload.quantity, 'quantity');
      await applyInventoryConsumption(characterId, { itemId, quantity });
      return;
    }
    case 'quest.complete': {
      const payload = parsePayload<{ questId?: unknown; progress?: unknown }>(payloadJson);
      const questId = ensureNonEmptyString(payload.questId, 'questId');
      const progress = normalizeJsonValue(payload.progress, 'progress');
      await applyQuestCompletion(characterId, { questId, progress });
      return;
    }
    default:
      throw new HttpError(400, `Unsupported action request type '${requestType}'.`);
  }
}

function parsePayload<T>(payloadJson: string): T {
  try {
    return JSON.parse(payloadJson ?? '{}') as T;
  } catch (_error) {
    return {} as T;
  }
}

function ensureNonEmptyString(value: unknown, fieldName: string): string {
  if (typeof value !== 'string' || value.trim().length === 0) {
    throw new HttpError(400, `${fieldName} must be a non-empty string.`);
  }
  return value.trim();
}

function ensurePositiveNumber(value: unknown, fieldName: string): number {
  const numberValue = typeof value === 'number' ? value : Number(value);
  if (!Number.isFinite(numberValue) || numberValue <= 0) {
    throw new HttpError(400, `${fieldName} must be a positive number.`);
  }
  return numberValue;
}

function normalizeJsonValue(value: unknown, fieldName: string): JsonValue | undefined {
  if (typeof value === 'undefined') {
    return undefined;
  }
  if (!isJsonValue(value)) {
    throw new HttpError(400, `${fieldName} must be JSON-serializable.`);
  }
  return value;
}
