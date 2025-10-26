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

        private Rigidbody _rigidbody;

        public string InstanceId => instanceId;
        public string BlueprintId => blueprintId;
        public bool IsPlaced => isPlaced;

        private void Awake()
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }
        }

        public void Initialize(string blueprint, bool placed = false)
        {
            blueprintId = blueprint;
            isPlaced = placed;
            EnsureInstanceId();
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
            return new SerializableConstructionState
            {
                InstanceId = instanceId,
                BlueprintId = blueprintId,
                IsPlaced = isPlaced,
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
            transform.SetPositionAndRotation(state.Position, state.Rotation);
            transform.localScale = state.Scale;
        }

        [Serializable]
        public struct SerializableConstructionState
        {
            public string InstanceId;
            public string BlueprintId;
            public bool IsPlaced;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
        }
    }
}
