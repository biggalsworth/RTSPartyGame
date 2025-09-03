using Mirror;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections;

public class ServerClient : MonoBehaviour
{
    public static ServerClient instance;

    public string joinCode;
    bool connected = false;

    internal NetworkRelay messageRelay;

    internal int GameState = 0;

    private void Start()
    {
        if (instance != null)
        {
            Destroy(gameObject); // Already exists, kill the duplicate
            return;
        }

        instance = this;

        DontDestroyOnLoad(gameObject);

        //joinCode = MatchSettings.instance.JoinCode;
        //joinCode = GetLocalIPAddress();

        //messageRelay = new NetworkRelay();

        NetworkClient.RegisterHandler<Notification>(OnMessageRecieved);
        NetworkClient.RegisterHandler<GameSettings>(RecievedSettings);
        NetworkClient.OnConnectedEvent += HandleClientConnected;    

    }

    private void HandleClientConnected()
    {
        Debug.Log("Waiting for message relay: " + messageRelay);
        StartCoroutine(WaitForIdentity());
    }

    private IEnumerator WaitForIdentity()
    {
        while (NetworkClient.connection == null || NetworkClient.connection.identity == null)
        {
            Debug.Log("MessageRelay is not ready");
            yield return null;
        }

        messageRelay = NetworkClient.connection.identity.GetComponent<NetworkRelay>();
        Debug.Log("MessageRelay is ready: " + messageRelay);
    }


    private void Update()
    {
        if (!connected)
            AttemptConnection();

        if (connected && messageRelay == null)
        {
            GetLocalPlayer();
        }

        //foreach (var kvp in NetworkServer.connections)
        //{
        //    int connectionId = kvp.Key;
        //    NetworkConnectionToClient conn = kvp.Value;
        //
        //    Debug.Log($"Client connected: ID = {connectionId}, Address = {conn.address}");
        //}

    }


    void AttemptConnection()
    {
        joinCode = MatchSettings.instance.JoinCode;

        NetworkManager.singleton.networkAddress = joinCode;
        NetworkManager.singleton.StartClient();
        Debug.Log("Connecting to " + joinCode);
        if(NetworkClient.isConnected)
            connected = true;
        else
            NetworkClient.Send(new Notification { text = "connected" });

        //});
    }

    public void Disconnect()
    {
        NetworkClient.Disconnect();
    }


    void GetLocalPlayer()
    {
        if (NetworkClient.connection == null || NetworkClient.connection.identity == null)
            return;

        messageRelay = NetworkClient.connection.identity.GetComponent<NetworkRelay>();
        Debug.Log("MessageRelay is ready: " + messageRelay);
    }

    public string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                return ip.ToString();
        }
        return "No IPv4 address found";
    }

    void OnMessageRecieved(Notification msg)
    {
        Debug.Log("Message recieved from server " + msg.text);
        HandleMessage(msg.text);
    }

    public void MessageServer(string msg)
    {
        NetworkClient.Send(new Notification { text = msg });
        //messageRelay.CmdSendMessageToServer(msg);
    }

    void RecievedSettings(GameSettings settings)
    {
        MatchSettings.instance.Budget = settings.Budget;
        MatchSettings.instance.MapSeed = settings.MapSeed;
        MatchSettings.instance.size = settings.size;
        MatchSettings.instance.MountainChance = settings.MountainChance;
        MatchSettings.instance.HillChance = settings.HillChance;
        MatchSettings.instance.WaterChance = settings.WaterChance;
    }


    public void HandleMessage(string text)
    {
        List<string> lines = new List<string>(
             text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
             .Select(line => line.Replace(" ", "")));

        if (lines[0] == "disconnect")
        {
            NetworkClient.Disconnect();
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            Destroy(gameObject);
        }
        if (lines[0] == "team")
        {
            MatchSettings.instance.team = int.Parse(lines[1]);
        }
        if (lines[0] == "turn")
        {
            GameplayManager.instance.turn = int.Parse(lines[1]);
            foreach(UnitClass unit in GameObject.FindObjectsByType<UnitClass>(FindObjectsSortMode.None))
            {
                if (unit.team != MatchSettings.instance.team)
                    unit.Mesh.SetActive(false);
            }
        }
        if (lines[0] == "lost")
        {
            GameObject.FindWithTag("Player").SetActive(false);
            GameState = int.Parse(lines[1]);
        }
    }

    public void SpawnUnit(Vector3 position, Quaternion rotation, int team, int v)
    {
        messageRelay.CmdSpawnUnit(position, rotation, team, v);
    }

    internal void DestroyUnit(GameObject unit)
    {
        messageRelay.CmdDestroyUnit(unit);
    }
}

