namespace Fate.Systems.Game
{
    /// <summary>
    /// Which action map a given local player's <see cref="PlayerInputRig"/> currently has active.
    /// Deliberately just two states for now (see <see cref="LocalInputCoordinator"/>'s doc comment
    /// for why pause is scoped per-player instead of global) - expand later if more contexts
    /// (e.g. a dedicated inventory/map context) turn out to be needed.
    /// </summary>
    public enum InputContext
    {
        Gameplay,
        Menu,
    }
}
