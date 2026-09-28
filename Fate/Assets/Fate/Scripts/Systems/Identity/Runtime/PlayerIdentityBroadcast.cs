using FishNet.Broadcast;

namespace Fate.Systems.Identity
{
    /// <summary>
    /// One local player's identity as sent over the wire. Raw strings rather than
    /// <see cref="PlayerId"/> directly, to sidestep any FishNet codegen edge cases with custom
    /// nested struct serialization (shape follows FishNet's own reference broadcast pattern in
    /// Assets/FishNet/Demos/Authenticator/Scripts/Broadcasts.cs).
    /// </summary>
    public struct LocalPlayerEntry
    {
        public string PlayerId;
        public string DisplayName;
    }

    /// <summary>
    /// Sent client -> server right after a connection is established (see
    /// <see cref="Fate.Systems.Lobby.LobbySession"/>), one entry per local player slot that
    /// client machine intends to bring into the party/lobby. Received by
    /// <see cref="PartyRosterService"/> and stashed on the connection for later use by the
    /// (not-yet-built) multi-object-per-connection spawner.
    /// </summary>
    public struct PlayerIdentityBroadcast : IBroadcast
    {
        public LocalPlayerEntry[] LocalPlayers;
    }
}
