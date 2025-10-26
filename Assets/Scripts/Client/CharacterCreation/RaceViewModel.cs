using UnityEngine;

namespace Client.CharacterCreation
{
    public sealed class RaceViewModel
    {
        public RaceViewModel(RaceDefinition definition, RaceVisualEntry visuals)
        {
            Definition = definition;
            Visuals = visuals;
        }

        public RaceDefinition Definition { get; }
        public RaceVisualEntry Visuals { get; }

        public string Id => Definition?.Id;

        public GameObject PreviewPrefab => Visuals?.PreviewPrefab;

        public void ApplyDefaultMaterials(GameObject instance)
        {
            Visuals?.ApplyDefaultMaterials(instance);
        }
    }
}
