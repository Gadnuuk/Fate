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
    }
}
