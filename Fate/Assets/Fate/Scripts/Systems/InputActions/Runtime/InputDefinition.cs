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

        [Tooltip("Used when Kind is Axis.")]
        public GamepadAxis GamepadAxis = GamepadAxis.None;
    }
}
