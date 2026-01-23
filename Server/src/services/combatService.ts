import { AbilityExecutor, AbilityRegistry, CombatStatRegistry } from '../gameplay/combat';
import {
  CombatAbilityExecutionRequestDto,
  CombatAbilityExecutionResponseDto,
  CombatAbilityEventDto,
} from '../types/combatApi';
import { generatedAbilityDefinitions } from '../gameplay/combat/generated/abilityRegistry';
import { generatedStatDefinitions } from '../gameplay/combat/generated/statRegistry';
import { CombatEntitySnapshot } from '../gameplay/combat/types';
import { HttpError } from '../utils/errors';
import { findCharacterById } from '../db/characterRepository';
import {
  getAbilityCooldown,
  getCharacterResourceState,
  getCombatClientTime,
  recordCombatEvents,
  upsertAbilityCooldown,
  upsertCharacterResourceState,
  upsertCombatClientTime,
} from '../db/combatRepository';
import { getCharacterProgressionSnapshot } from '../db/progressionRepository';
import {
  AbilityRecord,
  getClassById,
  getAbilityById,
  getClassBaseStatsById,
  getItemsByIds,
  listLevelProgression,
} from '../db/catalogRepository';

const statRegistry = new CombatStatRegistry(generatedStatDefinitions);
const abilityRegistry = new AbilityRegistry(generatedAbilityDefinitions);
const executor = new AbilityExecutor({ stats: statRegistry, abilities: abilityRegistry });
interface ResourceSnapshot {
  resourceType: string;
  currentValue: number;
  maxValue: number;
}

interface CombatExecutionContext {
  userId: string;
}

export async function executeCombatAbility(
  request: CombatAbilityExecutionRequestDto,
  context: CombatExecutionContext,
): Promise<CombatAbilityExecutionResponseDto> {
  const casterId = request.casterId.trim();
  const abilityId = request.abilityId.trim();
  const { casterCharacter, abilityRecord, resourceSnapshot } = await validateCombatRequest(
    request,
    context,
  );
  const participants = await buildCombatParticipants(request, casterCharacter);

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

  if (abilityRecord) {
    await applyAbilityCooldown(casterId, abilityRecord);
    await applyResourceCost(casterId, abilityRecord, resourceSnapshot);
  }

  await recordCombatEvents(
    request.requestId,
    abilityId,
    casterId,
    result.events as CombatAbilityEventDto[],
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

type CharacterRecord = NonNullable<Awaited<ReturnType<typeof findCharacterById>>>;

async function validateCombatRequest(
  request: CombatAbilityExecutionRequestDto,
  context: CombatExecutionContext,
): Promise<{
  casterCharacter: CharacterRecord;
  abilityRecord?: AbilityRecord;
  resourceSnapshot?: ResourceSnapshot;
}> {
  const casterId = request.casterId.trim();
  const casterCharacter = await findCharacterById(casterId);
  if (!casterCharacter) {
    throw new HttpError(404, `Caster '${casterId}' does not exist.`);
  }

  if (casterCharacter.userId !== context.userId) {
    throw new HttpError(403, 'You do not have access to this caster.');
  }

  await ensureClientTimeIsMonotonic(context.userId, casterId, request.clientTime);

  const abilityRecord = await getAbilityById(request.abilityId);
  let resourceSnapshot: ResourceSnapshot | undefined;
  if (abilityRecord) {
    await ensureAbilityOffCooldown(casterId, abilityRecord);
    const progression = await getCharacterProgressionSnapshot(casterId);
    resourceSnapshot = await validateAbilityResourceCost(
      abilityRecord,
      casterCharacter,
      progression.progression.level,
      casterId,
    );
    validateAbilityRange(request, abilityRecord);
  }

  const abilityDefinition = abilityRegistry.get(request.abilityId);
  if (abilityDefinition) {
    const needsPrimaryTarget = abilityDefinition.graph.nodes.some(
      (node) =>
        node.kind === 'selectTargets' &&
        (node.selector === 'primaryEnemy' || node.selector === 'primaryAlly')
    );
    if (needsPrimaryTarget && !request.primaryTargetId?.trim()) {
      throw new HttpError(400, 'primaryTargetId is required for this ability.');
    }
  }

  return { casterCharacter: casterCharacter as CharacterRecord, abilityRecord, resourceSnapshot };
}

async function ensureClientTimeIsMonotonic(
  userId: string,
  casterId: string,
  clientTime: number,
): Promise<void> {
  const last = await getCombatClientTime(userId, casterId);
  if (last && clientTime <= last.lastClientTime) {
    throw new HttpError(409, 'Out-of-order combat request detected.');
  }
  await upsertCombatClientTime(userId, casterId, clientTime);
}

async function buildCombatParticipants(
  request: CombatAbilityExecutionRequestDto,
  casterCharacter: { id: string; realmId: string; userId: string; classId: string | null },
): Promise<CombatEntitySnapshot[]> {
  const participantIds = new Set<string>([request.casterId]);
  if (request.primaryTargetId) {
    participantIds.add(request.primaryTargetId);
  }
  (request.targetIds ?? []).forEach((id) => participantIds.add(id));

  const participants: CombatEntitySnapshot[] = [];
  for (const participantId of participantIds) {
    const participantCharacter = await findCharacterById(participantId);
    if (!participantCharacter) {
      throw new HttpError(404, `Participant '${participantId}' does not exist.`);
    }
    if (participantCharacter.realmId !== casterCharacter.realmId) {
      throw new HttpError(400, `Participant '${participantId}' is not in the same realm.`);
    }
    participants.push(
      await buildCombatEntitySnapshot(participantCharacter, casterCharacter.userId),
    );
  }

  return participants;
}

async function buildCombatEntitySnapshot(
  character: { id: string; userId: string; classId: string | null },
  casterUserId: string,
): Promise<CombatEntitySnapshot> {
  const progression = await getCharacterProgressionSnapshot(character.id);
  const equipment = progression.equipment.items.filter((item) =>
    character.classId ? item.classId === character.classId : true,
  );
  const equipmentItems = await getItemsByIds(equipment.map((item) => item.itemId));
  const stats = await buildCombatStats(character.classId, equipmentItems);
  const maxHealth = await resolveMaxHealth(character.classId, progression.progression.level, stats);

  return {
    id: character.id,
    team: character.userId === casterUserId ? 'ally' : 'enemy',
    health: maxHealth,
    maxHealth,
    stats,
  };
}

async function buildCombatStats(
  classId: string | null,
  equipmentItems: { metadataJson: string }[],
): Promise<Record<string, number>> {
  const stats: Record<string, number> = {};
  const classBaseStats = classId ? await getClassBaseStatsById(classId) : undefined;
  if (classBaseStats) {
    stats['stat.strength'] = classBaseStats.strength;
    stats['stat.agility'] = classBaseStats.agility;
    stats['stat.vitality'] = classBaseStats.vitality;
    stats['stat.defense'] = classBaseStats.defense;
    stats['stat.magic'] = classBaseStats.intelligence;
  }

  for (const item of equipmentItems) {
    const metadata = safeParseMetadata(item.metadataJson);
    const baseStatsFromItem = metadata?.baseStats;
    if (baseStatsFromItem && typeof baseStatsFromItem === 'object') {
      for (const [statId, value] of Object.entries(baseStatsFromItem)) {
        const numeric = typeof value === 'number' ? value : Number(value);
        if (Number.isFinite(numeric)) {
          stats[statId] = (stats[statId] ?? 0) + numeric;
        }
      }
    }
  }

  return stats;
}

async function resolveMaxHealth(
  classId: string | null,
  level: number,
  stats: Record<string, number>,
): Promise<number> {
  const classBaseStats = classId ? await getClassBaseStatsById(classId) : undefined;
  const baseHealth = classBaseStats?.baseHealth ?? 0;
  const vitality = stats['stat.vitality'] ?? 0;
  const levelProgression = await listLevelProgression();
  const levelBonus = levelProgression
    .filter((entry) => entry.level > 1 && entry.level <= level)
    .reduce((sum, entry) => sum + (entry.hpGain ?? 0), 0);
  const derived = baseHealth + vitality * 10 + levelBonus;
  return Math.max(1, derived > 0 ? derived : 100);
}

async function ensureAbilityOffCooldown(casterId: string, ability: AbilityRecord): Promise<void> {
  if (!ability.cooldownSeconds || ability.cooldownSeconds <= 0) {
    return;
  }
  const lastUsed = await getAbilityCooldown(casterId, ability.id);
  const now = Date.now() / 1000;
  if (lastUsed && now - lastUsed.lastUsedAt < ability.cooldownSeconds) {
    throw new HttpError(409, 'Ability is still on cooldown.');
  }
}

async function validateAbilityResourceCost(
  ability: AbilityRecord,
  caster: { classId: string | null },
  level: number,
  casterId: string,
): Promise<ResourceSnapshot | undefined> {
  if (!ability.resourceCost || ability.resourceCost <= 0) {
    return undefined;
  }
  const classBaseStats = caster.classId ? await getClassBaseStatsById(caster.classId) : undefined;
  const classRecord = caster.classId ? await getClassById(caster.classId) : undefined;
  const levelProgression = await listLevelProgression();
  const manaBonus = levelProgression
    .filter((entry) => entry.level > 1 && entry.level <= level)
    .reduce((sum, entry) => sum + (entry.manaGain ?? 0), 0);
  const maxResource = (classBaseStats?.baseMana ?? 0) + manaBonus || 100;
  const resourceType = classRecord?.resourceType?.trim() || 'mana';
  const resourceState = await getCharacterResourceState(casterId, resourceType);
  const currentValue = resourceState ? resourceState.currentValue : maxResource;
  const clampedCurrent = Math.min(currentValue, maxResource);
  if (ability.resourceCost > clampedCurrent) {
    throw new HttpError(400, 'Insufficient resources to use this ability.');
  }
  return { resourceType, currentValue: clampedCurrent, maxValue: maxResource };
}

function validateAbilityRange(
  request: CombatAbilityExecutionRequestDto,
  ability: AbilityRecord,
): void {
  if (!ability.rangeMeters || ability.rangeMeters <= 0) {
    return;
  }
  if (!request.targetPoint) {
    return;
  }
  const { x, y, z } = request.targetPoint;
  const distance = Math.sqrt(x * x + y * y + z * z);
  if (distance > ability.rangeMeters) {
    throw new HttpError(400, 'Target is out of range.');
  }
}

async function applyAbilityCooldown(casterId: string, ability: AbilityRecord): Promise<void> {
  if (!ability.cooldownSeconds || ability.cooldownSeconds <= 0) {
    return;
  }
  const now = Date.now() / 1000;
  await upsertAbilityCooldown(casterId, ability.id, now);
}

async function applyResourceCost(
  casterId: string,
  ability: AbilityRecord,
  resourceSnapshot?: ResourceSnapshot,
): Promise<void> {
  if (!resourceSnapshot || !ability.resourceCost || ability.resourceCost <= 0) {
    return;
  }
  const remaining = Math.max(0, resourceSnapshot.currentValue - ability.resourceCost);
  await upsertCharacterResourceState(
    casterId,
    resourceSnapshot.resourceType,
    remaining,
    resourceSnapshot.maxValue,
  );
}

function safeParseMetadata(metadataJson: string | undefined): Record<string, any> | undefined {
  if (!metadataJson) {
    return undefined;
  }
  try {
    return JSON.parse(metadataJson) as Record<string, any>;
  } catch (_error) {
    return undefined;
  }
}

function executeFallbackAbility(
  participants: CombatEntitySnapshot[],
  request: CombatAbilityExecutionRequestDto,
): { abilityId: string; events: CombatAbilityEventDto[]; participants: CombatEntitySnapshot[] } {
  const targetIds = (request.targetIds ?? []).filter(Boolean);

  const events: CombatAbilityEventDto[] = [];

  targetIds.forEach((targetId) => {
    const target = participants.find((participant) => participant.id === targetId);
    if (!target) {
      return;
    }

    const caster = participants.find((participant) => participant.id === request.casterId);
    const baseDamage = Math.max(1, Math.round(caster?.stats?.['stat.attackPower'] ?? 1));
    target.health = Math.max(0, target.health - baseDamage);
    events.push({ kind: 'damage', targetId: target.id, amount: baseDamage });
  });

  return {
    abilityId: request.abilityId,
    events,
    participants,
  };
}
