namespace Fate.Systems.Game
{
    /// <summary>
    /// Implemented by any component on a <see cref="Pawn"/> that wants to bind to input actions.
    /// <see cref="Pawn"/> invokes these on every sibling component that implements it whenever a
    /// <see cref="PlayerController"/> takes or releases possession. Implementations bind directly
    /// to gameplay actions (e.g. "Jump") and never need to know about input contexts - the
    /// possessing controller has already filtered which actions it relays.
    /// </summary>
    public interface IPossessionHandler
    {
        void OnPossessed(PlayerController controller);

        void OnUnpossessed(PlayerController controller);
    }
}
