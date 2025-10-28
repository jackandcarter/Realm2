import { getAllowedClassIdsForRace, getStarterClassIdsForRace } from './classRules';

export interface RaceCustomizationRange {
  min: number;
  max: number;
}

export interface RaceCustomizationOptions {
  height?: RaceCustomizationRange;
  build?: RaceCustomizationRange;
}

export interface RaceDefinition {
  id: string;
  displayName: string;
  customization?: RaceCustomizationOptions;
  allowedClassIds: string[];
  starterClassIds: string[];
}

function createRaceDefinition(
  id: string,
  displayName: string,
  customization?: RaceCustomizationOptions
): RaceDefinition {
  return {
    id,
    displayName,
    customization,
    allowedClassIds: getAllowedClassIdsForRace(id),
    starterClassIds: getStarterClassIdsForRace(id),
  };
}

export const RACE_DEFINITIONS: RaceDefinition[] = [
  createRaceDefinition('felarian', 'Felarian', {
    height: { min: 1.55, max: 1.9 },
    build: { min: 0.35, max: 0.7 },
  }),
  createRaceDefinition('human', 'Human', {
    height: { min: 1.5, max: 2.05 },
    build: { min: 0.25, max: 0.85 },
  }),
  createRaceDefinition('crystallian', 'Crystallian', {
    height: { min: 1.8, max: 2.3 },
    build: { min: 0.45, max: 0.95 },
  }),
  createRaceDefinition('revenant', 'Revenant', {
    height: { min: 1.65, max: 2.0 },
    build: { min: 0.2, max: 0.6 },
  }),
  createRaceDefinition('gearling', 'Gearling', {
    height: { min: 1.0, max: 1.4 },
    build: { min: 0.25, max: 0.55 },
  }),
];

export const DEFAULT_RACE_ID = 'human';

const raceMap = new Map<string, RaceDefinition>();
for (const race of RACE_DEFINITIONS) {
  raceMap.set(normalizeRaceKey(race.id), race);
}

function normalizeRaceKey(id: string): string {
  return id.trim().toLowerCase();
}

export function findRaceById(rawId: string | undefined | null): RaceDefinition | undefined {
  if (!rawId) {
    return undefined;
  }
  return raceMap.get(normalizeRaceKey(rawId));
}

export function getDefaultRace(): RaceDefinition {
  const race = findRaceById(DEFAULT_RACE_ID);
  if (!race) {
    throw new Error('Default race definition is missing');
  }
  return race;
}

export function getCanonicalRaceIds(): string[] {
  return RACE_DEFINITIONS.map((race) => race.id);
}
