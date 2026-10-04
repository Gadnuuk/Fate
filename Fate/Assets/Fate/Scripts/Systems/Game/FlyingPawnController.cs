using Fate.Systems.InputActions;
using FishNet.Object;
using UnityEngine;

namespace Fate.Systems.Game
{
    /// <summary>
    /// Simple networked flying movement controller for testing: reads move/look input from its
    /// possessing <see cref="PlayerController"/> and flies the <see cref="CharacterController"/>
    /// on this GameObject like a free-fly scene camera. Look input rotates this transform
    /// directly (yaw + pitch) rather than a separate camera - <see cref="PawnCameraComponent"/>'s
    /// view anchor follows along since it's a child of this transform. Only the owning client
    /// simulates movement; every other client sees it via this object's FishNet NetworkTransform.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FlyingPawnController : NetworkBehaviour, IPossessionHandler
    {
        [SerializeField]
        private InputAction moveHorizontalAction;

        [SerializeField]
        private InputAction moveVerticalAction;

        [SerializeField]
        private InputAction lookHorizontalAction;

        [SerializeField]
        private InputAction lookVerticalAction;

        [SerializeField]
        private float moveSpeed = 6f;

        [Tooltip("Degrees per second of rotation at full axis deflection (right stick or mouse).")]
        [SerializeField]
        private float lookSpeed = 120f;

        [SerializeField]
        private float minPitch = -89f;

        [SerializeField]
        private float maxPitch = 89f;

        private CharacterController _characterController;
        private PlayerController _controller;
        private float _yaw;
        private float _pitch;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            Vector3 angles = transform.eulerAngles;
            _yaw = angles.y;
            _pitch = angles.x;
        }

        public void OnPossessed(PlayerController controller)
        {
            _controller = controller;
        }

        public void OnUnpossessed(PlayerController controller)
        {
            _controller = null;
        }

        private void Update()
        {
            if (!IsOwner || _controller == null)
                return;

            ApplyLook();
            ApplyMove();
        }

        private void ApplyLook()
        {
            // Mouse is read as part of the Keyboard device (see InputManager), so this already
            // picks up whichever of right-stick/mouse is actually driving the axis.
            float lookX = _controller.GetAxis(lookHorizontalAction) * lookSpeed * Time.deltaTime;
            float lookY = _controller.GetAxis(lookVerticalAction) * lookSpeed * Time.deltaTime;

            _yaw += lookX;
            _pitch = Mathf.Clamp(_pitch - lookY, minPitch, maxPitch);

            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void ApplyMove()
        {
            float horizontal = _controller.GetAxis(moveHorizontalAction);
            float vertical = _controller.GetAxis(moveVerticalAction);

            Vector3 direction = transform.right * horizontal + transform.forward * vertical;
            _characterController.Move(direction * moveSpeed * Time.deltaTime);
        }
    }
}
