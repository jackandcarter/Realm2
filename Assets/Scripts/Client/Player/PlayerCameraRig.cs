using UnityEngine;

namespace Client.Player
{
    public class PlayerCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);
        [SerializeField] private float distance = 4.5f;
        [SerializeField] private float minPitch = -40f;
        [SerializeField] private float maxPitch = 70f;
        [SerializeField] private float lookSensitivity = 120f;
        [SerializeField] private float followSmoothing = 12f;

        private float _yaw;
        private float _pitch;
        private Vector3 _currentVelocity;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var mouseX = Input.GetAxis("Mouse X");
            var mouseY = Input.GetAxis("Mouse Y");
            _yaw += mouseX * lookSensitivity * Time.deltaTime;
            _pitch -= mouseY * lookSensitivity * Time.deltaTime;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            var targetPosition = target.position + pivotOffset;
            var desiredPosition = targetPosition - rotation * Vector3.forward * distance;

            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _currentVelocity, 1f / followSmoothing);
            transform.rotation = rotation;
        }
    }
}
