import {
  AbilityExecutionContext,
  AbilityExecutionResult,
  AbilityGraphNode,
  AbilityResultEvent,
  ApplyHealingNode,
  ApplyStateNode,
  CombatEntitySnapshot,
  DealDamageNode,
  SelectTargetsNode,
} from './types';
import { AbilityRegistry, CombatStatRegistry } from './registries';
import { evaluateStatWithRatios } from './statCalculator';

export interface AbilityExecutorDependencies {
  stats: CombatStatRegistry;
  abilities: AbilityRegistry;
}

export class AbilityExecutor {
  constructor(private readonly deps: AbilityExecutorDependencies) {}

  execute(abilityId: string, context: AbilityExecutionContext): AbilityExecutionResult {
    const ability = this.deps.abilities.require(abilityId);

    const participants = context.participants.map((entry) => cloneParticipant(entry));
    const caster = participants.find((entity) => entity.id === context.casterId);

    if (!caster) {
      throw new Error(`Caster '${context.casterId}' is not present in the combat snapshot.`);
    }

    const random = context.random ?? Math.random;
    const events: AbilityResultEvent[] = [];
    let currentTargets: CombatEntitySnapshot[] = [];

    const queue: string[] = [ability.graph.entryNodeId];
    const maxIterations = (ability.graph.nodes?.length ?? 0) * 4;
    let iteration = 0;

    while (queue.length > 0 && iteration < maxIterations) {
      iteration += 1;
      const nodeId = queue.shift();
      if (!nodeId) {
        continue;
      }

      const node = ability.nodeLookup.get(nodeId);
      if (!node) {
        continue;
      }

      switch (node.kind) {
        case 'selectTargets':
          currentTargets = resolveTargets(node, caster, participants, context);
          break;
        case 'dealDamage':
          if (currentTargets.length > 0) {
            const damageEvents = applyDamageNode(node, caster, currentTargets, this.deps.stats, random);
            events.push(...damageEvents);
          }
          break;
        case 'applyHealing':
          if (currentTargets.length > 0) {
            const healEvents = applyHealingNode(node, caster, currentTargets, this.deps.stats, random);
            events.push(...healEvents);
          }
          break;
        case 'applyState':
          if (currentTargets.length > 0) {
            const stateEvents = applyStateNode(node, currentTargets);
            events.push(...stateEvents);
          }
          break;
        default:
          exhaust(node);
      }

      (node.next ?? []).forEach((next) => queue.push(next));
    }

    return {
      abilityId,
      events,
      participants,
    };
  }
}

function resolveTargets(
  node: SelectTargetsNode,
  caster: CombatEntitySnapshot,
  participants: CombatEntitySnapshot[],
  context: AbilityExecutionContext,
): CombatEntitySnapshot[] {
  switch (node.selector) {
    case 'self':
      return [caster];
    case 'primaryEnemy':
      return resolvePrimaryTarget(caster, participants, context.primaryTargetId, false, node.maxTargets ?? 1);
    case 'primaryAlly':
      return resolvePrimaryTarget(caster, participants, context.primaryTargetId, true, node.maxTargets ?? 1);
    case 'allEnemies':
      return limitTargets(
        participants.filter((participant) => participant.team !== caster.team),
        node.maxTargets,
      );
    case 'allAllies':
      {
        const allies = participants.filter((participant) => participant.team === caster.team);
        if (!node.includeCaster) {
          return limitTargets(allies.filter((ally) => ally.id !== caster.id), node.maxTargets);
        }

        return limitTargets(allies, node.maxTargets);
      }
    default:
      return [];
  }
}

function resolvePrimaryTarget(
  caster: CombatEntitySnapshot,
  participants: CombatEntitySnapshot[],
  primaryTargetId: string | undefined,
  preferAllies: boolean,
  maxTargets: number,
): CombatEntitySnapshot[] {
  const normalizedId = primaryTargetId?.trim().toLowerCase();
  if (normalizedId) {
    const explicit = participants.find(
      (participant) => participant.id.trim().toLowerCase() === normalizedId,
    );

    if (explicit && (preferAllies === (explicit.team === caster.team))) {
      return [explicit];
    }
  }

  const candidates = participants.filter((participant) =>
    preferAllies ? participant.team === caster.team : participant.team !== caster.team,
  );

  if (candidates.length === 0) {
    return [];
  }

  return candidates.slice(0, Math.max(1, maxTargets));
}

function limitTargets(
  targets: CombatEntitySnapshot[],
  maxTargets: number | undefined,
): CombatEntitySnapshot[] {
  if (!maxTargets || maxTargets <= 0) {
    return [...targets];
  }

  return targets.slice(0, maxTargets);
}

function applyDamageNode(
  node: DealDamageNode,
  caster: CombatEntitySnapshot,
  targets: CombatEntitySnapshot[],
  stats: CombatStatRegistry,
  random: () => number,
): AbilityResultEvent[] {
  const scaling = node.scaling?.statId
    ? evaluateStatWithRatios({
        statId: node.scaling.statId,
        stats: caster.stats,
        registry: stats,
        random,
      }) * (node.scaling.multiplier ?? 1)
    : 0;

  const baseDamage = node.baseDamage ?? 0;

  return targets.map((target) => {
    const mitigation = node.mitigation?.statId
      ? evaluateStatWithRatios({
          statId: node.mitigation.statId,
          stats: target.stats,
          registry: stats,
          random,
        }) * (node.mitigation.multiplier ?? 1)
      : 0;

    const rawDamage = baseDamage + scaling - mitigation;
    const amount = Math.max(node.minimumDamage ?? 0, roundToTwoDecimals(Math.max(0, rawDamage)));
    target.health = Math.max(0, roundToTwoDecimals(target.health - amount));
    return {
      kind: 'damage' as const,
      targetId: target.id,
      amount,
    };
  });
}

function applyHealingNode(
  node: ApplyHealingNode,
  caster: CombatEntitySnapshot,
  targets: CombatEntitySnapshot[],
  stats: CombatStatRegistry,
  random: () => number,
): AbilityResultEvent[] {
  const scaling = node.scaling?.statId
    ? evaluateStatWithRatios({
        statId: node.scaling.statId,
        stats: caster.stats,
        registry: stats,
        random,
      }) * (node.scaling.multiplier ?? 1)
    : 0;

  const baseHeal = node.baseHeal ?? 0;

  return targets.map((target) => {
    const amount = roundToTwoDecimals(Math.max(0, baseHeal + scaling));
    target.health = Math.min(target.maxHealth, roundToTwoDecimals(target.health + amount));
    return {
      kind: 'heal' as const,
      targetId: target.id,
      amount,
    };
  });
}

function applyStateNode(
  node: ApplyStateNode,
  targets: CombatEntitySnapshot[],
): AbilityResultEvent[] {
  return targets.map((target) => {
    const states = target.states ?? [];
    states.push({ id: node.stateId, durationSeconds: node.durationSeconds });
    target.states = states;
    return {
      kind: 'stateApplied' as const,
      targetId: target.id,
      stateId: node.stateId,
      durationSeconds: node.durationSeconds,
    };
  });
}

function cloneParticipant(participant: CombatEntitySnapshot): CombatEntitySnapshot {
  return {
    id: participant.id,
    team: participant.team,
    health: participant.health,
    maxHealth: participant.maxHealth,
    stats: { ...participant.stats },
    states: participant.states ? participant.states.map((state) => ({ ...state })) : undefined,
  };
}

function roundToTwoDecimals(value: number): number {
  return Math.round(value * 100) / 100;
}

function exhaust(node: AbilityGraphNode): never {
  throw new Error(`Unhandled graph node '${(node as AbilityGraphNode).kind}'.`);
}
