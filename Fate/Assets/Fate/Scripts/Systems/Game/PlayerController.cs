using System;
using System.Collections.Generic;
using Fate.Systems.InputActions;
using Fate.Systems.Player;
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

        /// <summary>Same as <see cref="ActionPressed"/>/<see cref="ActionReleased"/> but also carries which device triggered it - for controllers (like the global controller) that listen to every device and need to tell them apart.</summary>
        public event Action<InputAction, InputDevice> ActionPressedForDevice;
        public event Action<InputAction, InputDevice> ActionReleasedForDevice;

        private readonly HashSet<Pawn> _globalPawns = new();

        public Pawn PossessedPawn { get; private set; }

        /// <summary>The profile this controller belongs to, or null for the global controller (which belongs to no single player).</summary>
        public PlayerId? OwnerPlayerId { get; private set; }

        /// <summary>True for the manager's single global controller (<see cref="OwnerPlayerId"/> is null).</summary>
        public bool IsGlobalController => !OwnerPlayerId.HasValue;

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

        /// <summary>Sets which profile (or none, for the global controller) and which device this controller is bound to. Called once by <see cref="PlayerControllerManager"/> right after spawning.</summary>
        public void Initialize(PlayerId? ownerPlayerId, InputDevice? targetDevice)
        {
            OwnerPlayerId = ownerPlayerId;
            TargetDevice = targetDevice;
        }

        /// <summary>
        /// Adds <paramref name="pawn"/> to the set of pawns this controller drives alongside any others,
        /// without taking the exclusive <see cref="PossessedPawn"/> slot used by <see cref="Possess"/>.
        /// Intended for the global controller driving every <see cref="Pawn.IsGlobal"/> pawn at once.
        /// </summary>
        public void PossessGlobalPawn(Pawn pawn)
        {
            if (!_globalPawns.Add(pawn))
                return;

            pawn.Possess(this);
        }

        /// <summary>Removes <paramref name="pawn"/> from the set added via <see cref="PossessGlobalPawn"/>.</summary>
        public void UnpossessGlobalPawn(Pawn pawn)
        {
            if (!_globalPawns.Remove(pawn))
                return;

            pawn.Unpossess();
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

        /// <summary>Whether this action is currently held on <paramref name="device"/> specifically, bypassing this controller's own <see cref="TargetDevice"/> filter. Always false for actions outside the active context.</summary>
        public bool GetButtonForDevice(InputAction action, InputDevice device)
        {
            if (inputManager == null || !IsInContext(action))
                return false;

            return inputManager.GetButton(action, device);
        }

        /// <summary>The current value of an axis action on <paramref name="device"/> specifically, bypassing this controller's own <see cref="TargetDevice"/> filter. Always 0 for actions outside the active context.</summary>
        public float GetAxisForDevice(InputAction action, InputDevice device)
        {
            if (inputManager == null || !IsInContext(action))
                return 0f;

            return inputManager.GetAxis(action, device);
        }

        private bool IsInContext(InputAction action) => activeContext != null && activeContext.Contains(action);

        private bool MatchesTargetDevice(InputDevice device) => !TargetDevice.HasValue || TargetDevice.Value == device;

        private void HandleActionPressed(InputAction action, InputDevice device)
        {
            if (!IsInContext(action) || !MatchesTargetDevice(device))
                return;

            ActionPressed?.Invoke(action);
            ActionPressedForDevice?.Invoke(action, device);
        }

        private void HandleActionReleased(InputAction action, InputDevice device)
        {
            if (!IsInContext(action) || !MatchesTargetDevice(device))
                return;

            ActionReleased?.Invoke(action);
            ActionReleasedForDevice?.Invoke(action, device);
        }
    }
}
