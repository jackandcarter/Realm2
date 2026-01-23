import { randomUUID } from 'crypto';
import { db } from './database';
import { ActionRequestType } from '../config/gameEnums';

export type ActionRequestStatus = 'pending' | 'processing' | 'completed' | 'rejected';

export interface ActionRequestRecord {
  id: string;
  characterId: string;
  realmId: string | null;
  requestedBy: string;
  requestType: ActionRequestType;
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
  requestType: ActionRequestType;
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

export async function listPendingActionRequests(limit = 25): Promise<ActionRequestRecord[]> {
  return db.query<ActionRequestRecord[]>(
    `SELECT
       id,
       character_id as characterId,
       realm_id as realmId,
       requested_by as requestedBy,
       request_type as requestType,
       payload_json as payloadJson,
       status,
       error_message as errorMessage,
       created_at as createdAt,
       updated_at as updatedAt,
       resolved_at as resolvedAt
     FROM character_action_requests
     WHERE status = 'pending'
     ORDER BY created_at ASC
     LIMIT ?`,
    [limit]
  );
}

export async function markActionRequestProcessing(id: string): Promise<boolean> {
  const now = new Date().toISOString();
  const result = await db.execute(
    `UPDATE character_action_requests
     SET status = 'processing',
         updated_at = ?
     WHERE id = ? AND status = 'pending'`,
    [now, id]
  );
  return (result.affectedRows ?? 0) > 0;
}

export async function resolveActionRequest(
  id: string,
  status: Exclude<ActionRequestStatus, 'processing' | 'pending'>,
  errorMessage?: string | null
): Promise<void> {
  const now = new Date().toISOString();
  await db.execute(
    `UPDATE character_action_requests
     SET status = ?,
         error_message = ?,
         updated_at = ?,
         resolved_at = ?
     WHERE id = ?`,
    [status, errorMessage ?? null, now, now, id]
  );
}
