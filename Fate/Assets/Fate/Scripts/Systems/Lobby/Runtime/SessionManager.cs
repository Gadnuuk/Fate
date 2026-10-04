using System.Linq;
using Fate.Systems.Content;
using Fate.Systems.Game;
using Fate.Systems.Player;
using FishNet;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using UnityEngine;

namespace Fate.Systems.Lobby
{
    /// <summary>
    /// Lives alongside FishNet's NetworkManager in the Lobby content scene. Never connects
    /// on its own - lobby UI calls these explicitly once the player chooses to host or join.
    /// </summary>
    public class SessionManager : MonoBehaviour
    {
        [Tooltip("Content bundle to load for every connection once the lobby starts the game.")]
        [SerializeField] private ContentBundle gameplayBundle;

        [Tooltip("Spawned as the GameMode when gameplayBundle has no GameModePrefab of its own - a bare GameModeBase with no gameplay-specific overrides, so pawns still spawn for bundles that don't need a custom GameMode.")]
        [SerializeField] private GameModeBase defaultGameModePrefab;

        /// <summary>The bundle StartGame is currently loading, consumed by HandleLoadEnd once that load finishes server-side. Null whenever no gameplay load is in flight.</summary>
        private ContentBundle _pendingBundle;

        private void OnEnable()
        {
            InstanceFinder.SceneManager.OnLoadEnd += HandleLoadEnd;
        }

        private void OnDisable()
        {
            if (InstanceFinder.SceneManager == null)
                return;

            InstanceFinder.SceneManager.OnLoadEnd -= HandleLoadEnd;
        }

        public void StartHost()
        {
            InstanceFinder.ServerManager.StartConnection();
            InstanceFinder.ClientManager.StartConnection();
            InstanceFinder.ClientManager.OnClientConnectionState += OnClientConnectionState;
        }

        public void JoinHost(string address)
        {
            InstanceFinder.ClientManager.StartConnection(address);
            InstanceFinder.ClientManager.OnClientConnectionState += OnClientConnectionState;
        }

        private void OnClientConnectionState(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState != LocalConnectionState.Started)
                return;

            InstanceFinder.ClientManager.OnClientConnectionState -= OnClientConnectionState;
            SendLocalIdentity();
        }

        /// <summary>
        /// Announces every profile <see cref="ProfileManager"/> has logged in on this machine
        /// (see the main menu's login flow) to the server as this connection's "online presence"
        /// (see <see cref="PlayerIdentityBroadcast"/> / <see cref="PartyRosterService"/>). The
        /// (architecture-only) join lobby is what will eventually let a player pick which
        /// logged-in profile(s) to bring instead of sending all of them.
        /// </summary>
        private void SendLocalIdentity()
        {
            var localPlayers = ProfileManager.Instance.LoggedInProfiles
                .Select(p => new LocalPlayerEntry { PlayerId = p.PlayerId.Uuid, DisplayName = p.PlayerId.Tag })
                .ToArray();

            if (localPlayers.Length == 0)
            {
                Debug.LogWarning($"{nameof(SessionManager)}: no logged-in player profiles to announce - " +
                                  "log in via the main menu before connecting.", this);
                return;
            }

            InstanceFinder.ClientManager.Broadcast(new PlayerIdentityBroadcast { LocalPlayers = localPlayers });
        }

        public void LeaveLobby()
        {
            InstanceFinder.ClientManager.StopConnection();

            if (InstanceFinder.IsServerStarted)
                InstanceFinder.ServerManager.StopConnection(sendDisconnectMessage: true);
        }

        /// <summary>
        /// Host-only. Pushes every current (and future) connection into <see cref="gameplayBundle"/>'s
        /// scenes together. The Lobby content scene stays loaded underneath so the NetworkManager
        /// and the live connections survive the transition. Once the load finishes server-side,
        /// <see cref="HandleLoadEnd"/> spawns the GameMode that actually puts pawns into the world -
        /// see <see cref="GameModeBase"/>.
        /// </summary>
        public void StartGame()
        {
            if (!InstanceFinder.IsServerStarted)
            {
                Debug.LogWarning($"{nameof(StartGame)} can only be called by the host.", this);
                return;
            }

            if (gameplayBundle == null || gameplayBundle.BaseScene == null || !gameplayBundle.BaseScene.IsAssigned)
            {
                Debug.LogError($"{nameof(SessionManager)} has no gameplay bundle (or the bundle has no base scene) assigned.", this);
                return;
            }

            string[] sceneNames = new[] { gameplayBundle.BaseScene.SceneName }
                .Concat(gameplayBundle.Layers.Select(layer => layer.SceneName))
                .ToArray();

            _pendingBundle = gameplayBundle;

            var sceneLoadData = new SceneLoadData(sceneNames);
            InstanceFinder.SceneManager.LoadGlobalScenes(sceneLoadData);
        }

        /// <summary>
        /// Server-only. Fires for every global scene load/unload, not just <see cref="StartGame"/>'s -
        /// filters to the one it queued via <see cref="_pendingBundle"/> and ignores the rest (e.g. a
        /// future return-to-lobby load). Spawns exactly one <see cref="GameModeBase"/> - the
        /// bundle's own <see cref="ContentBundle.GameModePrefab"/> if it has one, otherwise
        /// <see cref="defaultGameModePrefab"/> - into the newly loaded base scene, so pawn spawning
        /// still happens for bundles that don't need a custom GameMode.
        /// </summary>
        private void HandleLoadEnd(SceneLoadEndEventArgs args)
        {
            if (!args.QueueData.AsServer || _pendingBundle == null)
                return;

            ContentBundle bundle = _pendingBundle;
            _pendingBundle = null;

            UnityEngine.SceneManagement.Scene gameplayScene = default;
            bool found = false;
            foreach (UnityEngine.SceneManagement.Scene loadedScene in args.LoadedScenes)
            {
                if (loadedScene.name != bundle.BaseScene.SceneName)
                    continue;

                gameplayScene = loadedScene;
                found = true;
                break;
            }

            if (!found)
            {
                Debug.LogError($"{nameof(SessionManager)}: base scene '{bundle.BaseScene.SceneName}' was not among the scenes just loaded - cannot spawn its GameMode.", this);
                return;
            }

            GameModeBase gameModePrefab = bundle.GameModePrefab != null ? bundle.GameModePrefab : defaultGameModePrefab;
            if (gameModePrefab == null)
            {
                Debug.LogError($"{nameof(SessionManager)}: bundle '{bundle.name}' has no GameModePrefab and no {nameof(defaultGameModePrefab)} is assigned - no pawns will spawn.", this);
                return;
            }

            GameModeBase gameMode = Instantiate(gameModePrefab);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gameMode.gameObject, gameplayScene);
        }
    }
}
