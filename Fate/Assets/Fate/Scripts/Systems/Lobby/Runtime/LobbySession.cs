using Fate.Systems.Utilities;
using FishNet;
using FishNet.Managing.Scened;
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

        public void StartHost()
        {
            InstanceFinder.ServerManager.StartConnection();
            InstanceFinder.ClientManager.StartConnection();
        }

        public void JoinHost(string address)
        {
            InstanceFinder.ClientManager.StartConnection(address);
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
