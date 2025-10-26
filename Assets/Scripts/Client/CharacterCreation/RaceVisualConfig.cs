using System;
using System.Collections.Generic;
using UnityEngine;

namespace Client.CharacterCreation
{
    [CreateAssetMenu(menuName = "Character Creation/Race Visual Config", fileName = "RaceVisualConfig")]
    public class RaceVisualConfig : ScriptableObject
    {
        [SerializeField]
        private List<RaceVisualEntry> raceVisuals = new();

        private Dictionary<string, RaceVisualEntry> _lookup;

        public IReadOnlyList<RaceVisualEntry> RaceVisuals => raceVisuals;

        public bool TryGetVisualForRace(string raceId, out RaceVisualEntry entry)
        {
            if (string.IsNullOrWhiteSpace(raceId))
            {
                entry = null;
                return false;
            }

            EnsureLookup();
            if (_lookup == null)
            {
                entry = null;
                return false;
            }

            return _lookup.TryGetValue(raceId, out entry);
        }

        private void EnsureLookup()
        {
            if (_lookup != null)
            {
                return;
            }

            _lookup = new Dictionary<string, RaceVisualEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var visual in raceVisuals)
            {
                if (visual == null || string.IsNullOrWhiteSpace(visual.RaceId))
                {
                    continue;
                }

                _lookup[visual.RaceId] = visual;
            }
        }

        private void OnValidate()
        {
            _lookup = null;
            EnsureLookup();
        }
    }

    [Serializable]
    public class RaceVisualEntry
    {
        [SerializeField] private string raceId;
        [SerializeField] private GameObject previewPrefab;
        [SerializeField] private List<RaceMaterialVariant> materialVariants = new();
        [SerializeField] private int defaultVariantIndex;

        public string RaceId => raceId;
        public GameObject PreviewPrefab => previewPrefab;
        public IReadOnlyList<RaceMaterialVariant> MaterialVariants => materialVariants;
        public int DefaultVariantIndex => defaultVariantIndex;

        public RaceMaterialVariant GetVariant(int index)
        {
            if (materialVariants == null || materialVariants.Count == 0)
            {
                return null;
            }

            if (index < 0 || index >= materialVariants.Count)
            {
                index = Mathf.Clamp(defaultVariantIndex, 0, materialVariants.Count - 1);
            }

            return materialVariants[index];
        }

        public void ApplyDefaultMaterials(GameObject instance)
        {
            ApplyVariantMaterials(instance, defaultVariantIndex);
        }

        public void ApplyVariantMaterials(GameObject instance, int variantIndex)
        {
            if (instance == null)
            {
                return;
            }

            var variant = GetVariant(variantIndex);
            if (variant == null || variant.Materials == null || variant.Materials.Count == 0)
            {
                return;
            }

            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            foreach (var renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                var original = renderer.sharedMaterials;
                if (original == null || original.Length == 0)
                {
                    continue;
                }

                var applied = new Material[original.Length];
                for (var i = 0; i < applied.Length; i++)
                {
                    applied[i] = i < variant.Materials.Count && variant.Materials[i] != null
                        ? variant.Materials[i]
                        : original[i];
                }

                renderer.sharedMaterials = applied;
            }
        }
    }

    [Serializable]
    public class RaceMaterialVariant
    {
        [SerializeField] private string variantId;
        [SerializeField] private Material[] materials = Array.Empty<Material>();

        public string VariantId => variantId;
        public IReadOnlyList<Material> Materials => materials;
    }
}
