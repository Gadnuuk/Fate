using System;
using System.Collections.Generic;
using Fate.Systems.Game.Generated;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Fate.Systems.Game
{
    /// <summary>
    /// Owns one local player slot's input: a single instance of the generated
    /// <see cref="GameInputActions"/> wrapper, scoped to only the device(s) that belong to that
    /// slot, plus the active <see cref="InputContext"/> for that slot only.
    ///
    /// Deliberately plain device-scoping (<see cref="InputActionAsset.devices"/>) rather than
    /// <c>PlayerInput</c>/<c>InputUser</c> - this project has no per-user rebindable-controls
    /// requirement yet, and the simpler approach is enough to stop local players from fighting
    /// over the same keyboard/gamepad state (see <see cref="PlayerCameraRig"/>'s doc comment for
    /// the equivalent problem/fix on the camera side).
    ///
    /// <see cref="Initialize"/> is called once, right after slot assignment, by
    /// <see cref="PlayerCameraRig.ApplyLocalSlot"/> - the one place that already owns "what does
    /// this instance get once we know it's ours and which slot it is."
    ///
    /// Pause is split across two actions so a paused slot can always unpause itself without any
    /// other slot's input being touched: <see cref="PausePressed"/> fires from the gameplay-only
    /// <c>Player/Pause</c> action, <see cref="MenuCancelPressed"/> fires from the menu-only
    /// <c>UI/Cancel</c> action (already bound to Escape/gamepad East by the template). Only one of
    /// the two maps is ever enabled at a time (see <see cref="SetContext"/>), so there's no risk of
    /// both firing for the same key press.
    /// </summary>
    public class PlayerInputRig : MonoBehaviour
    {
        private GameInputActions _actions;
        private InputContext _context = InputContext.Gameplay;

        public int SlotIndex { get; private set; } = -1;
        public InputContext Context => _context;

        /// <summary>Raised when this slot's own Pause action fires (Gameplay context only).</summary>
        public event Action PausePressed;

        /// <summary>Raised when this slot's own UI-Cancel action fires (Menu context only).</summary>
        public event Action MenuCancelPressed;

        private void Awake()
        {
            _actions = new GameInputActions();
            _actions.Player.Pause.performed += OnPausePerformed;
            _actions.UI.Cancel.performed += OnMenuCancelPerformed;
        }

        private void OnDestroy()
        {
            _actions.Player.Pause.performed -= OnPausePerformed;
            _actions.UI.Cancel.performed -= OnMenuCancelPerformed;
            _actions.Dispose();
        }

        private void OnPausePerformed(InputAction.CallbackContext ctx) => PausePressed?.Invoke();
        private void OnMenuCancelPerformed(InputAction.CallbackContext ctx) => MenuCancelPressed?.Invoke();

        /// <summary>
        /// Restricts this rig's entire action asset to whichever physical device(s) belong to
        /// <paramref name="slotIndex"/> and enables the Gameplay context.
        ///
        /// Temporary manual mapping (mirrors <see cref="PlayerCameraRig.localSlotIndex"/>'s own
        /// "hand-set for now" note) - slot 0 gets keyboard + mouse plus the first gamepad (if any),
        /// so a single-player/no-gamepad session works out of the box; slots 1-3 each get exactly
        /// one gamepad. The future join-lobby device-pairing UI (see
        /// Assets/Fate/Docs/SplitScreenNetworkedPlayers.md, Phase 3) replaces this with an explicit
        /// per-slot device pick instead of this positional guess.
        /// </summary>
        public void Initialize(int slotIndex)
        {
            SlotIndex = slotIndex;

            var devices = new List<InputDevice>();
            if (slotIndex == 0)
            {
                if (Keyboard.current != null)
                    devices.Add(Keyboard.current);
                if (Mouse.current != null)
                    devices.Add(Mouse.current);
                if (Gamepad.all.Count > 0)
                    devices.Add(Gamepad.all[0]);
            }
            else if (slotIndex < Gamepad.all.Count)
            {
                devices.Add(Gamepad.all[slotIndex]);
            }
            else
            {
                Debug.LogWarning($"{nameof(PlayerInputRig)}: no gamepad available for slot {slotIndex} - " +
                                  "this rig will have no bound devices until one is connected.", this);
            }

            _actions.devices = devices.ToArray();
            SetContext(InputContext.Gameplay);
        }

        /// <summary>
        /// Swaps which action map is enabled for this rig only - never touches any other slot's
        /// rig. See <see cref="LocalInputCoordinator"/> for the scene-level per-slot pause wiring.
        /// </summary>
        public void SetContext(InputContext context)
        {
            _context = context;

            switch (context)
            {
                case InputContext.Gameplay:
                    _actions.UI.Disable();
                    _actions.Player.Enable();
                    break;
                case InputContext.Menu:
                    _actions.Player.Disable();
                    _actions.UI.Enable();
                    break;
            }
        }

        // -- Typed accessors for the player-prefab scripts (Part 6) --

        public Vector2 Move => _actions.Player.Move.ReadValue<Vector2>();
        public Vector2 Look => _actions.Player.Look.ReadValue<Vector2>();
        public bool SprintHeld => _actions.Player.Sprint.IsPressed();
        public bool CrouchPressed => _actions.Player.Crouch.WasPressedThisFrame();
        public bool JumpPressed => _actions.Player.Jump.WasPressedThisFrame();

        /// <summary>Edge-triggered - matches the kit's original single-shot GetMouseButtonDown check.</summary>
        public bool AttackPressed => _actions.Player.Attack.WasPressedThisFrame();

        /// <summary>Held - matches the kit's original full-auto GetMouseButton check.</summary>
        public bool AttackHeld => _actions.Player.Attack.IsPressed();

        public bool InteractPressed => _actions.Player.Interact.WasPressedThisFrame();

        // Both aim actions are toggles in the kit (each press flips an aim state), so these are
        // edge-triggered rather than held, matching the original GetMouseButtonDown checks.
        public bool AimViewPressed => _actions.Player.AimView.WasPressedThisFrame();
        public bool AimSightPressed => _actions.Player.AimSight.WasPressedThisFrame();

        public bool ViewChangePressed => _actions.Player.ViewChange.WasPressedThisFrame();
        public float Lean => _actions.Player.Lean.ReadValue<float>();
        public bool WeaponSlot1Pressed => _actions.Player.WeaponSlot1.WasPressedThisFrame();
        public bool WeaponSlot2Pressed => _actions.Player.WeaponSlot2.WasPressedThisFrame();
        public bool WeaponSlot3Pressed => _actions.Player.WeaponSlot3.WasPressedThisFrame();
        public bool WeaponSlot4Pressed => _actions.Player.WeaponSlot4.WasPressedThisFrame();
    }
}
