using Fate.Systems.Player;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace Fate.Systems.Game
{
    /// <summary>
    /// Networking-only glue between a server-spawned <see cref="Pawn"/> and the owning client's
    /// local <see cref="PlayerController"/>. Lives as a sibling component on the same prefab as
    /// <see cref="Pawn"/> rather than on <see cref="Pawn"/> itself, so <see cref="Pawn"/> and
    /// <see cref="PlayerController"/> stay entirely unaware of FishNet - this component is the
    /// only thing that knows a pawn is networked at all.
    ///
    /// Server flow (driven by the not-yet-built server-side game mode): instantiate the pawn
    /// prefab, call <see cref="Initialize"/> with the owning <see cref="PlayerId"/> *before*
    /// spawning it, then <c>ServerManager.Spawn(instance, connection)</c> - the id travels down
    /// as part of the initial spawn payload, which is why it has to be set pre-spawn rather than
    /// via an RPC afterward.
    ///
    /// Owning-client flow: once spawned, resolves the synced <see cref="PlayerId"/> to whichever
    /// local <see cref="PlayerController"/> was already created for it (see
    /// <see cref="PlayerControllerManager.TryGetController"/> - today that controller must exist
    /// before hosting/joining, see Assets/Fate/Docs/SplitScreenNetworkedPlayers.md) and possesses
    /// this pawn with it. Non-owning clients never resolve/possess anything.
    /// </summary>
    [RequireComponent(typeof(Pawn))]
    public class PawnNetworkBridge : NetworkBehaviour
    {
        private readonly SyncVar<string> _ownerPlayerIdUuid = new();

        private Pawn _pawn;
        private PlayerController _possessingController;

        private void Awake()
        {
            _pawn = GetComponent<Pawn>();
        }

        /// <summary>
        /// Server-only. Must be called after instantiating the pawn but before <c>ServerManager.Spawn</c>
        /// so the id is included in the initial spawn payload. Uses <c>UseIsStarted</c> rather than the
        /// default per-object server-initialized check, since this object isn't spawned yet when this runs.
        /// </summary>
        [Server(UseIsStarted = true)]
        public void Initialize(PlayerId ownerPlayerId)
        {
            _ownerPlayerIdUuid.Value = ownerPlayerId.Uuid;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!IsOwner)
                return;

            var ownerPlayerId = new PlayerId { Uuid = _ownerPlayerIdUuid.Value };
            if (!PlayerControllerManager.Instance.TryGetController(ownerPlayerId, out PlayerController controller))
            {
                Debug.LogError($"{nameof(PawnNetworkBridge)}: owning client has no local {nameof(PlayerController)} for player id " +
                                $"'{_ownerPlayerIdUuid.Value}' - a controller must be created for every local player before hosting/joining.", this);
                return;
            }

            _possessingController = controller;
            controller.Possess(_pawn);
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            if (_possessingController == null)
                return;

            if (_possessingController.PossessedPawn == _pawn)
                _possessingController.Unpossess();

            _possessingController = null;
        }
    }
}
