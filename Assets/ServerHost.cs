using Mirror;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using System;
using System.Linq;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using System.Collections;
using System.Threading.Tasks;
using UnityEditor;
using Unity.Services.Core.Environments;
using TMPro;
using Utp;

public class ServerHost : MonoBehaviour
{
    public static ServerHost instance;

    public string JoinCode;

    private static Dictionary<string, string> codeToIP = new Dictionary<string, string>();

    bool serverHosted = false;

    internal NetworkRelay messageRelay;

    int team = 0;
    int turn = 0;

    public bool connected;
    public bool isConnecting;
    public bool canConnect;
    public bool canStart;

    private async void Start()
    {
        if (ServerHost.instance != null)
        {
            Destroy(gameObject); // Already exists, kill the duplicate
            return;
        }

        instance = this;

        connected = false;
        isConnecting = false;
        canConnect = false;


        //JoinCode = GetLocalIPAddress();

        NetworkServer.RegisterHandler<Notification>(OnChatMessageReceived);

        if (MatchSettings.instance.hosting && !isConnecting)
        {
            DontDestroyOnLoad(gameObject);
            //CreateServer();

            GetComponent<ServerClient>().canConnect = false;
            //NetworkClient.RegisterHandler<Notification>(GetComponent<ServerClient>().OnMessageRecieved);
            //NetworkClient.RegisterHandler<GameSettings>(GetComponent<ServerClient>().RecievedSettings);

            GameObject.Find("NetworkInfo").GetComponent<TextMeshProUGUI>().text = "Attempting hosting";

            //await LaunchRelayServer();

            StartCoroutine(LaunchRelayServerCoroutine());

            //StartCoroutine(LaunchRelayServer());
        }
        else
        {
            GameObject.Find("NetworkInfo").GetComponent<TextMeshProUGUI>().text = "Not hosting, joining as client...";
        }

        messageRelay = NetworkRelay.instance;

    }

    private void Awake()
    {
        // Subscribe to connection events
        NetworkServer.OnConnectedEvent += OnClientConnected;
        NetworkServer.OnDisconnectedEvent += OnClientDisconnected;
    }


    private void OnDestroy()
    {
        // Clean up to avoid memory leaks
        NetworkServer.OnConnectedEvent -= OnClientConnected;
        NetworkServer.OnDisconnectedEvent -= OnClientDisconnected;

        AuthenticationService.Instance.SignOut(true);

        Destroy(NetworkManager.singleton.gameObject);
    }
    private void OnServerStarted(NetworkConnectionToClient conn)
    {
        // Now the server is active, register handlers
        //NetworkServer.RegisterHandler<Notification>(OnChatMessageReceived);
        //Debug.Log("Registered Notification handler");
    }

    private void OnClientConnected(NetworkConnectionToClient conn)
    {
        Debug.Log($"[Server] Client connected: {conn.connectionId}");

        canStart = false;
    }

    private void OnClientDisconnected(NetworkConnectionToClient conn)
    {
        Debug.Log($"[Server] Client disconnected: {conn.connectionId}");
    }

    #region Creating connections

    private IEnumerator LaunchRelayServerCoroutine()
    {
        // Async init
        var initTask = LaunchRelayServer();
        while (!initTask.IsCompleted) yield return null;
    
        if (initTask.Exception != null)
        {
            Debug.LogError(initTask.Exception);
            GameObject.Find("NetworkInfo").GetComponent<TextMeshProUGUI>().text = "Error launching server: " + initTask.Exception;
        }
    }
    
    private async Task LaunchRelayServer() // with task
    {
        if (!UnityServices.State.Equals(ServicesInitializationState.Initialized))
        {
            await UnityServices.InitializeAsync();
        }
    
        if (AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut();
        }

        await Task.Delay(500);

        bool isSigningIn = false;
        if (!AuthenticationService.Instance.IsSignedIn && !isSigningIn)
        {
            isSigningIn = true;
            await AuthenticationService.Instance.SignInAnonymouslyAsync(new SignInOptions { CreateAccount = true });
        }
        isSigningIn = false;
    
        // Wait until the NetworkManager exists
        while (NetworkManager.singleton == null)
        {
            await Task.Yield(); // equivalent to yield return null
        }
    
        GameObject.Find("NetworkInfo").GetComponent<TextMeshProUGUI>().text = "Allocations complete";
    
        await Task.Delay(200); // 0.1s
    
        Debug.LogWarning("Creating Server");
        isConnecting = true;
    
        try
        {
            await CreateServer(); // your server setup method
            connected = true;
            isConnecting = false;
            canConnect = false;

            GameObject.Find("NetworkInfo").GetComponent<TextMeshProUGUI>().text = "Server Created - Connecting";

            MatchSettings.instance.JoinCode = JoinCode;

        }
        catch (Exception ex)
        {
            GameObject.Find("NetworkInfo").GetComponent<TextMeshProUGUI>().text = "Server Failed: " + ex.Message;
            Debug.LogError(ex);
            isConnecting = false;
        }
    }

    /*//enumerator solution
    private IEnumerator LaunchRelayServer()
    {

        if (!UnityServices.State.Equals(ServicesInitializationState.Initialized))
        {
            var initTask = UnityServices.InitializeAsync();
            while (!initTask.IsCompleted) yield return null;
        }
    
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            var SignInTask = AuthenticationService.Instance.SignInAnonymouslyAsync(new SignInOptions { CreateAccount = true });
            while (!SignInTask.IsCompleted) yield return null;


            if (SignInTask.Exception != null)
            {
                Debug.LogError(SignInTask.Exception);
                GameObject.Find("NetworkInfo").GetComponent<TextMeshProUGUI>().text = "Error launching server: " + SignInTask.Exception;
            }

        }

        yield return new WaitUntil(() => NetworkManager.singleton != null);
        yield return null; // Let Awake() finish to make sure the network manager exists
        yield return null; // maybe one more frame to be safe


        Debug.LogWarning("Creating Server");
        isConnecting = true;
        var task = CreateServer();
        while (!task.IsCompleted) yield return null;

        if (task.Exception != null)
        {
            Debug.LogError(task.Exception);
            isConnecting = false;
        }
        else
        {
            //ServerClient.instance.canConnect = true;

            connected = true;
            isConnecting = false;
            canConnect = false;

        }

    }
    */

    public async Task CreateServer()
    {
        //var options = new InitializationOptions().SetEnvironmentName("production"); // or "staging"
        //await UnityServices.InitializeAsync();

        /*
        //var allocation = await RelayService.Instance.CreateAllocationAsync(4);
        //JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        //MatchSettings.instance.JoinCode = JoinCode;
        //
        //
        //var relayServerData = AllocationUtils.ToRelayServerData(allocation, "udp");
        //var transport = NetworkManager.singleton.GetComponent<UnityTransport>();
        //
        //Debug.Log("NetworkManager.singleton: " + NetworkManager.singleton);
        //Debug.Log("UnityTransport component: " + NetworkManager.singleton?.GetComponent<UnityTransport>());
        //Debug.Log("RelayServerData: " + relayServerData);
        //
        //transport.SetRelayServerData(relayServerData);
        //
        //NetworkManager.singleton.StartServer();
        //
        //NetworkServer.RegisterHandler<Notification>(OnChatMessageReceived);
        //
        //Debug.Log($"Relay Join Code: {JoinCode}");
        //serverHosted = true;
        */
        
        var transport = NetworkManager.singleton.GetComponent<UtpTransport>();
        transport.useRelay = true;

        var tcs = new TaskCompletionSource<string>();

        GameObject.Find("NetworkInfo").GetComponent<TextMeshProUGUI>().text = "Creating server";

        transport.AllocateRelayServer(
            maxPlayers: 4,
            regionId: "europe-central2", // or your preferred region
            onSuccess: (joinCode) =>
            {
                JoinCode = joinCode;
                Debug.Log($"Relay Join Code: {JoinCode}");

                tcs.SetResult(JoinCode);

                //NetworkServer.RegisterHandler<Notification>(OnChatMessageReceived);

                NetworkManager.singleton.StartHost();
                serverHosted = true;
            },
            onFailure: () =>
            {
                Debug.LogError("Relay allocation failed");

                GameObject.Find("NetworkInfo").GetComponent<TextMeshProUGUI>().text = "Relay allocation failed";

                tcs.SetException(new Exception("Relay allocation failed"));
            });


        string joinCode = await tcs.Task;
    }
    public static string LookupIP(string code)
    {
        return codeToIP.ContainsKey(code) ? codeToIP[code] : null;
    }

    #endregion

    internal int GetClientCount()
    {
        return NetworkServer.connections.Count;
    }

    public void BroadcastMessageToClients(string message)
    {
        if (!NetworkServer.active)
        {
            Debug.LogWarning("Server is not active. Cannot send message.");
            return;
        }

        messageRelay.RpcSendMessage(message);
    }
    void OnChatMessageReceived(NetworkConnectionToClient conn, Notification msg)
    {
        Debug.Log($"[SERVER] Received chat: {msg.text} from client {conn.connectionId}");
        HandleMessage(msg.text.ToLower(), conn);
    }



    //Gameplay logic
    public void BeginPlay()
    {
        if (canStart == false)
            return;

        NetworkManager.singleton.ServerChangeScene("PlayScene");
    }
    internal void CloseGame()
    {
        connected = false;
        isConnecting = false;
        NetworkServer.SendToAll<Notification>(new Notification { text = "lost\n" + MatchSettings.instance.team });
        NetworkServer.SendToAll<Notification>(new Notification { text = "disconnect" });
        NetworkManager.singleton.transport.Shutdown();
        NetworkManager.singleton.StopHost(); 
    }

    public void HandleMessage(string message, NetworkConnectionToClient conn)
    {
        List<string> lines = new List<string>(
            message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Replace(" ", "")));

        if (message == "connected" && MatchSettings.instance.hosting)
        {
            canStart = false;
            StartCoroutine(SendWelcomeMessage(conn));

        }

        if (lines[0] == "ready")
        {
            if (canStart == false)
                canStart = true;
        }

        if (lines[0] == "disconnect" && MatchSettings.instance.hosting)
        {
            if (HexManager.instance == null)
                return;

            if (lines[1] == "0")
            {
                HexManager.instance.GetBase(0).SetActive(false);
            }
            else if(lines[1] == "1")
            {
                HexManager.instance.GetBase(1).SetActive(false);
            }
        }

        if (lines[0] == "turn")
        {
            //NetworkServer.SendToAll<Notification>(new Notification { text = "turn\n" + lines[1] });
            NetworkServer.SendToAll(new Notification { text = "turn\n" + lines[1] });
            turn = int.Parse(lines[1]);
        }

        if (lines[0] == "lost")
        {
            NetworkServer.SendToAll(new Notification { text = "lost\n" + lines[1] });
        }
    }

    IEnumerator SendWelcomeMessage(NetworkConnectionToClient conn)
    {
        yield return new WaitUntil(() => conn.isReady);

        conn.Send(new Notification { text = "NEWCONNECTION" });

        //NetworkServer.SendToAll<Notification>(new Notification { text = "NEWCONNECTION" });

        yield return new WaitForSeconds(1f);


        conn.Send<Notification>(new Notification { text = "team\n" + (NetworkServer.connections.Count - 1) });

        //Send match settings
        MatchSettings settings = MatchSettings.instance;
        GameSettings newSettings = new GameSettings
        {
            MapSeed = settings.MapSeed,
            Budget = settings.Budget,
            sizex = settings.size.x,
            sizey = settings.size.y,
            MountainChance = settings.MountainChance,
            HillChance = settings.HillChance,
            WaterChance = settings.WaterChance
        };

        conn.Send<GameSettings>(newSettings);

        //canStart = true;
    }

}
