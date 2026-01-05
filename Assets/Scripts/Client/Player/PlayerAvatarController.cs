using System;
using UnityEngine;

namespace Client.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerAvatarController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float sprintSpeed = 6.5f;
        [SerializeField] private float acceleration = 14f;
        [SerializeField] private float rotationSpeed = 720f;
        [SerializeField] private float gravity = -18f;
        [SerializeField] private float jumpHeight = 1.2f;

        [Header("References")]
        [SerializeField] private Transform viewRoot;
        [SerializeField] private Transform cameraRoot;

        private CharacterController _controller;
        private Vector3 _velocity;
        private Vector3 _desiredMove;
        private bool _jumpQueued;
        private float _currentSpeed;

        public Transform ViewRoot => viewRoot != null ? viewRoot : transform;
        public Transform CameraRoot => cameraRoot != null ? cameraRoot : ViewRoot;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (viewRoot == null)
            {
                viewRoot = transform;
            }

            if (cameraRoot == null)
            {
                cameraRoot = viewRoot;
            }
        }

        private void Update()
        {
            ReadInput();
            UpdateMovement(Time.deltaTime);
        }

        private void ReadInput()
        {
            var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            input = Vector2.ClampMagnitude(input, 1f);

            var forward = ViewRoot.forward;
            var right = ViewRoot.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            _desiredMove = (forward * input.y + right * input.x).normalized;
            _jumpQueued = Input.GetButtonDown("Jump");
        }

        private void UpdateMovement(float deltaTime)
        {
            var wantsSprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var targetSpeed = wantsSprint ? sprintSpeed : walkSpeed;
            var targetVelocity = _desiredMove * targetSpeed;

            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetVelocity.magnitude, acceleration * deltaTime);
            var desiredVelocity = _desiredMove * _currentSpeed;

            var horizontal = new Vector3(desiredVelocity.x, 0f, desiredVelocity.z);
            _velocity.x = horizontal.x;
            _velocity.z = horizontal.z;

            if (_controller.isGrounded && _velocity.y < 0f)
            {
                _velocity.y = -2f;
            }

            if (_jumpQueued && _controller.isGrounded)
            {
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            _velocity.y += gravity * deltaTime;

            if (_desiredMove.sqrMagnitude > 0.01f)
            {
                var targetRotation = Quaternion.LookRotation(_desiredMove, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * deltaTime);
            }

            _controller.Move(_velocity * deltaTime);
        }
    }
}
