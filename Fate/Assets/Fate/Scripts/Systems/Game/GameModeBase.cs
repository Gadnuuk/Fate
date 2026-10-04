using System.Collections.Generic;
using Fate.Systems.Lobby;
using Fate.Systems.Player;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

namespace Fate.Systems.Game
{
    /// <summary>
    /// Server-only, scene-scoped pawn spawner - the Unreal <c>GameMode</c> equivalent. Exactly one
    /// instance exists per gameplay session (enforced by <see cref="Current"/>), created by the
    /// (not-yet-built) scene-load bootstrap only on the server (never on pure clients).
    ///
    /// Spawns one pawn per declared local player, for every connection currently known to
    /// <see cref="PartyRosterService"/> - both the ones already registered when this GameMode is
    /// created (<see cref="PartyRosterService.AllRosters"/>, covers the host's own loopback
    /// connection and any other early joiners) and any that register afterward
    /// (<see cref="PartyRosterService.RosterReceived"/>, covers late joins). Both paths funnel
    /// through the same <see cref="SpawnPawnsForRoster"/>, so there is no special-casing between
    /// "late" and "normal" joins.
    ///
    /// Deliberately a plain <see cref="MonoBehaviour"/>, not a <see cref="NetworkBehaviour"/> -
    /// this object itself is never networked, it only drives <c>ServerManager.Spawn</c> calls for
    /// the pawns it creates.
    /// </summary>
    public class GameModeBase : MonoBehaviour
    {
        [Tooltip("Used by GetPawnPrefab when a subclass doesn't override pawn selection. Must have a NetworkObject + PawnNetworkBridge (e.g. FatePawn_Default).")]
        [SerializeField]
        private NetworkObject defaultPawnPrefab;

        /// <summary>The single live instance for this session, or null if no GameMode currently exists.</summary>
        public static GameModeBase Current { get; private set; }

        /// <summary>Guards against spawning the same connection+player twice (e.g. a duplicate roster rebroadcast).</summary>
        private readonly HashSet<(NetworkConnection Connection, string PlayerIdUuid)> _spawnedPlayers = new();

        private void Awake()
        {
            if (Current != null && Current != this)
            {
                Debug.LogError($"{nameof(GameModeBase)}: duplicate instance - only one GameMode may exist per session.", this);
                Destroy(this);
                return;
            }

            Current = this;
        }

        private void OnDestroy()
        {
            if (Current == this)
                Current = null;
        }

        private void OnEnable()
        {
            if (PartyRosterService.Instance == null)
            {
                Debug.LogError($"{nameof(GameModeBase)}: no {nameof(PartyRosterService)} in the scene - cannot spawn pawns for any connection.", this);
                return;
            }

            // Catch up on every connection that registered its roster before this GameMode existed
            // (notably the host's own loopback connection), then stay subscribed for the rest of
            // this GameMode's lifetime so later joins (and disconnects) are handled the same way.
            foreach (KeyValuePair<NetworkConnection, PartyRosterService.ConnectionRoster> pair in PartyRosterService.Instance.AllRosters)
                SpawnPawnsForRoster(pair.Key, pair.Value);

            PartyRosterService.Instance.RosterReceived += OnRosterReceived;
            PartyRosterService.Instance.RosterRemoved += OnRosterRemoved;
        }

        private void OnDisable()
        {
            if (PartyRosterService.Instance == null)
                return;

            PartyRosterService.Instance.RosterReceived -= OnRosterReceived;
            PartyRosterService.Instance.RosterRemoved -= OnRosterRemoved;
        }

        private void OnRosterReceived(NetworkConnection conn, PartyRosterService.ConnectionRoster roster) =>
            SpawnPawnsForRoster(conn, roster);

        private void OnRosterRemoved(NetworkConnection conn)
        {
            // Pawns are despawned automatically by FishNet when their owning connection drops -
            // this just stops tracking them so a reconnect (treated as a brand-new join) isn't
            // blocked by a stale "already spawned" entry.
            _spawnedPlayers.RemoveWhere(entry => entry.Connection == conn);
        }

        /// <summary>
        /// Spawns one pawn per entry in <paramref name="roster"/> not already spawned for
        /// <paramref name="conn"/>, owned by <paramref name="conn"/> and pre-initialized with the
        /// player's identity so <see cref="PawnNetworkBridge"/> can resolve it on the owning client.
        /// </summary>
        private void SpawnPawnsForRoster(NetworkConnection conn, PartyRosterService.ConnectionRoster roster)
        {
            foreach (LocalPlayerEntry entry in roster.LocalPlayers)
            {
                if (!_spawnedPlayers.Add((conn, entry.PlayerId)))
                    continue;

                var playerId = new PlayerId { Uuid = entry.PlayerId, Tag = entry.DisplayName };

                NetworkObject pawnPrefab = GetPawnPrefab(entry);
                if (pawnPrefab == null)
                {
                    Debug.LogError($"{nameof(GameModeBase)}: no pawn prefab available for player '{playerId}' - cannot spawn.", this);
                    continue;
                }

                NetworkObject instance = Instantiate(pawnPrefab, GetSpawnPosition(entry), GetSpawnRotation(entry));

                var bridge = instance.GetComponent<PawnNetworkBridge>();
                if (bridge == null)
                {
                    Debug.LogError($"{nameof(GameModeBase)}: pawn prefab '{pawnPrefab.name}' has no {nameof(PawnNetworkBridge)} - destroying, cannot assign ownership.", this);
                    Destroy(instance.gameObject);
                    continue;
                }

                // Must happen before Spawn - the owner id travels down as part of the initial spawn payload.
                bridge.Initialize(playerId);

                InstanceFinder.ServerManager.Spawn(instance, conn, gameObject.scene);
            }
        }

        /// <summary>Which pawn prefab to spawn for <paramref name="entry"/>. Override to vary pawns per player/role; defaults to <see cref="defaultPawnPrefab"/>.</summary>
        protected virtual NetworkObject GetPawnPrefab(LocalPlayerEntry entry) => defaultPawnPrefab;

        /// <summary>Where to spawn the pawn for <paramref name="entry"/>. Defaults to the origin - override once spawn points exist.</summary>
        protected virtual Vector3 GetSpawnPosition(LocalPlayerEntry entry) => Vector3.zero;

        /// <summary>Which rotation to spawn the pawn with for <paramref name="entry"/>. Defaults to identity - override once spawn points exist.</summary>
        protected virtual Quaternion GetSpawnRotation(LocalPlayerEntry entry) => Quaternion.identity;
    }
}
