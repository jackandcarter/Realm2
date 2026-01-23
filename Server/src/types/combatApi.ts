export interface CombatVector3Dto {
  x: number;
  y: number;
  z: number;
}

export interface CombatAbilityExecutionRequestDto {
  requestId: string;
  abilityId: string;
  casterId: string;
  primaryTargetId?: string;
  targetIds?: string[];
  targetPoint?: CombatVector3Dto;
  clientTime: number;
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
