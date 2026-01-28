using System;
using Client.Building;
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
        [SerializeField] private string plotIdentifier;
        [SerializeField] private SerializableBounds bounds;
        [SerializeField] private float baseElevation;
        [SerializeField] private int materialLayerIndex;
        [SerializeField] private string ownerUserId;
        [SerializeField] private string plotSizeTemplateId;
        [SerializeField] private string buildZoneId;
        [SerializeField] private bool buildZoneValid = true;
        [SerializeField] private string buildZoneFailureReason;
        [SerializeField] private PlotPermissionEntry[] permissions;

        public BuildPlotDefinition()
        {
        }

        public BuildPlotDefinition(string plotId, Bounds worldBounds, float baseElevation, int materialLayerIndex)
        {
            this.plotId = string.IsNullOrWhiteSpace(plotId) ? Guid.NewGuid().ToString("N") : plotId.Trim();
            plotIdentifier = this.plotId;
            bounds = SerializableBounds.FromBounds(worldBounds);
            this.baseElevation = baseElevation;
            this.materialLayerIndex = Mathf.Max(0, materialLayerIndex);
            buildZoneValid = true;
        }

        public BuildPlotDefinition(BuildPlotDefinition source)
        {
            if (source == null)
            {
                plotId = Guid.NewGuid().ToString("N");
                plotIdentifier = plotId;
                bounds = new SerializableBounds(Vector3.zero, Vector3.zero);
                baseElevation = 0f;
                materialLayerIndex = 0;
                ownerUserId = null;
                plotSizeTemplateId = null;
                buildZoneId = null;
                buildZoneValid = true;
                buildZoneFailureReason = null;
                permissions = Array.Empty<PlotPermissionEntry>();
                return;
            }

            plotId = source.plotId;
            plotIdentifier = source.plotIdentifier;
            bounds = source.bounds;
            baseElevation = source.baseElevation;
            materialLayerIndex = source.materialLayerIndex;
            ownerUserId = source.ownerUserId;
            plotSizeTemplateId = source.plotSizeTemplateId;
            buildZoneId = source.buildZoneId;
            buildZoneValid = source.buildZoneValid;
            buildZoneFailureReason = source.buildZoneFailureReason;
            permissions = source.permissions == null ? Array.Empty<PlotPermissionEntry>() : (PlotPermissionEntry[])source.permissions.Clone();
        }

        public string PlotId => plotId;

        public string PlotIdentifier => string.IsNullOrWhiteSpace(plotIdentifier) ? plotId : plotIdentifier;

        public Bounds Bounds => bounds.ToBounds();

        public float BaseElevation => baseElevation;

        public int MaterialLayerIndex => materialLayerIndex;

        public string OwnerUserId => ownerUserId;

        public string PlotSizeTemplateId => plotSizeTemplateId;

        public string BuildZoneId => buildZoneId;

        public bool BuildZoneValid => buildZoneValid;

        public string BuildZoneFailureReason => buildZoneFailureReason;

        public PlotPermissionEntry[] Permissions => permissions ?? Array.Empty<PlotPermissionEntry>();

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

        public void SetPlotIdentifier(string identifier)
        {
            plotIdentifier = string.IsNullOrWhiteSpace(identifier) ? plotId : identifier.Trim();
        }

        public void SetOwnerUserId(string userId)
        {
            ownerUserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
        }

        public void SetPlotSizeTemplateId(string templateId)
        {
            plotSizeTemplateId = string.IsNullOrWhiteSpace(templateId) ? null : templateId.Trim();
        }

        public void SetBuildZoneCompliance(string zoneId, bool isValid, string failureReason = null)
        {
            buildZoneId = string.IsNullOrWhiteSpace(zoneId) ? null : zoneId.Trim();
            buildZoneValid = isValid;
            buildZoneFailureReason = string.IsNullOrWhiteSpace(failureReason) ? null : failureReason.Trim();
        }

        public void SetPermissions(PlotPermissionEntry[] entries)
        {
            permissions = entries == null ? Array.Empty<PlotPermissionEntry>() : (PlotPermissionEntry[])entries.Clone();
        }
    }
}
