//using FishNet;
//using FishNet.Connection;
//using FishNet.Object;
//using FishNet.Transporting;
//using UnityEngine;
//using System.Collections.Generic;

//public class PlayerSpawnManager : MonoBehaviour
//{
//    public NetworkObject playerPrefab;

//    private HashSet<int> spawnedConnections = new HashSet<int>();

//    private void OnEnable()
//    {
//        InstanceFinder.SceneManager.OnLoadEnd += OnSceneLoadEnd;
//        InstanceFinder.ServerManager.OnRemoteConnectionState += OnClientConnectionState;
//    }

//    private void OnDisable()
//    {
//        InstanceFinder.SceneManager.OnLoadEnd -= OnSceneLoadEnd;
//        InstanceFinder.ServerManager.OnClientConnectionState -= OnClientConnectionState;
//    }

//    // Server scene finished loading
//    private void OnSceneLoadEnd(FishNet.Managing.Scened.SceneLoadEndEventArgs args)
//    {
//        if (!InstanceFinder.IsServer) return;

//        // Spawn players for all clients connected at this time
//        foreach (NetworkConnection conn in InstanceFinder.ServerManager.Clients.Values)
//        {
//            TrySpawnPlayer(conn);
//        }
//    }

//    // Handle late joiners
//    private void OnClientConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
//    {
//        if (!InstanceFinder.IsServer) return;
//        if (state.ConnectionState != conn.Con) return;

//        TrySpawnPlayer(conn);
//    }

//    private void TrySpawnPlayer(NetworkConnection conn)
//    {
//        if (spawnedConnections.Contains(conn.ClientId)) return;

//        NetworkObject player = Instantiate(playerPrefab);
//        InstanceFinder.ServerManager.Spawn(player, conn);
//        spawnedConnections.Add(conn.ClientId);

//        Debug.Log($"Spawned player for connection {conn.ClientId}");
//    }
//}