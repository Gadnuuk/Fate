namespace Fate.Systems.InputActions
{
    /// <summary>
    /// A continuous gamepad input, named semantically rather than by raw axis number. Resolved to
    /// one of the ten generic "Gamepad Axis N" entries in the project's legacy Input Manager
    /// (see <see cref="GamepadInputMap"/>) so it reads the same regardless of which/how many
    /// joysticks are plugged in.
    /// </summary>
    public enum GamepadAxis
    {
        None,
        LeftStickX,
        LeftStickY,
        RightStickX,
        RightStickY,
        LeftTrigger,
        RightTrigger,

        /// <summary>
        /// D-Pad horizontal. On this project's confirmed hardware the D-Pad is a POV hat reported
        /// as a genuine analog axis (not discrete joystick buttons) - see <see cref="GamepadButton.DPadLeft"/>/
        /// <see cref="GamepadButton.DPadRight"/>, which resolve through this axis instead of a KeyCode.
        /// </summary>
        DPadX,

        /// <summary>D-Pad vertical - see <see cref="DPadX"/>.</summary>
        DPadY,
    }
}
