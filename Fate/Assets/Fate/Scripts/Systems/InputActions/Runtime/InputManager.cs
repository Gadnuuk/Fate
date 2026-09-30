using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fate.Systems.InputActions
{
    /// <summary>
    /// Listens to legacy Unity input (<see cref="Input"/>) for every binding declared in its
    /// <see cref="InputConfig"/>, and every frame updates each bound <see cref="InputAction"/>'s
    /// current button/axis state *per <see cref="InputDevice"/>* (keyboard, and gamepad slots
    /// 1-4 independently), firing <see cref="ActionPressed"/>/<see cref="ActionReleased"/> with
    /// the device that triggered the transition. Knows nothing about contexts, possession, or
    /// which device a player cares about - <c>PlayerController</c> filters that.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        private static readonly InputDevice[] AllDevices =
        {
            InputDevice.Keyboard, InputDevice.Gamepad1, InputDevice.Gamepad2, InputDevice.Gamepad3, InputDevice.Gamepad4,
        };

        [SerializeField]
        private InputConfig config;

        public event Action<InputAction, InputDevice> ActionPressed;
        public event Action<InputAction, InputDevice> ActionReleased;

        private readonly Dictionary<(InputAction, InputDevice), bool> _buttonStates = new();
        private readonly Dictionary<(InputAction, InputDevice), float> _axisValues = new();

        public InputConfig Config
        {
            get => config;
            set => config = value;
        }

        private void Update()
        {
            if (config == null)
                return;

            foreach (InputBinding binding in config.Bindings)
            {
                if (binding.Action == null)
                    continue;

                if (binding.Definition.Kind == InputKind.Button)
                    UpdateButtonAllDevices(binding.Action, binding.Definition);
                else
                    UpdateAxisAllDevices(binding.Action, binding.Definition);
            }
        }

        /// <summary>Whether this action is held. If <paramref name="device"/> is null, returns true if it's held on any device.</summary>
        public bool GetButton(InputAction action, InputDevice? device = null)
        {
            if (device.HasValue)
                return _buttonStates.GetValueOrDefault((action, device.Value), false);

            foreach (InputDevice candidate in AllDevices)
            {
                if (_buttonStates.GetValueOrDefault((action, candidate), false))
                    return true;
            }

            return false;
        }

        /// <summary>The current value (-1..1) of an axis action. If <paramref name="device"/> is null, returns the value from whichever device is driven furthest from zero.</summary>
        public float GetAxis(InputAction action, InputDevice? device = null)
        {
            if (device.HasValue)
                return _axisValues.GetValueOrDefault((action, device.Value), 0f);

            float best = 0f;
            foreach (InputDevice candidate in AllDevices)
            {
                float value = _axisValues.GetValueOrDefault((action, candidate), 0f);
                if (Mathf.Abs(value) > Mathf.Abs(best))
                    best = value;
            }

            return best;
        }

        private void UpdateButtonAllDevices(InputAction action, InputDefinition definition)
        {
            UpdateButton(action, InputDevice.Keyboard, ReadKeyboardButton(definition));

            for (int gamepadIndex = 1; gamepadIndex <= GamepadInputMap.MaxGamepads; gamepadIndex++)
                UpdateButton(action, IndexToGamepadDevice(gamepadIndex), ReadGamepadButton(definition, gamepadIndex));
        }

        private void UpdateAxisAllDevices(InputAction action, InputDefinition definition)
        {
            _axisValues[(action, InputDevice.Keyboard)] = ReadKeyboardAxis(definition);

            for (int gamepadIndex = 1; gamepadIndex <= GamepadInputMap.MaxGamepads; gamepadIndex++)
                _axisValues[(action, IndexToGamepadDevice(gamepadIndex))] = ReadGamepadAxis(definition, gamepadIndex);
        }

        private void UpdateButton(InputAction action, InputDevice device, bool held)
        {
            var key = (action, device);
            bool wasHeld = _buttonStates.GetValueOrDefault(key, false);
            _buttonStates[key] = held;

            if (held && !wasHeld)
                ActionPressed?.Invoke(action, device);
            else if (!held && wasHeld)
                ActionReleased?.Invoke(action, device);
        }

        private static bool ReadKeyboardButton(InputDefinition definition) =>
            definition.KeyboardKey != KeyCode.None && Input.GetKey(definition.KeyboardKey);

        private static bool ReadGamepadButton(InputDefinition definition, int gamepadIndex)
        {
            KeyCode keyCode = GamepadInputMap.GetButtonKeyCode(definition.GamepadButton, gamepadIndex);
            return keyCode != KeyCode.None && Input.GetKey(keyCode);
        }

        private static float ReadKeyboardAxis(InputDefinition definition)
        {
            float value = 0f;
            if (definition.KeyboardPositive != KeyCode.None && Input.GetKey(definition.KeyboardPositive))
                value += 1f;
            if (definition.KeyboardNegative != KeyCode.None && Input.GetKey(definition.KeyboardNegative))
                value -= 1f;

            return value;
        }

        private static float ReadGamepadAxis(InputDefinition definition, int gamepadIndex)
        {
            string axisName = GamepadInputMap.GetAxisName(definition.GamepadAxis, gamepadIndex, out bool invert);
            if (string.IsNullOrEmpty(axisName))
                return 0f;

            float value = Input.GetAxis(axisName);
            return invert ? -value : value;
        }

        private static InputDevice IndexToGamepadDevice(int gamepadIndex) => gamepadIndex switch
        {
            1 => InputDevice.Gamepad1,
            2 => InputDevice.Gamepad2,
            3 => InputDevice.Gamepad3,
            4 => InputDevice.Gamepad4,
            _ => throw new ArgumentOutOfRangeException(nameof(gamepadIndex), gamepadIndex, "Only gamepad slots 1-4 are supported."),
        };
    }
}
