export type JsonPrimitive = string | number | boolean | null;

export type JsonValue = JsonPrimitive | JsonValue[] | { [key: string]: JsonValue };

export interface CharacterAppearance {
  hairStyle?: string;
  hairColor?: string;
  eyeColor?: string;
  skinTone?: string;
  height?: number;
  build?: number;
  bodyType?: string;
  accessories?: JsonValue;
  markings?: JsonValue;
  [key: string]: JsonValue | undefined;
}

export function serializeAppearance(appearance?: CharacterAppearance): string {
  if (!appearance || Object.keys(appearance).length === 0) {
    return '{}';
  }
  return JSON.stringify(appearance);
}

export function deserializeAppearance(json: string | null | undefined): CharacterAppearance {
  if (!json) {
    return {};
  }

  try {
    const parsed = JSON.parse(json) as unknown;
    if (isCharacterAppearance(parsed)) {
      return parsed;
    }
  } catch (_error) {
    // ignore JSON parsing errors and fall back to an empty appearance object
  }

  return {};
}

export function isCharacterAppearance(value: unknown): value is CharacterAppearance {
  if (!value || typeof value !== 'object' || Array.isArray(value)) {
    return false;
  }

  return Object.values(value as Record<string, unknown>).every(isJsonValue);
}

function isJsonValue(value: unknown): value is JsonValue {
  if (value === null) {
    return true;
  }

  const valueType = typeof value;
  if (valueType === 'string' || valueType === 'number' || valueType === 'boolean') {
    return true;
  }

  if (Array.isArray(value)) {
    return value.every(isJsonValue);
  }

  if (valueType === 'object') {
    return Object.values(value as Record<string, unknown>).every(isJsonValue);
  }

  return false;
}
