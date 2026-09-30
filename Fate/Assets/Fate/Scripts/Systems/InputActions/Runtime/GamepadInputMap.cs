using System.Collections.Generic;
using UnityEngine;

namespace Fate.Systems.InputActions
{
    /// <summary>
    /// Resolves the normalized <see cref="GamepadButton"/> / <see cref="GamepadAxis"/> enums to
    /// the actual legacy-Input values that fire for them on a *specific* gamepad slot (1-4), so
    /// the rest of the input system never touches a raw joystick button ordinal or axis number.
    ///
    /// Legacy Unity Input has no real concept of a "normalized" gamepad - button ordinals and
    /// axis numbers for the same physical control differ by OS/driver/controller, and there is no
    /// way to query them generically. What this class provides instead:
    /// - Buttons: resolved to <see cref="KeyCode.Joystick1Button0"/>.."Joystick4Button19", which
    ///   fire only for that specific joystick slot (Unity lays these out in contiguous 20-button
    ///   blocks per joystick, so slot N's block starts at Joystick1Button0 + (N-1)*20). The
    ///   ordinal-to-face-button mapping below is the commonly documented Windows XInput layout
    ///   (A=0, B=1, X=2, Y=3, LB=4, RB=5, Back=6, Start=7, L3=8, R3=9). Verify against your actual
    ///   target controllers/platforms and adjust <see cref="ButtonOrdinalsByPlatform"/> if needed.
    ///   D-Pad ordinals (10-13) are an even rougher best-effort guess - on many drivers the D-Pad
    ///   is actually reported as a POV hat switch rather than discrete joystick buttons, in which
    ///   case it won't show up on any button ordinal at all and would need to be read as an axis
    ///   instead. Confirm with your actual hardware (log which <c>KeyCode.JoystickButton</c>, if
    ///   any, changes while pressing each D-Pad direction) before relying on them.
    /// - Axes: read through "Gamepad {slot} Axis {1..10}" entries pre-declared in
    ///   ProjectSettings/InputManager.asset (type: Joystick Axis, joystick: that specific slot),
    ///   so each device is read independently instead of blending every connected gamepad
    ///   together. Left stick (slots 1/2) is the well-established mapping already used by this
    ///   project's Horizontal/Vertical axes. Right stick and trigger slots (4/5/9/10) are
    ///   best-effort defaults - confirm them with your actual hardware (log
    ///   Input.GetAxisRaw("Gamepad 1 Axis N") for N in 1..10 while moving each stick/trigger) and
    ///   correct <see cref="AxisSlotsByPlatform"/> if they're off.
    /// </summary>
    public static class GamepadInputMap
    {
        public const int MaxGamepads = 4;
        private const int MaxAxisSlots = 10;
        private const int ButtonsPerJoystick = 20; // KeyCode.JoystickNButton0..19 block size.

        private struct AxisSlot
        {
            public int Slot; // 1-based index into the "Gamepad {slot} Axis N" Input Manager entries.
            public bool Invert;

            public AxisSlot(int slot, bool invert)
            {
                Slot = slot;
                Invert = invert;
            }
        }

        private static readonly Dictionary<RuntimePlatform, Dictionary<GamepadButton, int>> ButtonOrdinalsByPlatform = new()
        {
            [RuntimePlatform.WindowsPlayer] = WindowsButtonOrdinals(),
            [RuntimePlatform.WindowsEditor] = WindowsButtonOrdinals(),
        };

        private static readonly Dictionary<GamepadButton, int> DefaultButtonOrdinals = WindowsButtonOrdinals();

        private static readonly Dictionary<RuntimePlatform, Dictionary<GamepadAxis, AxisSlot>> AxisSlotsByPlatform = new()
        {
            [RuntimePlatform.WindowsPlayer] = WindowsAxisSlots(),
            [RuntimePlatform.WindowsEditor] = WindowsAxisSlots(),
        };

        private static readonly Dictionary<GamepadAxis, AxisSlot> DefaultAxisSlots = WindowsAxisSlots();

        private static Dictionary<GamepadButton, int> WindowsButtonOrdinals() => new()
        {
            [GamepadButton.South] = 0,
            [GamepadButton.East] = 1,
            [GamepadButton.West] = 2,
            [GamepadButton.North] = 3,
            [GamepadButton.LeftBumper] = 4,
            [GamepadButton.RightBumper] = 5,
            [GamepadButton.Select] = 6,
            [GamepadButton.Start] = 7,
            [GamepadButton.LeftStickClick] = 8,
            [GamepadButton.RightStickClick] = 9,
            [GamepadButton.DPadUp] = 10,
            [GamepadButton.DPadDown] = 11,
            [GamepadButton.DPadLeft] = 12,
            [GamepadButton.DPadRight] = 13,
        };

        private static Dictionary<GamepadAxis, AxisSlot> WindowsAxisSlots() => new()
        {
            [GamepadAxis.LeftStickX] = new AxisSlot(1, invert: false),
            [GamepadAxis.LeftStickY] = new AxisSlot(2, invert: true),
            [GamepadAxis.RightStickX] = new AxisSlot(4, invert: false),
            [GamepadAxis.RightStickY] = new AxisSlot(5, invert: true),
            [GamepadAxis.LeftTrigger] = new AxisSlot(9, invert: false),
            [GamepadAxis.RightTrigger] = new AxisSlot(10, invert: false),
        };

        /// <summary>Returns the <see cref="KeyCode"/> that fires when this button is held on gamepad slot <paramref name="gamepadIndex"/> (1-4).</summary>
        public static KeyCode GetButtonKeyCode(GamepadButton button, int gamepadIndex)
        {
            if (button == GamepadButton.None || gamepadIndex < 1 || gamepadIndex > MaxGamepads)
                return KeyCode.None;

            Dictionary<GamepadButton, int> table = ButtonOrdinalsByPlatform.GetValueOrDefault(Application.platform, DefaultButtonOrdinals);
            if (!table.TryGetValue(button, out int ordinal))
                return KeyCode.None;

            int offset = (gamepadIndex - 1) * ButtonsPerJoystick + ordinal;
            return KeyCode.Joystick1Button0 + offset;
        }

        /// <summary>Returns the Input Manager axis name to read this axis from on gamepad slot <paramref name="gamepadIndex"/> (1-4), and whether its raw value should be negated.</summary>
        public static string GetAxisName(GamepadAxis axis, int gamepadIndex, out bool invert)
        {
            invert = false;
            if (axis == GamepadAxis.None || gamepadIndex < 1 || gamepadIndex > MaxGamepads)
                return null;

            Dictionary<GamepadAxis, AxisSlot> table = AxisSlotsByPlatform.GetValueOrDefault(Application.platform, DefaultAxisSlots);
            if (!table.TryGetValue(axis, out AxisSlot slot))
                return null;

            invert = slot.Invert;
            return $"Gamepad {gamepadIndex} Axis {Mathf.Clamp(slot.Slot, 1, MaxAxisSlots)}";
        }
    }
}
