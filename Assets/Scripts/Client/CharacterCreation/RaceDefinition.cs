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
        public string[] AdjustableFeatures;
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
