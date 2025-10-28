import {
  AbilityExecutor,
  AbilityRegistry,
  CombatStatRegistry,
} from '../../../src/gameplay/combat';
import { generatedAbilityDefinitions } from '../../../src/gameplay/combat/generated/abilityRegistry';
import { generatedStatDefinitions } from '../../../src/gameplay/combat/generated/statRegistry';
import { AbilityExecutionContext } from '../../../src/gameplay/combat/types';

describe('AbilityExecutor', () => {
  const statRegistry = new CombatStatRegistry(generatedStatDefinitions);
  const abilityRegistry = new AbilityRegistry(generatedAbilityDefinitions);
  const executor = new AbilityExecutor({ stats: statRegistry, abilities: abilityRegistry });

  it('applies stat ratios, jitter, and mitigation when executing damage abilities', () => {
    const context: AbilityExecutionContext = {
      casterId: 'hero',
      primaryTargetId: 'goblin',
      random: createSequenceRandom([0.25, 0.75]),
      participants: [
        {
          id: 'hero',
          team: 'alliance',
          health: 100,
          maxHealth: 100,
          stats: {
            'stat.attackPower': 30,
            'stat.strength': 18,
            'stat.agility': 12,
            'stat.vitality': 10,
          },
        },
        {
          id: 'goblin',
          team: 'horde',
          health: 80,
          maxHealth: 80,
          stats: {
            'stat.defense': 10,
            'stat.vitality': 9,
          },
        },
      ],
    };

    const result = executor.execute('ability.powerStrike', context);

    const damageEvent = result.events.find((event) => event.kind === 'damage');
    expect(damageEvent).toBeDefined();
    expect(damageEvent?.amount).toBeCloseTo(51.54, 2);

    const target = result.participants.find((entity) => entity.id === 'goblin');
    expect(target?.health).toBeCloseTo(28.46, 2);
  });

  it('heals allies, applies rejuvenation state, and respects stat ratios', () => {
    const context: AbilityExecutionContext = {
      casterId: 'sage',
      random: () => 0.1,
      participants: [
        {
          id: 'sage',
          team: 'alliance',
          health: 65,
          maxHealth: 100,
          stats: {
            'stat.magic': 22,
            'stat.spirit': 14,
          },
        },
        {
          id: 'templar',
          team: 'alliance',
          health: 50,
          maxHealth: 90,
          stats: {
            'stat.magic': 8,
            'stat.spirit': 6,
          },
        },
        {
          id: 'archer',
          team: 'alliance',
          health: 40,
          maxHealth: 80,
          stats: {
            'stat.magic': 6,
            'stat.spirit': 4,
          },
        },
        {
          id: 'ogre',
          team: 'horde',
          health: 120,
          maxHealth: 120,
          stats: {
            'stat.defense': 18,
          },
        },
      ],
    };

    const result = executor.execute('ability.spiritBlessing', context);

    const healEvents = result.events.filter((event) => event.kind === 'heal');
    expect(healEvents).toHaveLength(3);
    healEvents.forEach((event) => {
      expect(event.amount).toBeCloseTo(23.02, 2);
    });

    const rejuvenated = result.participants
      .filter((entity) => entity.team === 'alliance')
      .map((entity) => entity.states ?? []);

    rejuvenated.forEach((states) => {
      expect(states).toEqual([
        { id: 'state.rejuvenation', durationSeconds: 6 },
      ]);
    });

    const caster = result.participants.find((entity) => entity.id === 'sage');
    expect(caster?.health).toBeCloseTo(88.02, 2);
    const templar = result.participants.find((entity) => entity.id === 'templar');
    expect(templar?.health).toBeCloseTo(73.02, 2);
    const archer = result.participants.find((entity) => entity.id === 'archer');
    expect(archer?.health).toBeCloseTo(63.02, 2);
  });
});

function createSequenceRandom(sequence: number[]): () => number {
  let index = 0;
  return () => {
    const value = sequence[index] ?? sequence[sequence.length - 1] ?? 0;
    index = Math.min(sequence.length - 1, index + 1);
    return value;
  };
}
