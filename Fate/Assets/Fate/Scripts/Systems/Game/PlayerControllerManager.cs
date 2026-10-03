using System.Collections.Generic;
using Fate.Systems.InputActions;
using Fate.Systems.Player;
using Fate.Systems.Utilities;
using UnityEngine;

namespace Fate.Systems.Game
{
    /// <summary>
    /// Central hub for <see cref="PlayerController"/> lifecycle. Owns a single persistent "global"
    /// controller that listens to every device (see <see cref="GlobalController"/>) and is never
    /// despawned, plus whichever per-player controllers are currently configured via
    /// <see cref="SpawnController"/>/<see cref="ApplyConfiguration"/>.
    ///
    /// Persistent singleton (see <see cref="PersistentSingleton{T}"/>), same pattern as
    /// <see cref="ProfileManager"/> - controllers spawned here survive scene loads.
    /// </summary>
    public class PlayerControllerManager : PersistentSingleton<PlayerControllerManager>
    {
        [SerializeField]
        private PlayerController playerControllerPrefab;

        [Tooltip("Assigned to the global controller's ActiveContext right after it's spawned. Per-player controllers keep whatever ActiveContext is already configured on playerControllerPrefab.")]
        [SerializeField]
        private InputContext globalControllerContext;

        private PlayerController _globalController;
        private readonly List<PlayerController> _activeControllers = new();

        /// <summary>The single controller that listens to every device regardless of player, used to drive every <see cref="Pawn.IsGlobal"/> pawn. Spawned on first access.</summary>
        public PlayerController GlobalController
        {
            get
            {
                if (_globalController == null)
                {
                    _globalController = SpawnControllerInternal(ownerPlayerId: null, targetDevice: null);
                    _globalController.ActiveContext = globalControllerContext;
                }

                return _globalController;
            }
        }

        /// <summary>Every currently spawned per-player controller (excludes <see cref="GlobalController"/>).</summary>
        public IReadOnlyList<PlayerController> ActiveControllers => _activeControllers;

        /// <summary>Spawns a <see cref="PlayerController"/> bound to <paramref name="playerId"/> and <paramref name="device"/>.</summary>
        public PlayerController SpawnController(PlayerId playerId, InputDevice device)
        {
            PlayerController controller = SpawnControllerInternal(playerId, device);
            _activeControllers.Add(controller);
            return controller;
        }

        /// <summary>
        /// Despawns every current per-player controller and spawns a fresh one per assignment.
        /// The global controller is never touched.
        /// </summary>
        // TODO: once pawn possession bridges to networked characters, this needs FishNet
        // ownership/RPC handling - today everything here is local-client-only.
        public void ApplyConfiguration(IReadOnlyList<PlayerControllerAssignment> assignments)
        {
            DespawnActiveControllers();

            foreach (PlayerControllerAssignment assignment in assignments)
                SpawnController(assignment.PlayerId, assignment.Device);
        }

        /// <summary>Destroys every current per-player controller. The global controller is never touched.</summary>
        public void DespawnActiveControllers()
        {
            foreach (PlayerController controller in _activeControllers)
            {
                if (controller != null)
                    Destroy(controller.gameObject);
            }

            _activeControllers.Clear();
        }

        private PlayerController SpawnControllerInternal(PlayerId? ownerPlayerId, InputDevice? targetDevice)
        {
            PlayerController controller = Instantiate(playerControllerPrefab, transform);
            controller.Initialize(ownerPlayerId, targetDevice);
            return controller;
        }
    }
}
