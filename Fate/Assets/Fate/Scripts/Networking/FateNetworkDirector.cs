using System;
using UnityEngine;
using FishNet.Managing;
using FishNet.Transporting.Tugboat;

[Serializable]
public enum FateClientMode
{
    HOST = 0,
    CLIENT= 1
}

public class FateNetworkDirector : MonoBehaviour
{
    // server only
    public static Action ServerStartedEvent;
    public static Action GameSceneLoadedEvent;
    
    // client only
    public static Action ConnectedToServerEvent;
    public static Action DisconnectedFromServerEvent;

    public static FateNetworkDirector Instance { get; private set; } = null;
    public static bool ServerStarted
    {
        get
        {
            if (Instance != null)
            {
                return Instance.serverStarted;
            }

            return false;
        }
        private set
        {
            if (Instance != null)
            {
                ServerStarted = value;
            }
        }
    }

    public static bool GameSceneLoaded
    {
        get
        {
            if (Instance != null)
            {
                return Instance.serverStarted;
            }

            return false;
        }
        private set
        {
            if (Instance != null)
            {
                ServerStarted = value;
            }
        }
    }

    [SerializeField]
    private FateClientMode mode = FateClientMode.CLIENT;

    [SerializeField]
    private ushort port = 7777;

    [SerializeField]
    private NetworkManager networkManager = null;

    private Tugboat tugBoat = null;
    private bool serverStarted = false;
    private bool gameSceneLoaded = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if(networkManager == null)
        {
            networkManager = GetComponent<NetworkManager>();
            if(networkManager == null)
            {
                Debug.LogError($"FateNetworkDirector.Awake() No NetworkManager found...");
                return;
            }
        }

        if(tugBoat == null)
        {
            tugBoat = networkManager.TransportManager.Transport as Tugboat;
            if(tugBoat == null)
            {
                Debug.LogError($"FateNetworkDirector.Awake() No Tugboat found...");
                return;
            }
        }

        if(mode == FateClientMode.HOST)
        {
            StartHost(port);
        }
        else
        {
            // TODO: get an IP to this step
            StartClient("127.0.0.1", port);
        }
    }

    private void OnDestroy()
    {
        if(Instance == this)
        {
            Instance = null;
        }
    }

    private void StartHost(ushort port)
    {
        if(networkManager && tugBoat)
        {
            tugBoat.SetPort(port);

            networkManager.ServerManager.StartConnection();
            networkManager.ClientManager.StartConnection();

            //SceneLoadData data = new SceneLoadData("GameScene");
            //manager.SceneManager.LoadGlobalScenes(data);
        }
        else
        {
            // TODO: error log
        }
    }

    private void StartClient(string ip, ushort port)
    {
        if (networkManager && tugBoat)
        {
            tugBoat.SetClientAddress(ip);
            tugBoat.SetPort(port);

            networkManager.ClientManager.StartConnection();
        }
        else
        {
            // TODO: error log
        }
    }
}
