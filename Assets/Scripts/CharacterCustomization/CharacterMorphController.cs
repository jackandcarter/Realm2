using System;
using System.Collections.Generic;
using UnityEngine;

namespace Realm.CharacterCustomization
{
    /// <summary>
    /// Allows artists and designers to tweak limb and torso proportions directly in the Unity Editor.
    /// Attach this component to the root object of a character prefab and assign transforms for each region you want to scale.
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterMorphController : MonoBehaviour
    {
        [Serializable]
        public class BodyRegion
        {
            [Tooltip("Label shown in the inspector for this region.")]
            public string label;

            [Tooltip("Transform whose local scale will be manipulated by the sliders.")]
            public Transform target;

            [Tooltip("Minimum and maximum height multiplier applied to the Y axis.")]
            public Vector2 heightRange = new Vector2(0.6f, 1.6f);

            [Tooltip("Minimum and maximum width multiplier applied to the X and Z axes.")]
            public Vector2 widthRange = new Vector2(0.6f, 1.6f);

            [Range(0f, 1f)]
            public float heightNormalized = 0.5f;

            [Range(0f, 1f)]
            public float widthNormalized = 0.5f;

            [HideInInspector]
            public Vector3 cachedScale = Vector3.one;

            [HideInInspector]
            public bool hasCachedScale;

            public void CacheDefaults()
            {
                if (target == null)
                {
                    return;
                }

                if (!hasCachedScale)
                {
                    cachedScale = target.localScale;
                    hasCachedScale = true;
                }
            }

            public void ForceRecache()
            {
                if (target == null)
                {
                    hasCachedScale = false;
                    return;
                }

                cachedScale = target.localScale;
                hasCachedScale = true;
            }

            public void Apply()
            {
                if (target == null)
                {
                    return;
                }

                CacheDefaults();

                var scale = cachedScale;
                var heightMultiplier = Mathf.Lerp(heightRange.x, heightRange.y, heightNormalized);
                var widthMultiplier = Mathf.Lerp(widthRange.x, widthRange.y, widthNormalized);
                scale.y = cachedScale.y * heightMultiplier;
                scale.x = cachedScale.x * widthMultiplier;
                scale.z = cachedScale.z * widthMultiplier;
                target.localScale = scale;
            }
        }

        [SerializeField]
        private List<BodyRegion> regions = new List<BodyRegion>();

        public IReadOnlyList<BodyRegion> Regions => regions;

        public void ApplyAll()
        {
            foreach (var region in regions)
            {
                region?.Apply();
            }
        }

        public void CaptureDefaultsFromScene()
        {
            foreach (var region in regions)
            {
                region?.ForceRecache();
            }
        }

        private void OnValidate()
        {
            ApplyAll();
        }

        private void Reset()
        {
            CaptureDefaultsFromScene();
            ApplyAll();
        }
    }
}
