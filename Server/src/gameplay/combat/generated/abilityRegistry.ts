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
  {
    id: 'ability.necromancer_reaper_combo',
    name: "Reaper's Spiral",
    summary: 'A four-hit scythe combo that cleaves nearby enemies with necrotic force.',
    delivery: {
      type: 'melee',
      rangeMeters: 2.5,
      hitbox: {
        shape: 'cone',
        size: { x: 2.2, y: 1.6, z: 2.4 },
        offset: { x: 0, y: 0.9, z: 1.1 },
        useCasterFacing: true,
        activeSeconds: 0.25,
        requiresContact: true,
      },
    },
    combo: {
      resetSeconds: 1.1,
      stages: [
        { id: 'swing-1', displayName: 'Grave Cut', damageMultiplier: 1.0, windowSeconds: 0.5 },
        { id: 'swing-2', displayName: 'Bone Splitter', damageMultiplier: 1.1, windowSeconds: 0.5 },
        { id: 'swing-3', displayName: 'Soul Shear', damageMultiplier: 1.2, windowSeconds: 0.55 },
        { id: 'swing-4', displayName: "Mortal Reap", damageMultiplier: 1.4, windowSeconds: 0.6 },
      ],
    },
    graph: {
      entryNodeId: 'select-target',
      nodes: [
        {
          id: 'select-target',
          kind: 'selectTargets',
          selector: 'primaryEnemy',
          next: ['deal-damage', 'apply-curse'],
        },
        {
          id: 'deal-damage',
          kind: 'dealDamage',
          baseDamage: 7,
          scaling: {
            statId: 'stat.attackPower',
            multiplier: 0.9,
          },
          mitigation: {
            statId: 'stat.defense',
            multiplier: 0.35,
          },
        },
        {
          id: 'apply-curse',
          kind: 'applyState',
          stateId: 'state.necrotic_wound',
          durationSeconds: 4,
        },
      ],
    },
  },
  {
    id: 'ability.necromancer_soul_bolt',
    name: 'Soul Bolt',
    summary: 'Launch a necrotic bolt that damages a distant target and weakens their defenses.',
    delivery: {
      type: 'projectile',
      rangeMeters: 16,
    },
    graph: {
      entryNodeId: 'select-target',
      nodes: [
        {
          id: 'select-target',
          kind: 'selectTargets',
          selector: 'primaryEnemy',
          next: ['deal-damage', 'apply-debuff'],
        },
        {
          id: 'deal-damage',
          kind: 'dealDamage',
          baseDamage: 6,
          scaling: {
            statId: 'stat.magic',
            multiplier: 1.05,
          },
          mitigation: {
            statId: 'stat.spirit',
            multiplier: 0.25,
          },
        },
        {
          id: 'apply-debuff',
          kind: 'applyState',
          stateId: 'state.soul_fray',
          durationSeconds: 6,
        },
      ],
    },
  },
];
