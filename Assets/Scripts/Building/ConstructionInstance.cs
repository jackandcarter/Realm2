using System;
using Client.Save;
using UnityEngine;

namespace Building
{
    [DisallowMultipleComponent]
    public class ConstructionInstance : MonoBehaviour
    {
        [SerializeField] private string instanceId;
        [SerializeField] private string blueprintId;
        [SerializeField] private bool isPlaced;
        [SerializeField] private string prefabId;

        private Rigidbody _rigidbody;

        public string InstanceId => instanceId;
        public string BlueprintId => blueprintId;
        public bool IsPlaced => isPlaced;
        public string PrefabId => prefabId;

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

        public void Initialize(string blueprint, bool placed = false)
        {
            blueprintId = blueprint;
            if (!string.IsNullOrWhiteSpace(blueprint))
            {
                prefabId = blueprint;
            }
            isPlaced = placed;
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
            isPlaced = true;
            Persist();
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
                InstanceId = instanceId,
                BlueprintId = blueprintId,
                IsPlaced = isPlaced,
                PrefabId = string.IsNullOrWhiteSpace(prefabId) ? blueprintId : prefabId,
                Position = transform.position,
                Rotation = transform.rotation,
                Scale = transform.localScale
            };
        }

        public void ApplyState(SerializableConstructionState state)
        {
            instanceId = state.InstanceId;
            blueprintId = state.BlueprintId;
            isPlaced = state.IsPlaced;
            prefabId = string.IsNullOrWhiteSpace(state.PrefabId) ? state.BlueprintId : state.PrefabId;
            transform.SetPositionAndRotation(state.Position, state.Rotation);
            transform.localScale = state.Scale;
            ArkitectRegistry.RegisterConstructionInstance(this);
        }

        [Serializable]
        public struct SerializableConstructionState
        {
            public string InstanceId;
            public string BlueprintId;
            public bool IsPlaced;
            public string PrefabId;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
        }
    }
}
