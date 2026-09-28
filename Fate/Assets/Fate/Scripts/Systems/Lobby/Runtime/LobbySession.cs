using System.Linq;
using Fate.Systems.Identity;
using Fate.Systems.Utilities;
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
    public class LobbySession : MonoBehaviour
    {
        [Tooltip("Scene to load for every connection once the lobby starts the game.")]
        [SerializeField] private SceneReference gameplayScene;

        private PlayerProfileStore _profileStore;

        private void Awake()
        {
            _profileStore = new PlayerProfileStore();
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
        /// Announces every locally-selected profile on this machine to the server as this
        /// connection's "online presence" (see <see cref="PlayerIdentityBroadcast"/> /
        /// <see cref="PartyRosterService"/>). Today that's just whatever profiles exist in the
        /// local save file - the (architecture-only) join lobby is what will eventually let a
        /// player pick which saved profile(s) to bring instead of sending all of them.
        /// </summary>
        private void SendLocalIdentity()
        {
            var localPlayers = _profileStore.LoadAll()
                .Select(p => new LocalPlayerEntry { PlayerId = p.Id, DisplayName = p.DisplayName })
                .ToArray();

            if (localPlayers.Length == 0)
            {
                Debug.LogWarning($"{nameof(LobbySession)}: no saved player profiles to announce - " +
                                  "create one via PlayerProfileStore before connecting.", this);
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
        /// Host-only. Pushes every current (and future) connection into the gameplay scene
        /// together. The Lobby content scene stays loaded underneath so the NetworkManager
        /// and the live connections survive the transition.
        /// </summary>
        public void StartGame()
        {
            if (!InstanceFinder.IsServerStarted)
            {
                Debug.LogWarning($"{nameof(StartGame)} can only be called by the host.", this);
                return;
            }

            if (gameplayScene == null || !gameplayScene.IsAssigned)
            {
                Debug.LogError($"{nameof(LobbySession)} has no gameplay scene assigned.", this);
                return;
            }

            var sceneLoadData = new SceneLoadData(gameplayScene.SceneName);
            InstanceFinder.SceneManager.LoadGlobalScenes(sceneLoadData);
        }
    }
}
