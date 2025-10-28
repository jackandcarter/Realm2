export type ClassUnlockMethod = 'starter' | 'quest';

export interface ClassRuleDefinition {
  id: string;
  unlockMethod: ClassUnlockMethod;
  exclusiveRaceIds?: string[];
}

const CLASS_RULES: ClassRuleDefinition[] = [
  { id: 'warrior', unlockMethod: 'starter' },
  { id: 'wizard', unlockMethod: 'starter' },
  { id: 'time-mage', unlockMethod: 'starter' },
  { id: 'sage', unlockMethod: 'starter' },
  { id: 'rogue', unlockMethod: 'starter' },
  { id: 'ranger', unlockMethod: 'starter', exclusiveRaceIds: ['felarian'] },
  { id: 'necromancer', unlockMethod: 'starter', exclusiveRaceIds: ['revenant'] },
  { id: 'technomancer', unlockMethod: 'starter', exclusiveRaceIds: ['gearling'] },
  { id: 'builder', unlockMethod: 'quest' },
];

const CLASS_RULE_LOOKUP = new Map<string, ClassRuleDefinition>();

for (const rule of CLASS_RULES) {
  CLASS_RULE_LOOKUP.set(rule.id.toLowerCase(), rule);
}

function normalizeRaceId(raceId: string): string {
  return raceId.trim().toLowerCase();
}

export function getAllClassRules(): ClassRuleDefinition[] {
  return CLASS_RULES;
}

export function findClassRule(classId: string | undefined | null): ClassRuleDefinition | undefined {
  if (!classId) {
    return undefined;
  }
  return CLASS_RULE_LOOKUP.get(classId.trim().toLowerCase());
}

export function isClassAllowedForRace(classId: string, raceId: string): boolean {
  const rule = findClassRule(classId);
  if (!rule) {
    return false;
  }

  if (!rule.exclusiveRaceIds || rule.exclusiveRaceIds.length === 0) {
    return true;
  }

  const normalized = normalizeRaceId(raceId);
  return rule.exclusiveRaceIds.some((id) => normalizeRaceId(id) === normalized);
}

export function getAllowedClassIdsForRace(raceId: string): string[] {
  if (!raceId) {
    return [];
  }

  return CLASS_RULES.filter((rule) => isClassAllowedForRace(rule.id, raceId)).map((rule) => rule.id);
}

export function getStarterClassIdsForRace(raceId: string): string[] {
  if (!raceId) {
    return [];
  }

  return CLASS_RULES.filter(
    (rule) => rule.unlockMethod === 'starter' && isClassAllowedForRace(rule.id, raceId)
  ).map((rule) => rule.id);
}
