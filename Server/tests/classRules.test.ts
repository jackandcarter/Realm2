import {
  getAllowedClassIdsForRace,
  getStarterClassIdsForRace,
  isClassAllowedForRace,
} from '../src/config/classRules';
import { RACE_DEFINITIONS } from '../src/config/races';

describe('Class rules catalog', () => {
  const canonicalRaces = RACE_DEFINITIONS.map((race) => race.id);

  it('exposes core classes to every race', () => {
    for (const raceId of canonicalRaces) {
      const allowed = getAllowedClassIdsForRace(raceId);
      expect(allowed).toEqual(expect.arrayContaining(['warrior', 'wizard', 'time-mage', 'sage', 'rogue']));
    }
  });

  it('enforces race-exclusive class assignments', () => {
    expect(getAllowedClassIdsForRace('felarian')).toContain('ranger');
    expect(getAllowedClassIdsForRace('felarian')).not.toContain('necromancer');
    expect(getAllowedClassIdsForRace('felarian')).not.toContain('technomancer');

    expect(getAllowedClassIdsForRace('revenant')).toContain('necromancer');
    expect(getAllowedClassIdsForRace('revenant')).not.toContain('ranger');
    expect(getAllowedClassIdsForRace('revenant')).not.toContain('technomancer');

    expect(getAllowedClassIdsForRace('gearling')).toContain('technomancer');
    expect(getAllowedClassIdsForRace('gearling')).not.toContain('ranger');
    expect(getAllowedClassIdsForRace('gearling')).not.toContain('necromancer');
  });

  it('marks builder as available but not a starter option', () => {
    for (const raceId of canonicalRaces) {
      const allowed = getAllowedClassIdsForRace(raceId);
      const starters = getStarterClassIdsForRace(raceId);
      expect(allowed).toContain('builder');
      expect(starters).not.toContain('builder');
    }
  });

  it('produces starter lists that are subsets of the allowed classes', () => {
    for (const raceId of canonicalRaces) {
      const allowed = new Set(getAllowedClassIdsForRace(raceId));
      const starters = getStarterClassIdsForRace(raceId);
      for (const starter of starters) {
        expect(allowed.has(starter)).toBe(true);
      }
    }
  });

  it('validates explicit race restrictions via helper', () => {
    expect(isClassAllowedForRace('ranger', 'felarian')).toBe(true);
    expect(isClassAllowedForRace('ranger', 'human')).toBe(false);
    expect(isClassAllowedForRace('technomancer', 'gearling')).toBe(true);
    expect(isClassAllowedForRace('technomancer', 'felarian')).toBe(false);
  });
});
