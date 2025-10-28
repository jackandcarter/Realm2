import {
  SerializedAbilityDefinition,
  SerializedStatDefinition,
  StatJitterConfig,
  StatRatioConfig,
} from './types';

export interface StatDefinitionRecord {
  id: string;
  displayName: string;
  description?: string;
  ratios: StatRatioConfig[];
  jitter?: StatJitterConfig;
}

export class CombatStatRegistry {
  private readonly definitions = new Map<string, StatDefinitionRecord>();

  constructor(definitions: SerializedStatDefinition[]) {
    definitions.forEach((definition) => {
      if (!definition?.id) {
        return;
      }

      const ratios = (definition.ratios ?? [])
        .filter((ratio) => Boolean(ratio?.statId) && Number.isFinite(ratio?.ratio))
        .map((ratio) => ({ statId: ratio.statId, ratio: ratio.ratio }))
        .filter((ratio, index, self) =>
          self.findIndex((entry) => entry.statId === ratio.statId) === index,
        );

      const jitter = normalizeJitter(definition.jitter);

      this.definitions.set(definition.id, {
        id: definition.id,
        displayName: definition.displayName,
        description: definition.description,
        ratios,
        jitter,
      });
    });
  }

  get(statId: string): StatDefinitionRecord | undefined {
    return this.definitions.get(statId);
  }

  require(statId: string): StatDefinitionRecord {
    const record = this.get(statId);
    if (!record) {
      throw new Error(`Stat '${statId}' was not registered by the editor.`);
    }

    return record;
  }
}

export interface AbilityDefinitionRecord extends SerializedAbilityDefinition {
  nodeLookup: Map<string, SerializedAbilityDefinition['graph']['nodes'][number]>;
}

export class AbilityRegistry {
  private readonly definitions = new Map<string, AbilityDefinitionRecord>();

  constructor(definitions: SerializedAbilityDefinition[]) {
    definitions.forEach((definition) => {
      if (!definition?.id) {
        return;
      }

      const nodeLookup = new Map(
        (definition.graph?.nodes ?? [])
          .filter((node) => Boolean(node?.id))
          .map((node) => [node.id, node]),
      );

      this.definitions.set(definition.id, {
        ...definition,
        nodeLookup,
      });
    });
  }

  get(abilityId: string): AbilityDefinitionRecord | undefined {
    return this.definitions.get(abilityId);
  }

  require(abilityId: string): AbilityDefinitionRecord {
    const definition = this.get(abilityId);
    if (!definition) {
      throw new Error(`Ability '${abilityId}' was not registered by the editor.`);
    }

    return definition;
  }
}

function normalizeJitter(jitter?: StatJitterConfig): StatJitterConfig | undefined {
  if (!jitter) {
    return undefined;
  }

  if (!Number.isFinite(jitter.min) || !Number.isFinite(jitter.max)) {
    return undefined;
  }

  if (jitter.min === 0 && jitter.max === 0) {
    return undefined;
  }

  if (jitter.min <= jitter.max) {
    return { min: jitter.min, max: jitter.max };
  }

  return { min: jitter.max, max: jitter.min };
}
