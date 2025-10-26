export interface CharacterClassState {
  classId: string;
  unlocked: boolean;
}

export function sanitizeClassStates(input: unknown): CharacterClassState[] {
  if (!Array.isArray(input)) {
    return [];
  }

  const normalized: CharacterClassState[] = [];
  const seen = new Set<string>();

  for (const entry of input) {
    if (!entry || typeof entry !== 'object') {
      continue;
    }

    const value = entry as Record<string, unknown>;
    if (typeof value.classId !== 'string') {
      continue;
    }

    const classId = value.classId.trim();
    if (!classId) {
      continue;
    }

    const key = classId.toLowerCase();
    const unlocked = Boolean(value.unlocked);

    if (seen.has(key)) {
      if (unlocked) {
        const existing = normalized.find((state) => state.classId.toLowerCase() === key);
        if (existing) {
          existing.unlocked = true;
        }
      }
      continue;
    }

    normalized.push({ classId, unlocked });
    seen.add(key);
  }

  if (!seen.has('builder')) {
    normalized.push({ classId: 'builder', unlocked: false });
  }

  return normalized;
}

export function serializeClassStates(states: CharacterClassState[] | undefined): string {
  if (!states || states.length === 0) {
    return '[]';
  }

  return JSON.stringify(sanitizeClassStates(states));
}

export function deserializeClassStates(json: string | null | undefined): CharacterClassState[] {
  if (!json) {
    return sanitizeClassStates([]);
  }

  try {
    const parsed = JSON.parse(json) as unknown;
    return sanitizeClassStates(parsed);
  } catch (_error) {
    return sanitizeClassStates([]);
  }
}
