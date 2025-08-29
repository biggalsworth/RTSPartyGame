using Mirror;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using System;
using System.Linq;

public class ServerHost : MonoBehaviour
{
    public static ServerHost instance;

    public string JoinCode;

    private static Dictionary<string, string> codeToIP = new Dictionary<string, string>();

    bool serverHosted = false;

    internal NetworkRelay messageRelay;


    int turn;

    private void Start()
    {
        if (instance != null)
        {
            Destroy(gameObject); // Already exists, kill the duplicate
            return;
        }

        instance = this;

        JoinCode = MatchSettings.instance.JoinCode;

        if (MatchSettings.instance.hosting)
        {
            DontDestroyOnLoad(gameObject);
            CreateServer();
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
    void CreateServer()
    {

        JoinCode = GetLocalIPAddress();
        MatchSettings.instance.JoinCode = JoinCode;

        //string joinCode = GenerateJoinCode();
        //codeToIP[JoinCode] = ip;

        NetworkManager.singleton.networkAddress = JoinCode;
        NetworkManager.singleton.StartHost();
        NetworkServer.RegisterHandler<Notification>(OnChatMessageReceived);

        Debug.Log($"Join Code: {JoinCode}");

        serverHosted = true;
    }


    string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                return ip.ToString();
        }
        return "No IPv4 address found";
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
