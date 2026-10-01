using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Transporting;
using UnityEngine;

namespace Fate.Systems.Lobby
{
    /// <summary>
    /// Server-side. Stashes each connection's declared local players (sent via
    /// <see cref="PlayerIdentityBroadcast"/>, see <see cref="LobbySession"/>)
    /// on that connection's own <see cref="NetworkConnection.CustomData"/> as a
    /// <see cref="ConnectionRoster"/>.
    ///
    /// This is deliberately just bookkeeping - it does not spawn anything. The (architecture-only,
    /// not yet built) multi-object-per-connection spawner is what will read
    /// <see cref="TryGetRoster"/> to know how many <c>FatePlayerNetworked Variant</c> instances a
    /// given connection should get and with which identities. See
    /// Assets/Fate/Docs/SplitScreenNetworkedPlayers.md, Phase 4.
    ///
    /// Lives alongside <see cref="LobbySession"/> in the Lobby content scene.
    /// </summary>
    public class PartyRosterService : MonoBehaviour
    {
        /// <summary>Every local player a connection declared, keyed by that connection.</summary>
        public class ConnectionRoster
        {
            public readonly List<LocalPlayerEntry> LocalPlayers = new();
        }

        private void OnEnable()
        {
            InstanceFinder.ServerManager.RegisterBroadcast<PlayerIdentityBroadcast>(OnPlayerIdentityBroadcast, requireAuthentication: false);
        }

        private void OnDisable()
        {
            if (InstanceFinder.ServerManager != null)
                InstanceFinder.ServerManager.UnregisterBroadcast<PlayerIdentityBroadcast>(OnPlayerIdentityBroadcast);
        }

        private void OnPlayerIdentityBroadcast(NetworkConnection conn, PlayerIdentityBroadcast broadcast, Channel channel)
        {
            if (broadcast.LocalPlayers == null || broadcast.LocalPlayers.Length == 0)
            {
                Debug.LogWarning($"{nameof(PartyRosterService)}: received an empty {nameof(PlayerIdentityBroadcast)} from connection {conn.ClientId}.");
                return;
            }

            var roster = new ConnectionRoster();
            roster.LocalPlayers.AddRange(broadcast.LocalPlayers);
            conn.CustomData = roster;

            Debug.Log($"{nameof(PartyRosterService)}: connection {conn.ClientId} declared {roster.LocalPlayers.Count} local player(s): " +
                      string.Join(", ", roster.LocalPlayers.ConvertAll(p => $"{p.DisplayName} ({p.PlayerId})")));
        }

        /// <summary>Read-only lookup for whoever needs the roster next (e.g. the future spawner).</summary>
        public static bool TryGetRoster(NetworkConnection conn, out ConnectionRoster roster)
        {
            roster = conn?.CustomData as ConnectionRoster;
            return roster != null;
        }
    }
}
