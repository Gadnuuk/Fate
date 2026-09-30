using System;
using UnityEngine;

namespace Fate.Systems.InputActions
{
    /// <summary>
    /// The actual binding behind an <see cref="InputAction"/>: which keyboard key and which
    /// normalized gamepad control drive it, and whether it should be read as a button or an axis.
    /// </summary>
    [Serializable]
    public class InputDefinition
    {
        public InputKind Kind = InputKind.Button;

        [Header("Keyboard")]
        [Tooltip("Used when Kind is Button.")]
        public KeyCode KeyboardKey = KeyCode.None;

        [Tooltip("Used when Kind is Axis - held key drives the axis toward +1.")]
        public KeyCode KeyboardPositive = KeyCode.None;

        [Tooltip("Used when Kind is Axis - held key drives the axis toward -1.")]
        public KeyCode KeyboardNegative = KeyCode.None;

        [Header("Gamepad (normalized across vendors/platforms/any connected controller)")]
        [Tooltip("Used when Kind is Button.")]
        public GamepadButton GamepadButton = GamepadButton.None;

        [Tooltip("Used when Kind is Axis. Combines additively with GamepadPositive/GamepadNegative below if those are also set.")]
        public GamepadAxis GamepadAxis = GamepadAxis.None;

        [Tooltip("Used when Kind is Axis - held button drives the axis toward +1. Lets an axis be built from two digital buttons (e.g. D-Pad Right) instead of/in addition to an analog GamepadAxis.")]
        public GamepadButton GamepadPositive = GamepadButton.None;

        [Tooltip("Used when Kind is Axis - held button drives the axis toward -1. Lets an axis be built from two digital buttons (e.g. D-Pad Left) instead of/in addition to an analog GamepadAxis.")]
        public GamepadButton GamepadNegative = GamepadButton.None;
    }
}
