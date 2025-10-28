import { CombatStatRegistry } from './registries';
import { StatJitterConfig, StatRatioConfig } from './types';

export interface EvaluateStatOptions {
  statId: string;
  stats: Record<string, number>;
  registry: CombatStatRegistry;
  ratioOverrides?: StatRatioConfig[];
  jitterOverride?: StatJitterConfig;
  random?: () => number;
}

export function evaluateStatWithRatios(options: EvaluateStatOptions): number {
  const { statId, stats, registry } = options;
  const definition = registry.get(statId);
  const baseValue = stats?.[statId] ?? 0;

  const ratios = (options.ratioOverrides ?? definition?.ratios ?? []).filter(
    (ratio) => Boolean(ratio?.statId) && Number.isFinite(ratio?.ratio),
  );

  const ratioContribution = ratios.reduce((total, ratio) => {
    const value = stats?.[ratio.statId] ?? 0;
    return total + value * ratio.ratio;
  }, 0);

  const jitterSource = options.jitterOverride ?? definition?.jitter;
  const jitterSample = sampleJitter(jitterSource, options.random);

  return baseValue + ratioContribution + jitterSample;
}

function sampleJitter(
  jitter: StatJitterConfig | undefined,
  random?: () => number,
): number {
  if (!jitter) {
    return 0;
  }

  const min = Math.min(jitter.min, jitter.max);
  const max = Math.max(jitter.min, jitter.max);

  if (min === 0 && max === 0) {
    return 0;
  }

  const sampler = random ?? Math.random;
  const normalized = clamp01(sampler());
  return min + (max - min) * normalized;
}

function clamp01(value: number): number {
  if (!Number.isFinite(value)) {
    return 0;
  }

  if (value < 0) {
    return 0;
  }

  if (value > 1) {
    return 1;
  }

  return value;
}
