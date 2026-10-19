using System.Collections;
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
        [Tooltip("Spawned as the GameMode when a bundle passed to StartGame has no GameModePrefab of its own - a bare GameModeBase with no gameplay-specific overrides, so pawns still spawn for bundles that don't need a custom GameMode.")]
        [SerializeField] private GameModeBase defaultGameModePrefab;

        private static SessionManager instance;

        /// <summary>The live instance, for UI (e.g. <see cref="Fate.Systems.Lobby.LobbyUIController"/>) that drives hosting/joining/starting the game.</summary>
        public static SessionManager Instance => instance;

        /// <summary>The bundle StartGame is currently loading, consumed by HandleLoadEnd once that load finishes server-side. Null whenever no gameplay load is in flight.</summary>
        private ContentBundle _pendingBundle;

        /// <summary>The bundle most recently requested via StartGame, so a later call knows what to unload first. Null until StartGame has been called at least once this session.</summary>
        private ContentBundle _currentBundle;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogError($"{nameof(SessionManager)}: duplicate instance in the scene.", this);
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
            bool serverStarted = InstanceFinder.ServerManager.StartConnection();
            bool clientStarted = InstanceFinder.ClientManager.StartConnection();
            Debug.Log($"{nameof(SessionManager)}: {nameof(StartHost)} - server start requested: {serverStarted}, loopback client start requested: {clientStarted}.", this);
            InstanceFinder.ClientManager.OnClientConnectionState += OnClientConnectionState;
        }

        public void JoinHost(string address)
        {
            InstanceFinder.ClientManager.StartConnection(address);
            InstanceFinder.ClientManager.OnClientConnectionState += OnClientConnectionState;
        }

        private void OnClientConnectionState(ClientConnectionStateArgs args)
        {
            Debug.Log($"{nameof(SessionManager)}: client connection state changed to {args.ConnectionState}.", this);

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
        /// Host-only. Pushes every current (and future) connection into <paramref name="bundle"/>'s
        /// scenes together. <see cref="SessionManager"/> itself lives in the persistent
        /// runtime_systems scene (not the lobby content scene), so it and the live connections
        /// survive the transition regardless; the lobby content scene each peer has loaded locally
        /// via <see cref="ContentSceneManager"/> is torn down separately, by
        /// <see cref="HandleLoadEnd"/>. Once the load finishes server-side, <see cref="HandleLoadEnd"/>
        /// also spawns the GameMode that actually puts pawns into the world - see
        /// <see cref="GameModeBase"/>.
        ///
        /// Callable more than once per session (e.g. the lobby's Start Game button, or later a
        /// level-transition trigger in gameplay) - if a bundle is already loaded, its scenes are
        /// unloaded first so everyone moves into <paramref name="bundle"/> together rather than
        /// stacking scenes on top of each other.
        /// </summary>
        public void StartGame(ContentBundle bundle)
        {
            if (!InstanceFinder.IsServerStarted)
            {
                Debug.LogWarning($"{nameof(StartGame)} can only be called by the host.", this);
                return;
            }

            if (bundle == null || bundle.BaseScene == null || !bundle.BaseScene.IsAssigned)
            {
                Debug.LogError($"{nameof(SessionManager)}: {nameof(StartGame)} was passed no bundle (or a bundle with no base scene).", this);
                return;
            }

            if (_currentBundle != null)
            {
                string[] previousSceneNames = new[] { _currentBundle.BaseScene.SceneName }
                    .Concat(_currentBundle.Layers.Select(layer => layer.SceneName))
                    .ToArray();

                InstanceFinder.SceneManager.UnloadGlobalScenes(new SceneUnloadData(previousSceneNames));
            }

            string[] sceneNames = new[] { bundle.BaseScene.SceneName }
                .Concat(bundle.Layers.Select(layer => layer.SceneName))
                .ToArray();

            _pendingBundle = bundle;
            _currentBundle = bundle;

            var sceneLoadData = new SceneLoadData(sceneNames);
            InstanceFinder.SceneManager.LoadGlobalScenes(sceneLoadData);
        }

        /// <summary>
        /// Leaves the current game session entirely - stops this peer's network connection (the
        /// client half always, and the server half too if this peer is the host - same shutdown
        /// <see cref="LeaveLobby"/> does) and then unloads whatever gameplay scenes
        /// <see cref="StartGame"/> last loaded via FishNet's global scenes.
        ///
        /// That unload has to go through plain <see cref="UnityEngine.SceneManagement.SceneManager"/>
        /// rather than <c>InstanceFinder.SceneManager.UnloadGlobalScenes</c> - by this point the
        /// connection (and with it FishNet's own scene-sync machinery) has already been stopped, so
        /// there's no session left to coordinate the unload across; the scenes are just sitting
        /// there locally on this machine now and have to be cleaned up by hand.
        ///
        /// Host-only for the unload half: <see cref="_currentBundle"/> is only ever set inside
        /// <see cref="StartGame"/>, which itself only runs on the host - so a client calling this
        /// still disconnects correctly, it just has nothing recorded to unload (see StartGame's own
        /// host-only gate for the same reason).
        /// </summary>
        public void LeaveGame()
        {
            bool wasServer = InstanceFinder.IsServerStarted;

            InstanceFinder.ClientManager.StopConnection();
            if (wasServer)
                InstanceFinder.ServerManager.StopConnection(sendDisconnectMessage: true);

            if (_currentBundle == null)
                return;

            StartCoroutine(UnloadGameplayScenes(_currentBundle));
            _currentBundle = null;
            _pendingBundle = null;
        }

        private static IEnumerator UnloadGameplayScenes(ContentBundle bundle)
        {
            string[] sceneNames = new[] { bundle.BaseScene.SceneName }
                .Concat(bundle.Layers.Select(layer => layer.SceneName))
                .ToArray();

            foreach (string sceneName in sceneNames)
            {
                UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
                if (scene.IsValid())
                    yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
            }
        }

        /// <summary>
        /// Fires on every peer (host and clients alike, each getting their own local callback once
        /// FishNet's global scenes finish loading on their machine) for every global scene
        /// load/unload, not just <see cref="StartGame"/>'s.
        ///
        /// Unconditionally unloads whatever <see cref="ContentSceneManager"/> currently has loaded
        /// locally (e.g. the lobby content scene, loaded via LoadBundle from the main menu) - that
        /// manager is a separate, non-networked system that StartGame's global-scene load/unload
        /// knows nothing about, so without this the lobby UI would stay loaded underneath gameplay
        /// forever. Safe to call every time: once ContentSceneManager has nothing loaded, it's a
        /// no-op, so later unrelated global loads (e.g. a future level transition) don't do anything
        /// here.
        ///
        /// The GameMode-spawning half below stays server-only - filters to the one load it queued
        /// via <see cref="_pendingBundle"/> and ignores the rest (e.g. a future return-to-lobby load).
        /// Spawns exactly one <see cref="GameModeBase"/> - the bundle's own
        /// <see cref="ContentBundle.GameModePrefab"/> if it has one, otherwise
        /// <see cref="defaultGameModePrefab"/> - into the newly loaded base scene, so pawn spawning
        /// still happens for bundles that don't need a custom GameMode.
        /// </summary>
        private void HandleLoadEnd(SceneLoadEndEventArgs args)
        {
            ContentSceneManager.UnloadActiveBundle();

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
