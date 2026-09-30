namespace Fate.Systems.InputActions
{
    /// <summary>
    /// A digital gamepad button, named by its physical position rather than a vendor's label
    /// (South = A/Cross, East = B/Circle, West = X/Square, North = Y/Triangle). Resolved to the
    /// legacy <see cref="UnityEngine.KeyCode"/> ordinal that actually fires for it on the current
    /// platform via <see cref="GamepadInputMap"/>.
    /// </summary>
    public enum GamepadButton
    {
        None,
        South,
        East,
        West,
        North,
        LeftBumper,
        RightBumper,
        LeftStickClick,
        RightStickClick,
        Select,
        Start,
    }
}
