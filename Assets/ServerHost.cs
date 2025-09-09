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

public class ServerHost : MonoBehaviour
{
    public static ServerHost instance;

    public string JoinCode;

    private static Dictionary<string, string> codeToIP = new Dictionary<string, string>();

    bool serverHosted = false;

    internal NetworkRelay messageRelay;


    int turn;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject); // Already exists, kill the duplicate
            return;
        }

        instance = this;

        //JoinCode = GetLocalIPAddress();

        if (MatchSettings.instance.hosting)
        {
            DontDestroyOnLoad(gameObject);
            StartCoroutine(LaunchRelayServer());
            //CreateServer();
        }

        //messageRelay = NetworkRelay.instance;

    }

    private void Update()
    {
        //if (!hosting)
        //    CreateServer();

        //if (SceneManager.GetActiveScene().buildIndex == 0)
        //    Destroy(gameObject);
    }


    //Creating connections
    private IEnumerator LaunchRelayServer()
    {
        var task = CreateServer();
        while (!task.IsCompleted) yield return null;

        if (task.Exception != null)
            Debug.LogError(task.Exception);
        else
        {
            Debug.Log("UnityServices initialized");
            Debug.Log("Signed in: " + AuthenticationService.Instance.IsSignedIn);
            Debug.Log("Join code: " + JoinCode);
            Debug.Log("NetworkClient.isConnected: " + NetworkClient.isConnected);
            Debug.Log("Connection: " + NetworkClient.connection);
            Debug.Log("Identity: " + NetworkClient.connection?.identity);

            GameObject.Find("NetworkInfo").GetComponent<TextMeshProUGUI>().text = $@"
UnityServices initialized
Signed in: {AuthenticationService.Instance.IsSignedIn}
Join code: {JoinCode}
NetworkClient.isConnected: {NetworkClient.isConnected}
Connection: {NetworkClient.connection}
Identity: {NetworkClient.connection?.identity}";

        }

    }
    public async Task CreateServer()
    {
        //var options = new InitializationOptions().SetEnvironmentName("production"); // or "staging"
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync(new SignInOptions { CreateAccount = true });
        }

        var allocation = await RelayService.Instance.CreateAllocationAsync(4);
        JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        MatchSettings.instance.JoinCode = JoinCode;


        var relayServerData = AllocationUtils.ToRelayServerData(allocation, "udp");
        var transport = NetworkManager.singleton.GetComponent<UnityTransport>();
        
        Debug.Log("NetworkManager.singleton: " + NetworkManager.singleton);
        Debug.Log("UnityTransport component: " + NetworkManager.singleton?.GetComponent<UnityTransport>());
        Debug.Log("RelayServerData: " + relayServerData);

        transport.SetRelayServerData(relayServerData);

        NetworkManager.singleton.StartHost();

        NetworkServer.RegisterHandler<Notification>(OnChatMessageReceived);

        Debug.Log($"Relay Join Code: {JoinCode}");
        serverHosted = true;
    }


    //async void CreateServer()
    //{
    //    await UnityServices.InitializeAsync();
    //    await AuthenticationService.Instance.SignInAnonymouslyAsync();
    //
    //    var allocation = await RelayService.Instance.CreateAllocationAsync(4);
    //    JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
    //    MatchSettings.instance.JoinCode = JoinCode;
    //
    //    var relayServerData = AllocationUtils.ToRelayServerData(allocation, "udp");
    //    var transport = NetworkManager.singleton.GetComponent<UnityTransport>();
    //    transport.SetRelayServerData(relayServerData);
    //
    //    NetworkManager.singleton.StartHost();
    //    NetworkServer.RegisterHandler<Notification>(OnChatMessageReceived);
    //
    //    Debug.Log($"Relay Join Code: {JoinCode}");
    //    serverHosted = true;
    //}

    //Using mirror
    /*
    void CreateServer()
    {

        JoinCode = GetLocalIPAddress();
        //JoinCode = "localHost";
        MatchSettings.instance.JoinCode = JoinCode;

        //string joinCode = GenerateJoinCode();
        //codeToIP[JoinCode] = ip;

        NetworkManager.singleton.networkAddress = JoinCode;
        NetworkManager.singleton.StartHost();
        NetworkServer.RegisterHandler<Notification>(OnChatMessageReceived);

        Debug.Log($"Join Code: {JoinCode}");

        serverHosted = true;
    }
    */

    string GetLocalIPAddress()
    {
        using (var webClient = new System.Net.WebClient())
        {
            return webClient.DownloadString("https://api.ipify.org");
        }

        //var host = Dns.GetHostEntry(Dns.GetHostName());
        //foreach (var ip in host.AddressList)
        //{
        //    if (ip.AddressFamily == AddressFamily.InterNetwork)
        //        return ip.ToString();
        //}
        //return "No IPv4 address found";
    }

    public static string LookupIP(string code)
    {
        return codeToIP.ContainsKey(code) ? codeToIP[code] : null;
    }

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
        NetworkManager.singleton.ServerChangeScene("PlayScene");
    }
    internal void CloseGame()
    {
        NetworkServer.SendToAll<Notification>(new Notification { text = "disconnect" });
        NetworkManager.singleton.StopHost();

    }

    public void HandleMessage(string message, NetworkConnectionToClient conn)
    {
        List<string> lines = new List<string>(
            message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Replace(" ", "")));

        if (message == "connected" && MatchSettings.instance.hosting)
        {
            //Send match settings
            MatchSettings settings = MatchSettings.instance;
            GameSettings newSettings = new GameSettings
            {
                MapSeed = settings.MapSeed,
                Budget = settings.Budget,
                size = settings.size,
                MountainChance = settings.MountainChance,
                HillChance = settings.HillChance,
                WaterChance = settings.WaterChance
            };
            conn.Send<GameSettings>(newSettings);
            conn.Send<Notification>(new Notification { text = "team\n" + (NetworkServer.connections.Count - 1) });

            NetworkServer.SendToAll<Notification>(new Notification { text = "NEWCONNECTION"});

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
}
