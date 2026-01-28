using System;

namespace Client.CharacterCreation
{
    [Serializable]
    public class RaceDefinition
    {
        public string Id;
        public string DisplayName;
        public string LoreSummary;
        public string AppearanceSummary;
        public string[] SignatureAbilities;
        public string[] AllowedClassIds;
        public string[] StarterClassIds;
        public RaceCustomizationOptions Customization;
    }

    [Serializable]
    public class RaceCustomizationOptions
    {
        public FloatRange Height;
        public FloatRange Build;
        public RaceFeatureDefinition[] FeatureOptions;
        public string[] AdjustableFeatures;
    }

    [Serializable]
    public class RaceFeatureDefinition
    {
        public string Id;
        public string DisplayName;
        public string[] Options;
        public string DefaultOption;
    }

    [Serializable]
    public struct FloatRange
    {
        public float Min;
        public float Max;

        public FloatRange(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }
}
