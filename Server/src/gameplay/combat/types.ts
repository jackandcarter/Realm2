export interface StatRatioConfig {
  statId: string;
  ratio: number;
}

export interface StatJitterConfig {
  min: number;
  max: number;
}

export interface SerializedStatDefinition {
  id: string;
  displayName: string;
  description?: string;
  ratios?: StatRatioConfig[];
  jitter?: StatJitterConfig;
}

export interface TargetingProfile {
  mode: 'self' | 'primaryEnemy' | 'primaryAlly' | 'allEnemies' | 'allAllies';
  maxTargets?: number;
  includeCaster?: boolean;
}

export interface AbilityGraph {
  entryNodeId: string;
  nodes: AbilityGraphNode[];
}

export type AbilityGraphNode =
  | SelectTargetsNode
  | DealDamageNode
  | ApplyHealingNode
  | ApplyStateNode;

export interface GraphNodeBase {
  id: string;
  next?: string[];
}

export interface SelectTargetsNode extends GraphNodeBase {
  kind: 'selectTargets';
  selector: TargetingProfile['mode'];
  maxTargets?: number;
  includeCaster?: boolean;
}

export interface ScalingConfig {
  statId?: string;
  multiplier?: number;
}

export interface MitigationConfig {
  statId?: string;
  multiplier?: number;
}

export interface DealDamageNode extends GraphNodeBase {
  kind: 'dealDamage';
  baseDamage?: number;
  scaling?: ScalingConfig;
  mitigation?: MitigationConfig;
  minimumDamage?: number;
}

export interface ApplyHealingNode extends GraphNodeBase {
  kind: 'applyHealing';
  baseHeal?: number;
  scaling?: ScalingConfig;
}

export interface ApplyStateNode extends GraphNodeBase {
  kind: 'applyState';
  stateId: string;
  durationSeconds?: number;
}

export interface SerializedAbilityDefinition {
  id: string;
  name: string;
  summary?: string;
  graph: AbilityGraph;
}

export interface CombatEntitySnapshot {
  id: string;
  team: string;
  health: number;
  maxHealth: number;
  stats: Record<string, number>;
  states?: CombatStateInstance[];
}

export interface CombatStateInstance {
  id: string;
  durationSeconds?: number;
}

export interface AbilityExecutionContext {
  casterId: string;
  participants: CombatEntitySnapshot[];
  primaryTargetId?: string;
  random?: () => number;
}

export interface AbilityResultEvent {
  kind: 'damage' | 'heal' | 'stateApplied';
  targetId: string;
  amount?: number;
  stateId?: string;
  durationSeconds?: number;
}

export interface AbilityExecutionResult {
  abilityId: string;
  events: AbilityResultEvent[];
  participants: CombatEntitySnapshot[];
}
