using System;
using System.Collections.Generic;
using Client.Save;
using UnityEngine;

namespace Building
{
    public enum ConstructionPlacementState
    {
        Preview,
        Pending,
        Placed,
        Failed
    }

    [Serializable]
    public struct ConstructionMaterialCost
    {
        public string ItemId;
        public int Quantity;
    }

    [DisallowMultipleComponent]
    public class ConstructionInstance : MonoBehaviour
    {
        [SerializeField] private string instanceId;
        [SerializeField] private string blueprintId;
        [SerializeField] private string plotId;
        [SerializeField] private bool isPlaced;
        [SerializeField] private string prefabId;
        [SerializeField] private ConstructionPlacementState placementState = ConstructionPlacementState.Preview;
        [SerializeField] private ConstructionMaterialCost[] materialCostSnapshot;

        private Rigidbody _rigidbody;

        public string InstanceId => instanceId;
        public string ConstructionId => instanceId;
        public string BlueprintId => blueprintId;
        public string PlotId => plotId;
        public bool IsPlaced => isPlaced;
        public string PrefabId => prefabId;
        public ConstructionPlacementState PlacementState => placementState;
        public IReadOnlyList<ConstructionMaterialCost> MaterialCostSnapshot =>
            materialCostSnapshot ?? Array.Empty<ConstructionMaterialCost>();

        private void Awake()
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }
        }

        private void OnEnable()
        {
            EnsureInstanceId();
            ArkitectRegistry.RegisterConstructionInstance(this);
        }

        private void OnDisable()
        {
            ArkitectRegistry.UnregisterConstructionInstance(this);
        }

        public void Initialize(
            string blueprint,
            bool placed = false,
            string assignedPlotId = null,
            ConstructionPlacementState? stateOverride = null)
        {
            blueprintId = blueprint;
            if (!string.IsNullOrWhiteSpace(blueprint))
            {
                prefabId = blueprint;
            }
            if (!string.IsNullOrWhiteSpace(assignedPlotId))
            {
                plotId = assignedPlotId.Trim();
            }
            placementState = stateOverride ?? (placed ? ConstructionPlacementState.Placed : ConstructionPlacementState.Preview);
            isPlaced = placed;
            if (placementState == ConstructionPlacementState.Placed)
            {
                isPlaced = true;
            }
            EnsureInstanceId();
            ArkitectRegistry.RegisterConstructionInstance(this);
        }

        public void EnsureInstanceId()
        {
            if (!string.IsNullOrWhiteSpace(instanceId))
            {
                return;
            }

            instanceId = Guid.NewGuid().ToString("N");
        }

        public void MarkPlaced()
        {
            placementState = ConstructionPlacementState.Placed;
            isPlaced = true;
            Persist();
        }

        public void AssignPlotId(string assignedPlotId)
        {
            plotId = string.IsNullOrWhiteSpace(assignedPlotId) ? null : assignedPlotId.Trim();
        }

        public void SetPlacementState(ConstructionPlacementState state)
        {
            placementState = state;
            if (placementState == ConstructionPlacementState.Placed)
            {
                isPlaced = true;
            }
        }

        public void SetMaterialCostSnapshot(ConstructionMaterialCost[] snapshot)
        {
            materialCostSnapshot = snapshot ?? Array.Empty<ConstructionMaterialCost>();
        }

        public void Persist()
        {
            ConstructionSaveSystem.RecordInstance(this);
        }

        public void RefreshRigidbodyCache()
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }
        }

        public Rigidbody GetOrCreateRigidbody()
        {
            RefreshRigidbodyCache();
            if (_rigidbody == null)
            {
                _rigidbody = gameObject.AddComponent<Rigidbody>();
                _rigidbody.useGravity = false;
                _rigidbody.isKinematic = true;
            }

            return _rigidbody;
        }

        public SerializableConstructionState CaptureState()
        {
            EnsureInstanceId();
            return new SerializableConstructionState
            {
                ConstructionId = instanceId,
                InstanceId = instanceId,
                BlueprintId = blueprintId,
                PlotId = plotId,
                IsPlaced = isPlaced,
                PrefabId = string.IsNullOrWhiteSpace(prefabId) ? blueprintId : prefabId,
                PlacementState = placementState,
                MaterialCostSnapshot = materialCostSnapshot ?? Array.Empty<ConstructionMaterialCost>(),
                Position = transform.position,
                Rotation = transform.rotation,
                Scale = transform.localScale
            };
        }

        public void ApplyState(SerializableConstructionState state)
        {
            instanceId = string.IsNullOrWhiteSpace(state.ConstructionId) ? state.InstanceId : state.ConstructionId;
            blueprintId = state.BlueprintId;
            plotId = state.PlotId;
            placementState = state.PlacementState;
            if (placementState == ConstructionPlacementState.Preview && state.IsPlaced)
            {
                placementState = ConstructionPlacementState.Placed;
            }
            isPlaced = state.IsPlaced || placementState == ConstructionPlacementState.Placed;
            prefabId = string.IsNullOrWhiteSpace(state.PrefabId) ? state.BlueprintId : state.PrefabId;
            materialCostSnapshot = state.MaterialCostSnapshot ?? Array.Empty<ConstructionMaterialCost>();
            transform.SetPositionAndRotation(state.Position, state.Rotation);
            transform.localScale = state.Scale;
            ArkitectRegistry.RegisterConstructionInstance(this);
        }

        [Serializable]
        public struct SerializableConstructionState
        {
            public string ConstructionId;
            public string InstanceId;
            public string BlueprintId;
            public string PlotId;
            public bool IsPlaced;
            public string PrefabId;
            public ConstructionPlacementState PlacementState;
            public ConstructionMaterialCost[] MaterialCostSnapshot;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
        }
    }
}
