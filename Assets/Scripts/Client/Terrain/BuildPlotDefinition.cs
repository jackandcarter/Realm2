using System;
using UnityEngine;

namespace Client.Terrain
{
    [Serializable]
    public struct SerializableBounds
    {
        [SerializeField] private Vector3 center;
        [SerializeField] private Vector3 size;

        public SerializableBounds(Vector3 center, Vector3 size)
        {
            this.center = center;
            this.size = size;
        }

        public Vector3 Center
        {
            readonly get => center;
            set => center = value;
        }

        public Vector3 Size
        {
            readonly get => size;
            set => size = value;
        }

        public readonly Bounds ToBounds()
        {
            return new Bounds(center, size);
        }

        public static SerializableBounds FromBounds(Bounds bounds)
        {
            return new SerializableBounds(bounds.center, bounds.size);
        }
    }

    [Serializable]
    public class BuildPlotDefinition
    {
        [SerializeField] private string plotId;
        [SerializeField] private SerializableBounds bounds;
        [SerializeField] private float baseElevation;
        [SerializeField] private int materialLayerIndex;

        public BuildPlotDefinition()
        {
        }

        public BuildPlotDefinition(string plotId, Bounds worldBounds, float baseElevation, int materialLayerIndex)
        {
            this.plotId = string.IsNullOrWhiteSpace(plotId) ? Guid.NewGuid().ToString("N") : plotId.Trim();
            bounds = SerializableBounds.FromBounds(worldBounds);
            this.baseElevation = baseElevation;
            this.materialLayerIndex = Mathf.Max(0, materialLayerIndex);
        }

        public BuildPlotDefinition(BuildPlotDefinition source)
        {
            if (source == null)
            {
                plotId = Guid.NewGuid().ToString("N");
                bounds = new SerializableBounds(Vector3.zero, Vector3.zero);
                baseElevation = 0f;
                materialLayerIndex = 0;
                return;
            }

            plotId = source.plotId;
            bounds = source.bounds;
            baseElevation = source.baseElevation;
            materialLayerIndex = source.materialLayerIndex;
        }

        public string PlotId => plotId;

        public Bounds Bounds => bounds.ToBounds();

        public float BaseElevation => baseElevation;

        public int MaterialLayerIndex => materialLayerIndex;

        public SerializableBounds BoundsData => bounds;

        public void SetBaseElevation(float elevation)
        {
            baseElevation = elevation;
        }

        public void SetMaterialLayerIndex(int index)
        {
            materialLayerIndex = Mathf.Max(0, index);
        }

        public void SetBounds(Bounds worldBounds)
        {
            bounds = SerializableBounds.FromBounds(worldBounds);
        }
    }
}
