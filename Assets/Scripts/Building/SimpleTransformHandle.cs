using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Building
{
    [DefaultExecutionOrder(-25)]
    public class SimpleTransformHandle : MonoBehaviour
    {
        [SerializeField] private float translationSpeed = 3f;
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float gizmoScale = 1.2f;

        private Transform _target;
        private bool _active;

        private readonly Color _xColor = new Color(1f, 0.2f, 0.2f, 0.85f);
        private readonly Color _yColor = new Color(0.2f, 1f, 0.2f, 0.85f);
        private readonly Color _zColor = new Color(0.2f, 0.6f, 1f, 0.85f);

        public bool IsActive => _active && _target != null;

        public void Attach(Transform target)
        {
            _target = target;
            _active = target != null;
            enabled = _active;
        }

        public void Detach()
        {
            _active = false;
            _target = null;
            enabled = false;
        }

        private void Update()
        {
            if (!IsActive)
            {
                return;
            }

            transform.position = _target.position;
            transform.rotation = Quaternion.identity;

            var translation = Vector3.zero;
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                translation += Vector3.forward;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                translation += Vector3.back;
            }

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                translation += Vector3.left;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                translation += Vector3.right;
            }

            if (keyboard.eKey.isPressed)
            {
                translation += Vector3.up;
            }

            if (keyboard.qKey.isPressed)
            {
                translation += Vector3.down;
            }
#else
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                translation += Vector3.forward;
            }

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                translation += Vector3.back;
            }

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                translation += Vector3.left;
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                translation += Vector3.right;
            }

            if (Input.GetKey(KeyCode.E))
            {
                translation += Vector3.up;
            }

            if (Input.GetKey(KeyCode.Q))
            {
                translation += Vector3.down;
            }
#endif

            if (translation.sqrMagnitude > 0f)
            {
                translation = Quaternion.LookRotation(Camera.main != null ? Camera.main.transform.forward : Vector3.forward,
                        Vector3.up) * translation;
                translation *= translationSpeed * Time.deltaTime;
                _target.position += translation;
            }

            var rotation = 0f;
#if ENABLE_INPUT_SYSTEM
            if (keyboard.zKey.isPressed)
            {
                rotation -= 1f;
            }

            if (keyboard.cKey.isPressed)
            {
                rotation += 1f;
            }
#else
            if (Input.GetKey(KeyCode.Z))
            {
                rotation -= 1f;
            }

            if (Input.GetKey(KeyCode.C))
            {
                rotation += 1f;
            }
#endif

            if (Mathf.Abs(rotation) > 0.01f)
            {
                _target.Rotate(Vector3.up, rotation * rotationSpeed * Time.deltaTime, Space.World);
            }
        }

        private void OnDrawGizmos()
        {
            if (!IsActive)
            {
                return;
            }

            Gizmos.color = _xColor;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.right * gizmoScale);
            Gizmos.color = _yColor;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * gizmoScale);
            Gizmos.color = _zColor;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.forward * gizmoScale);
        }
    }
}
