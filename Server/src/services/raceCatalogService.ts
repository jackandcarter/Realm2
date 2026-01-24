import { RaceCustomizationOptions, RaceDefinition } from '../config/races';
import { listRaceClassRules, listRaces } from '../db/raceCatalogRepository';

interface RaceCatalogSnapshot {
  races: RaceDefinition[];
  rules: Array<{ raceId: string; classId: string; unlockMethod: string }>;
}

function parseCustomization(raw: string): RaceCustomizationOptions | undefined {
  if (!raw) {
    return undefined;
  }
  try {
    const parsed = JSON.parse(raw) as RaceCustomizationOptions;
    return parsed && typeof parsed === 'object' ? parsed : undefined;
  } catch (_error) {
    return undefined;
  }
}

async function loadRaceCatalogSnapshot(): Promise<RaceCatalogSnapshot | null> {
  const raceRows = await listRaces();
  if (raceRows.length === 0) {
    return null;
  }
  const rules = await listRaceClassRules();
  const races: RaceDefinition[] = raceRows.map((row) => ({
    id: row.id,
    displayName: row.displayName,
    customization: parseCustomization(row.customizationJson),
    allowedClassIds: [],
    starterClassIds: [],
  }));
  return { races, rules };
}

function applyRulesToRace(
  race: RaceDefinition,
  rules: Array<{ raceId: string; classId: string; unlockMethod: string }>
): RaceDefinition {
  const raceRules = rules.filter((rule) => rule.raceId === race.id);
  const allowedClassIds = raceRules.map((rule) => rule.classId);
  const starterClassIds = raceRules
    .filter((rule) => rule.unlockMethod === 'starter')
    .map((rule) => rule.classId);
  return {
    ...race,
    allowedClassIds,
    starterClassIds,
  };
}

export async function getRaceDefinitionById(
  raceId: string
): Promise<RaceDefinition | undefined> {
  const snapshot = await loadRaceCatalogSnapshot();
  if (!snapshot) {
    throw new Error('Race catalog is missing: seed the races table before use.');
  }
  const race = snapshot.races.find((entry) => entry.id.toLowerCase() === raceId.toLowerCase());
  return race ? applyRulesToRace(race, snapshot.rules) : undefined;
}

export async function listCanonicalRaceIds(): Promise<string[]> {
  const snapshot = await loadRaceCatalogSnapshot();
  if (!snapshot) {
    throw new Error('Race catalog is missing: seed the races table before use.');
  }
  return snapshot.races.map((race) => race.id);
}

export async function getDefaultRaceDefinition(): Promise<RaceDefinition> {
  const snapshot = await loadRaceCatalogSnapshot();
  if (!snapshot) {
    throw new Error('Race catalog is missing: seed the races table before use.');
  }
  const defaultRace =
    snapshot.races.find((race) => race.id === 'human') ?? snapshot.races[0];
  if (!defaultRace) {
    throw new Error('Default race definition is missing');
  }
  return applyRulesToRace(defaultRace, snapshot.rules);
}

export async function listAllowedClassIdsForRace(raceId: string): Promise<string[]> {
  const snapshot = await loadRaceCatalogSnapshot();
  if (!snapshot) {
    throw new Error('Race catalog is missing: seed the races table before use.');
  }
  const race = snapshot.races.find((entry) => entry.id === raceId);
  if (!race) {
    return [];
  }
  return applyRulesToRace(race, snapshot.rules).allowedClassIds;
}

export async function listStarterClassIdsForRace(raceId: string): Promise<string[]> {
  const snapshot = await loadRaceCatalogSnapshot();
  if (!snapshot) {
    throw new Error('Race catalog is missing: seed the races table before use.');
  }
  const race = snapshot.races.find((entry) => entry.id === raceId);
  if (!race) {
    return [];
  }
  return applyRulesToRace(race, snapshot.rules).starterClassIds;
}
