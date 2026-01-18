import { AbilityExecutor, AbilityRegistry, CombatStatRegistry } from '../gameplay/combat';
import {
  CombatAbilityExecutionRequestDto,
  CombatAbilityExecutionResponseDto,
  CombatAbilityEventDto,
  CombatParticipantSnapshotDto,
} from '../types/combatApi';
import { generatedAbilityDefinitions } from '../gameplay/combat/generated/abilityRegistry';
import { generatedStatDefinitions } from '../gameplay/combat/generated/statRegistry';
import { CombatEntitySnapshot, CombatStateInstance } from '../gameplay/combat/types';
import { HttpError } from '../utils/errors';
import { findCharacterById } from '../db/characterRepository';

const statRegistry = new CombatStatRegistry(generatedStatDefinitions);
const abilityRegistry = new AbilityRegistry(generatedAbilityDefinitions);
const executor = new AbilityExecutor({ stats: statRegistry, abilities: abilityRegistry });
const lastClientTimes = new Map<string, number>();

interface CombatExecutionContext {
  userId: string;
}

export async function executeCombatAbility(
  request: CombatAbilityExecutionRequestDto,
  context: CombatExecutionContext,
): Promise<CombatAbilityExecutionResponseDto> {
  const casterId = request.casterId.trim();
  const abilityId = request.abilityId.trim();
  await validateCombatRequest(request, context);
  const participants = request.participants.map((participant) => toCombatEntitySnapshot(participant));
  const hasCaster = participants.some((participant) => participant.id === casterId);
  if (!hasCaster) {
    throw new HttpError(400, `Caster '${casterId}' was not included in participants.`);
  }

  const abilityDefinition = abilityRegistry.get(abilityId);
  const result = abilityDefinition
    ? executor.execute(abilityId, {
        casterId,
        participants,
        primaryTargetId: request.primaryTargetId,
      })
    : executeFallbackAbility(participants, request);

  const targetIds = Array.from(
    new Set((result.events ?? []).map((event) => event.targetId).filter(Boolean)),
  );

  return {
    requestId: request.requestId,
    abilityId,
    casterId,
    clientTime: request.clientTime,
    targetIds,
    serverTime: Date.now() / 1000,
    events: result.events as CombatAbilityEventDto[],
  };
}

async function validateCombatRequest(
  request: CombatAbilityExecutionRequestDto,
  context: CombatExecutionContext,
): Promise<void> {
  const casterId = request.casterId.trim();
  const casterCharacter = await findCharacterById(casterId);
  if (!casterCharacter) {
    throw new HttpError(404, `Caster '${casterId}' does not exist.`);
  }

  if (casterCharacter.userId !== context.userId) {
    throw new HttpError(403, 'You do not have access to this caster.');
  }

  const realmId = casterCharacter.realmId;
  for (const participant of request.participants) {
    const participantCharacter = await findCharacterById(participant.id);
    if (participantCharacter && participantCharacter.realmId !== realmId) {
      throw new HttpError(400, `Participant '${participant.id}' is not in the same realm.`);
    }
  }

  const key = `${context.userId}:${casterId}`;
  const lastClientTime = lastClientTimes.get(key);
  if (lastClientTime !== undefined && request.clientTime <= lastClientTime) {
    throw new HttpError(409, 'Out-of-order combat request detected.');
  }
  lastClientTimes.set(key, request.clientTime);
}

function toCombatEntitySnapshot(participant: CombatParticipantSnapshotDto): CombatEntitySnapshot {
  const statsEntries = participant.stats ?? [];
  const stats = statsEntries.reduce<Record<string, number>>((acc, entry) => {
    if (entry?.id && Number.isFinite(entry.value)) {
      acc[entry.id] = entry.value;
    }
    return acc;
  }, {});

  const states = (participant.states ?? [])
    .filter((state) => Boolean(state?.id))
    .map((state) => ({
      id: state.id,
      durationSeconds: Number.isFinite(state.durationSeconds) ? state.durationSeconds : undefined,
    }));

  return {
    id: participant.id,
    team: participant.team,
    health: participant.health,
    maxHealth: participant.maxHealth,
    stats,
    states: states.length > 0 ? (states as CombatStateInstance[]) : undefined,
  };
}

function executeFallbackAbility(
  participants: CombatEntitySnapshot[],
  request: CombatAbilityExecutionRequestDto,
): { abilityId: string; events: CombatAbilityEventDto[]; participants: CombatEntitySnapshot[] } {
  const baseDamage = Number.isFinite(request.baseDamage) ? Math.max(0, request.baseDamage ?? 0) : 0;
  const targetIds = (request.targetIds ?? []).filter(Boolean);

  const events: CombatAbilityEventDto[] = [];

  targetIds.forEach((targetId) => {
    const target = participants.find((participant) => participant.id === targetId);
    if (!target) {
      return;
    }

    if (baseDamage > 0) {
      target.health = Math.max(0, target.health - baseDamage);
      events.push({ kind: 'damage', targetId: target.id, amount: baseDamage });
    }
  });

  return {
    abilityId: request.abilityId,
    events,
    participants,
  };
}
