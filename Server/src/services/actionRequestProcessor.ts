import { logger } from '../observability/logger';
import {
  listPendingActionRequests,
  markActionRequestProcessing,
  resolveActionRequest,
} from '../db/actionRequestRepository';
import { applyProgressionUpdate, ProgressionUpdateInput } from './progressionService';
import { HttpError } from '../utils/errors';

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
