using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Transporting;
using UnityEngine;

namespace Fate.Systems.Lobby
{
    /// <summary>
    /// Server-side. Maintains the full, cumulative set of every connection's declared local
    /// players (sent via <see cref="PlayerIdentityBroadcast"/>, see the Lobby content scene's
    /// session/connection manager) for as long as that connection stays connected, and notifies
    /// listeners as rosters arrive or their connection drops.
    ///
    /// This is deliberately just bookkeeping - it does not spawn anything. The (not-yet-built)
    /// server-side game mode is what reads <see cref="AllRosters"/>/<see cref="RosterReceived"/>
    /// to know how many pawns a given connection should get and with which identities, including
    /// late joins (a roster that arrives after the game mode already exists) and the host's own
    /// loopback connection (whose roster typically arrives before any game mode exists, hence
    /// <see cref="AllRosters"/> for catch-up on top of the live event). See
    /// Assets/Fate/Docs/SplitScreenNetworkedPlayers.md, Phase 4.
    ///
    /// Lives alongside the Lobby content scene's session/connection manager.
    /// </summary>
    public class PartyRosterService : MonoBehaviour
    {
        /// <summary>Every local player a connection declared, keyed by that connection.</summary>
        public class ConnectionRoster
        {
            public readonly List<LocalPlayerEntry> LocalPlayers = new();
        }

        private static PartyRosterService instance;

        /// <summary>The live instance, for code (e.g. the not-yet-built server-side game mode) that needs to read/subscribe outside this scene.</summary>
        public static PartyRosterService Instance => instance;

        private readonly Dictionary<NetworkConnection, ConnectionRoster> _rosters = new();

        /// <summary>Every roster received so far, for anything spawned after some connections have already registered (e.g. a game mode created partway through a session).</summary>
        public IReadOnlyDictionary<NetworkConnection, ConnectionRoster> AllRosters => _rosters;

        /// <summary>Raised whenever a connection's roster is (re)received.</summary>
        public event Action<NetworkConnection, ConnectionRoster> RosterReceived;

        /// <summary>Raised when a connection that had a roster disconnects.</summary>
        public event Action<NetworkConnection> RosterRemoved;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogError($"{nameof(PartyRosterService)}: duplicate instance in the scene.", this);
                Destroy(this);
                return;
            }

            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void OnEnable()
        {
            InstanceFinder.ServerManager.RegisterBroadcast<PlayerIdentityBroadcast>(OnPlayerIdentityBroadcast, requireAuthentication: false);
            InstanceFinder.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        }

        private void OnDisable()
        {
            if (InstanceFinder.ServerManager == null)
                return;

            InstanceFinder.ServerManager.UnregisterBroadcast<PlayerIdentityBroadcast>(OnPlayerIdentityBroadcast);
            InstanceFinder.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
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
            _rosters[conn] = roster;

            Debug.Log($"{nameof(PartyRosterService)}: connection {conn.ClientId} declared {roster.LocalPlayers.Count} local player(s): " +
                      string.Join(", ", roster.LocalPlayers.ConvertAll(p => $"{p.DisplayName} ({p.PlayerId})")));

            RosterReceived?.Invoke(conn, roster);
        }

        private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState != RemoteConnectionState.Stopped)
                return;

            if (_rosters.Remove(conn))
                RosterRemoved?.Invoke(conn);
        }

        /// <summary>Read-only lookup for a single connection's roster, if it has one.</summary>
        public static bool TryGetRoster(NetworkConnection conn, out ConnectionRoster roster)
        {
            roster = null;
            return instance != null && conn != null && instance._rosters.TryGetValue(conn, out roster);
        }
    }
}
