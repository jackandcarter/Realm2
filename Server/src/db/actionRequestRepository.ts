import { randomUUID } from 'crypto';
import { db } from './database';

export type ActionRequestStatus = 'pending' | 'processing' | 'completed' | 'rejected';

export interface ActionRequestRecord {
  id: string;
  characterId: string;
  realmId: string | null;
  requestedBy: string;
  requestType: string;
  payloadJson: string;
  status: ActionRequestStatus;
  errorMessage: string | null;
  createdAt: string;
  updatedAt: string;
  resolvedAt: string | null;
}

export interface ActionRequestInput {
  characterId: string;
  realmId?: string | null;
  requestedBy: string;
  requestType: string;
  payload: unknown;
}

function serializePayload(payload: unknown): string {
  try {
    return JSON.stringify(payload ?? {});
  } catch (_error) {
    return '{}';
  }
}

export async function createActionRequest(
  input: ActionRequestInput
): Promise<ActionRequestRecord> {
  const now = new Date().toISOString();
  const record: ActionRequestRecord = {
    id: randomUUID(),
    characterId: input.characterId,
    realmId: input.realmId ?? null,
    requestedBy: input.requestedBy,
    requestType: input.requestType,
    payloadJson: serializePayload(input.payload),
    status: 'pending',
    errorMessage: null,
    createdAt: now,
    updatedAt: now,
    resolvedAt: null,
  };

  await db.execute(
    `INSERT INTO character_action_requests (
        id,
        character_id,
        realm_id,
        requested_by,
        request_type,
        payload_json,
        status,
        error_message,
        created_at,
        updated_at,
        resolved_at
      )
      VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
    [
      record.id,
      record.characterId,
      record.realmId,
      record.requestedBy,
      record.requestType,
      record.payloadJson,
      record.status,
      record.errorMessage,
      record.createdAt,
      record.updatedAt,
      record.resolvedAt,
    ]
  );

  return record;
}
