using FishNet.Broadcast;

namespace Fate.Systems.Lobby
{
    /// <summary>
    /// One local player's identity as sent over the wire. Raw strings rather than
    /// <see cref="Fate.Systems.Player.PlayerId"/> directly, to sidestep any FishNet codegen edge
    /// cases with custom nested struct serialization (shape follows FishNet's own reference
    /// broadcast pattern in Assets/FishNet/Demos/Authenticator/Scripts/Broadcasts.cs).
    /// </summary>
    public struct LocalPlayerEntry
    {
        public string PlayerId;
        public string DisplayName;
    }

    /// <summary>
    /// Sent client -> server right after a connection is established (see
    /// <see cref="SessionManager"/>), one entry per local player profile that client machine's
    /// <see cref="Fate.Systems.Player.ProfileManager"/> has logged in and intends to bring into
    /// the party/lobby. Received by <see cref="PartyRosterService"/> and kept per-connection for
    /// later use by <see cref="Fate.Systems.Game.GameModeBase"/>, which spawns one pawn per entry.
    /// </summary>
    public struct PlayerIdentityBroadcast : IBroadcast
    {
        public LocalPlayerEntry[] LocalPlayers;
    }
}
