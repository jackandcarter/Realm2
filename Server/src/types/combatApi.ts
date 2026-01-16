export interface CombatVector3Dto {
  x: number;
  y: number;
  z: number;
}

export interface CombatParticipantStatDto {
  id: string;
  value: number;
}

export interface CombatParticipantStateDto {
  id: string;
  durationSeconds?: number;
}

export interface CombatParticipantSnapshotDto {
  id: string;
  team: string;
  health: number;
  maxHealth: number;
  stats?: CombatParticipantStatDto[];
  states?: CombatParticipantStateDto[];
}

export interface CombatAbilityExecutionRequestDto {
  requestId: string;
  abilityId: string;
  casterId: string;
  primaryTargetId?: string;
  targetIds?: string[];
  targetPoint?: CombatVector3Dto;
  clientTime: number;
  baseDamage?: number;
  participants: CombatParticipantSnapshotDto[];
}

export interface CombatAbilityEventDto {
  kind: 'damage' | 'heal' | 'stateApplied';
  targetId: string;
  amount?: number;
  stateId?: string;
  durationSeconds?: number;
}

export interface CombatAbilityExecutionResponseDto {
  requestId: string;
  abilityId: string;
  casterId: string;
  clientTime: number;
  targetIds: string[];
  serverTime: number;
  events: CombatAbilityEventDto[];
}
