using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Building
{
    [DefaultExecutionOrder(-40)]
    public class FloatModeController : MonoBehaviour
    {
        [FormerlySerializedAs("transformHandle")]
        [SerializeField] private SimpleTransformHandle floatTransformHandle;

        private ConstructionInstance _activeInstance;
        private bool _isFloating;

        public event Action<ConstructionInstance> FloatModeStarted;
        public event Action<ConstructionInstance> FloatModeEnded;
        public event Action<ConstructionInstance> PlacementFinalized;

        public ConstructionInstance ActiveInstance => _activeInstance;
        public bool IsFloating => _isFloating;

        private void Awake()
        {
            if (floatTransformHandle == null)
            {
                var go = new GameObject("FloatTransformHandle");
                go.hideFlags = HideFlags.HideInHierarchy;
                floatTransformHandle = go.AddComponent<SimpleTransformHandle>();
                DontDestroyOnLoad(go);
            }
        }

        private void OnDestroy()
        {
            if (floatTransformHandle != null && floatTransformHandle.gameObject != null)
            {
                Destroy(floatTransformHandle.gameObject);
            }
        }

        public void BeginFloat(ConstructionInstance instance)
        {
            if (instance == null)
            {
                return;
            }

            _activeInstance = instance;
            _isFloating = true;

            PrepareInstanceForFloat(instance);
            floatTransformHandle.Attach(instance.transform);
            FloatModeStarted?.Invoke(instance);
        }

        public void CancelFloat()
        {
            if (!_isFloating)
            {
                return;
            }

            _isFloating = false;
            floatTransformHandle.Detach();
            FloatModeEnded?.Invoke(_activeInstance);
        }

        public void FinalizePlacement()
        {
            if (_activeInstance == null)
            {
                return;
            }

            var instance = _activeInstance;
            ApplyPlacementConstraints(instance);
            instance.MarkPlaced();
            PlacementFinalized?.Invoke(instance);
            CancelFloat();
        }

        private static void PrepareInstanceForFloat(ConstructionInstance instance)
        {
            if (instance == null)
            {
                return;
            }

            var rigidbody = instance.GetOrCreateRigidbody();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
            rigidbody.constraints = RigidbodyConstraints.None;
        }

        private static void ApplyPlacementConstraints(ConstructionInstance instance)
        {
            if (instance == null)
            {
                return;
            }

            instance.Persist();
            var rigidbody = instance.GetOrCreateRigidbody();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
            rigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }
    }
}
