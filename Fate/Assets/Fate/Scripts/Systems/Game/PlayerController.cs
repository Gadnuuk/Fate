using System;
using Fate.Systems.InputActions;
using UnityEngine;

namespace Fate.Systems.Game
{
    /// <summary>
    /// Spawned for each player. Reads the shared <see cref="InputManager"/> and filters it
    /// through this player's active <see cref="InputContext"/>, relaying only the actions that
    /// pass that filter (as events, and as queryable button/axis state) to whichever
    /// <see cref="Pawn"/> it currently possesses.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [SerializeField]
        private InputManager inputManager;

        [SerializeField]
        private InputContext activeContext;

        [Tooltip("When enabled, this controller only relays/queries actions driven by targetDevice - input from every other device is ignored.")]
        [SerializeField]
        private bool filterByDevice;

        [SerializeField]
        private InputDevice targetDevice = InputDevice.Keyboard;

        public event Action<InputAction> ActionPressed;
        public event Action<InputAction> ActionReleased;

        public Pawn PossessedPawn { get; private set; }

        public InputContext ActiveContext
        {
            get => activeContext;
            set => activeContext = value;
        }

        /// <summary>The single device this controller listens to, or null to accept input from any device.</summary>
        public InputDevice? TargetDevice
        {
            get => filterByDevice ? targetDevice : null;
            set
            {
                filterByDevice = value.HasValue;
                if (value.HasValue)
                    targetDevice = value.Value;
            }
        }

        private void Awake()
        {
            if (inputManager == null)
                inputManager = FindFirstObjectByType<InputManager>();
        }

        private void OnEnable()
        {
            if (inputManager == null)
                return;

            inputManager.ActionPressed += HandleActionPressed;
            inputManager.ActionReleased += HandleActionReleased;
        }

        private void OnDisable()
        {
            if (inputManager == null)
                return;

            inputManager.ActionPressed -= HandleActionPressed;
            inputManager.ActionReleased -= HandleActionReleased;
        }

        /// <summary>Takes possession of <paramref name="pawn"/>, releasing whatever this controller currently possesses first.</summary>
        public void Possess(Pawn pawn)
        {
            if (PossessedPawn == pawn)
                return;

            Unpossess();

            PossessedPawn = pawn;
            pawn.Possess(this);
        }

        /// <summary>Releases the currently possessed pawn, if any.</summary>
        public void Unpossess()
        {
            if (PossessedPawn == null)
                return;

            Pawn pawn = PossessedPawn;
            PossessedPawn = null;
            pawn.Unpossess();
        }

        /// <summary>Whether this action is currently held. Always false for actions outside the active context.</summary>
        public bool GetButton(InputAction action)
        {
            if (inputManager == null || !IsInContext(action))
                return false;

            return inputManager.GetButton(action, TargetDevice);
        }

        /// <summary>The current value of an axis action. Always 0 for actions outside the active context.</summary>
        public float GetAxis(InputAction action)
        {
            if (inputManager == null || !IsInContext(action))
                return 0f;

            return inputManager.GetAxis(action, TargetDevice);
        }

        private bool IsInContext(InputAction action) => activeContext != null && activeContext.Contains(action);

        private bool MatchesTargetDevice(InputDevice device) => !TargetDevice.HasValue || TargetDevice.Value == device;

        private void HandleActionPressed(InputAction action, InputDevice device)
        {
            if (IsInContext(action) && MatchesTargetDevice(device))
                ActionPressed?.Invoke(action);
        }

        private void HandleActionReleased(InputAction action, InputDevice device)
        {
            if (IsInContext(action) && MatchesTargetDevice(device))
                ActionReleased?.Invoke(action);
        }
    }
}
