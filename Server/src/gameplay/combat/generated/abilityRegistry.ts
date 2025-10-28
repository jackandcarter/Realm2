import { SerializedAbilityDefinition } from '../types';

export const generatedAbilityDefinitions: SerializedAbilityDefinition[] = [
  {
    id: 'ability.powerStrike',
    name: 'Power Strike',
    summary: 'Deliver a heavy melee blow against the primary target.',
    graph: {
      entryNodeId: 'select-target',
      nodes: [
        {
          id: 'select-target',
          kind: 'selectTargets',
          selector: 'primaryEnemy',
          next: ['deal-damage'],
        },
        {
          id: 'deal-damage',
          kind: 'dealDamage',
          baseDamage: 8,
          scaling: {
            statId: 'stat.attackPower',
            multiplier: 1.1,
          },
          mitigation: {
            statId: 'stat.defense',
            multiplier: 0.4,
          },
        },
      ],
    },
  },
  {
    id: 'ability.spiritBlessing',
    name: 'Spirit Blessing',
    summary: 'Channel restorative energy to all nearby allies.',
    graph: {
      entryNodeId: 'select-allies',
      nodes: [
        {
          id: 'select-allies',
          kind: 'selectTargets',
          selector: 'allAllies',
          includeCaster: true,
          next: ['heal-allies'],
        },
        {
          id: 'heal-allies',
          kind: 'applyHealing',
          baseHeal: 4,
          scaling: {
            statId: 'stat.magic',
            multiplier: 0.6,
          },
          next: ['apply-renewal'],
        },
        {
          id: 'apply-renewal',
          kind: 'applyState',
          stateId: 'state.rejuvenation',
          durationSeconds: 6,
        },
      ],
    },
  },
];
