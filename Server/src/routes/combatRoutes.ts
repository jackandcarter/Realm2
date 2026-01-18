import { Router } from 'express';
import { requireAuth } from '../middleware/authMiddleware';
import { HttpError } from '../utils/errors';
import { executeCombatAbility } from '../services/combatService';
import {
  CombatAbilityExecutionRequestDto,
  CombatParticipantSnapshotDto,
} from '../types/combatApi';

export const combatRouter = Router();

combatRouter.post('/execute', requireAuth, async (req, res, next) => {
  try {
    const payload = toCombatAbilityExecutionRequest(req.body);
    const confirmation = await executeCombatAbility(payload, { userId: req.user!.id });
    res.json(confirmation);
  } catch (error) {
    next(error);
  }
});

function toCombatAbilityExecutionRequest(body: unknown): CombatAbilityExecutionRequestDto {
  if (!body || typeof body !== 'object') {
    throw new HttpError(400, 'Request body is required');
  }

  const value = body as Record<string, unknown>;
  const requestId = typeof value.requestId === 'string' ? value.requestId.trim() : '';
  const abilityId = typeof value.abilityId === 'string' ? value.abilityId.trim() : '';
  const casterId = typeof value.casterId === 'string' ? value.casterId.trim() : '';

  if (!requestId) {
    throw new HttpError(400, 'requestId is required');
  }

  if (!abilityId) {
    throw new HttpError(400, 'abilityId is required');
  }

  if (!casterId) {
    throw new HttpError(400, 'casterId is required');
  }

  const participantsRaw = value.participants;
  if (!Array.isArray(participantsRaw)) {
    throw new HttpError(400, 'participants must be an array');
  }

  const participants = participantsRaw.map((entry, index) => toParticipantSnapshot(entry, index));

  return {
    requestId,
    abilityId,
    casterId,
    primaryTargetId:
      typeof value.primaryTargetId === 'string' ? value.primaryTargetId.trim() : undefined,
    targetIds: Array.isArray(value.targetIds)
      ? value.targetIds.filter((id): id is string => typeof id === 'string' && id.trim() !== '')
      : undefined,
    targetPoint: parseVector3(value.targetPoint),
    clientTime: ensureRequiredNumber(value.clientTime, 'clientTime'),
    baseDamage: ensureNumber(value.baseDamage),
    participants,
  };
}

function toParticipantSnapshot(
  entry: unknown,
  index: number,
): CombatParticipantSnapshotDto {
  if (!entry || typeof entry !== 'object') {
    throw new HttpError(400, `participants[${index}] must be an object`);
  }

  const record = entry as Record<string, unknown>;
  const id = typeof record.id === 'string' ? record.id.trim() : '';
  if (!id) {
    throw new HttpError(400, `participants[${index}].id is required`);
  }

  const team = typeof record.team === 'string' && record.team.trim() !== ''
    ? record.team.trim()
    : 'neutral';

  const health = ensureRequiredNumber(record.health, `participants[${index}].health`);
  const maxHealth = ensureRequiredNumber(record.maxHealth, `participants[${index}].maxHealth`);

  const stats = Array.isArray(record.stats)
    ? record.stats
        .filter((stat): stat is Record<string, unknown> => Boolean(stat) && typeof stat === 'object')
        .map((stat) => ({
          id: typeof stat.id === 'string' ? stat.id.trim() : '',
          value: ensureNumber(stat.value),
        }))
        .filter((stat) => stat.id && Number.isFinite(stat.value))
    : undefined;

  const states = Array.isArray(record.states)
    ? record.states
        .filter((state): state is Record<string, unknown> => Boolean(state) && typeof state === 'object')
        .map((state) => ({
          id: typeof state.id === 'string' ? state.id.trim() : '',
          durationSeconds: ensureNumber(state.durationSeconds),
        }))
        .filter((state) => state.id)
    : undefined;

  return {
    id,
    team,
    health,
    maxHealth,
    stats,
    states,
  };
}

function parseVector3(value: unknown): CombatAbilityExecutionRequestDto['targetPoint'] {
  if (!value || typeof value !== 'object') {
    return undefined;
  }

  const record = value as Record<string, unknown>;
  const x = ensureNumber(record.x);
  const y = ensureNumber(record.y);
  const z = ensureNumber(record.z);

  if (!Number.isFinite(x) || !Number.isFinite(y) || !Number.isFinite(z)) {
    return undefined;
  }

  return { x, y, z };
}

function ensureNumber(value: unknown, fieldName?: string): number | undefined {
  if (value === undefined || value === null) {
    return undefined;
  }

  const num = typeof value === 'number' ? value : Number(value);
  if (!Number.isFinite(num)) {
    if (fieldName) {
      throw new HttpError(400, `${fieldName} must be a number`);
    }
    return undefined;
  }

  return num;
}

function ensureRequiredNumber(value: unknown, fieldName: string): number {
  const num = ensureNumber(value, fieldName);
  if (!Number.isFinite(num)) {
    throw new HttpError(400, `${fieldName} must be a number`);
  }

  return num;
}
