namespace Fate.Systems.InputActions
{
    /// <summary>
    /// A physical mouse axis. Read as part of <see cref="InputDevice.Keyboard"/> rather than a
    /// device of its own - keyboard and mouse are the same local input source.
    /// </summary>
    public enum MouseAxis
    {
        None,
        X,
        Y,
    }
}
