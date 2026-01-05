using Unity.Mathematics;

namespace Digger.Modules.Core.Sources
{
    public static class NoiseUtils
    {
        private const float DefaultLacunarity = 2f;
        private const float DefaultGain = 0.5f;

        public static float Perlin(float3 position, int seed, float frequency)
        {
            if (frequency <= 0f)
            {
                return 0f;
            }

            var seedOffset = new float3(seed * 0.123f, seed * 0.456f, seed * 0.789f);
            return noise.snoise((position + seedOffset) * frequency);
        }

        public static float Ridged(float3 position, int seed, float frequency, float ridgeSharpness)
        {
            var baseNoise = Perlin(position, seed, frequency);
            var ridge = 1f - math.abs(baseNoise);
            ridge = math.pow(math.saturate(ridge), math.max(0.0001f, ridgeSharpness));
            return ridge * 2f - 1f;
        }

        public static float Fbm(float3 position, int seed, int octaves, float frequency, float ridgeSharpness)
        {
            if (frequency <= 0f)
            {
                return 0f;
            }

            var octavesCount = math.clamp(octaves, 1, 8);
            var useRidged = ridgeSharpness > 0f;
            var amplitude = 1f;
            var maxAmplitude = 0f;
            var total = 0f;
            var currentFrequency = frequency;

            for (var i = 0; i < octavesCount; i++)
            {
                var sampleSeed = seed + i * 17;
                var sample = useRidged
                    ? Ridged(position, sampleSeed, currentFrequency, ridgeSharpness)
                    : Perlin(position, sampleSeed, currentFrequency);
                total += sample * amplitude;
                maxAmplitude += amplitude;
                amplitude *= DefaultGain;
                currentFrequency *= DefaultLacunarity;
            }

            return maxAmplitude > 0f ? total / maxAmplitude : 0f;
        }

        public static float ApplyTerracing(float value, int steps)
        {
            if (steps <= 1)
            {
                return value;
            }

            var levels = math.max(1, steps - 1);
            var normalized = math.saturate((value + 1f) * 0.5f);
            var stepped = math.round(normalized * levels) / levels;
            return stepped * 2f - 1f;
        }
    }
}
