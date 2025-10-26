using System;
using UnityEngine;

namespace Building
{
    [DefaultExecutionOrder(-40)]
    public class FloatModeController : MonoBehaviour
    {
        [SerializeField] private SimpleTransformHandle transformHandle;

        private ConstructionInstance _activeInstance;
        private bool _isFloating;

        public event Action<ConstructionInstance> FloatModeStarted;
        public event Action<ConstructionInstance> FloatModeEnded;
        public event Action<ConstructionInstance> PlacementFinalized;

        public ConstructionInstance ActiveInstance => _activeInstance;
        public bool IsFloating => _isFloating;

        private void Awake()
        {
            if (transformHandle == null)
            {
                var go = new GameObject("FloatTransformHandle");
                go.hideFlags = HideFlags.HideInHierarchy;
                transformHandle = go.AddComponent<SimpleTransformHandle>();
                DontDestroyOnLoad(go);
            }
        }

        private void OnDestroy()
        {
            if (transformHandle != null && transformHandle.gameObject != null)
            {
                Destroy(transformHandle.gameObject);
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
            transformHandle.Attach(instance.transform);
            FloatModeStarted?.Invoke(instance);
        }

        public void CancelFloat()
        {
            if (!_isFloating)
            {
                return;
            }

            _isFloating = false;
            transformHandle.Detach();
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
