using Mirror;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using UnityEditor;
using Unity.Services.Core.Environments;

using Unity.Networking.Transport.Relay;
using TMPro;
using Utp;
using Unity.Services.Matchmaker.Models;


public class ServerClient : MonoBehaviour
{
    public static ServerClient instance;

    public string joinCode = "";
    internal bool connected = false;
    bool isConnecting = false;
    internal bool canConnect = true;

    public NetworkRelay messageRelay = null;

    internal int GameState = 0;

    NetworkIdentity player;

    private void Start()
    {
        if (FindObjectsByType<NetworkManager>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        if (instance != null)
        {
            Destroy(gameObject); // Already exists, kill the duplicate
            return;
        }

        instance = this;

        DontDestroyOnLoad(gameObject);

        joinCode = MatchSettings.instance.JoinCode;
        //joinCode = GetLocalIPAddress();

        //messageRelay = new NetworkRelay();

        NetworkClient.OnConnectedEvent += HandleClientConnected;

        //StartCoroutine(AttemptConnection());    
        //StartCoroutine(WaitForIdentity());


        //Assign handlers before starting the client
        NetworkClient.RegisterHandler<Notification>(OnMessageRecieved);
        NetworkClient.RegisterHandler<GameSettings>(RecievedSettings);

    }
    void print() { Debug.Log("WE HAVE CONNECTED"); }


    private void Update()
    {
        if (MatchSettings.instance.hosting && MatchSettings.instance.JoinCode == "")
            return;

        if (!connected && !isConnecting)
        {
            isConnecting = true;
            StartCoroutine(AttemptConnection());
            //if (connected)
            //    HandleClientConnected();
        }

        if (NetworkClient.isConnected && !isConnecting)
        {
            player = NetworkClient.localPlayer;
            if (NetworkClient.ready == false)
                NetworkClient.Ready();
            if (messageRelay == null)
                messageRelay = NetworkClient.localPlayer.GetComponent<NetworkRelay>();
        }

    }



    IEnumerator AttemptConnection()
    //void AttemptConnection()
    {
        yield return new WaitForSeconds(1f);

        while (connected == false)
        {
            connected = NetworkClient.isConnected;

            if (!connected)
            {
                Debug.Log("Disconnecting");
                NetworkClient.Disconnect();
            }

            joinCode = MatchSettings.instance.JoinCode;

            isConnecting = true;
            //joinCode = MatchSettings.instance.JoinCode;
            //if (joinCode == "")
            //    yield return new WaitForSeconds(5f);


            //Assign handlers before starting the client
            NetworkClient.RegisterHandler<Notification>(OnMessageRecieved);
            NetworkClient.RegisterHandler<GameSettings>(RecievedSettings);

            //Check we arent hosting
            if (!NetworkServer.active)
            {
                // Only non-host clients connect
                JoinRelayClient(joinCode);
            }

            Debug.Log("Connecting via Relay with join code: " + joinCode);

            yield return new WaitForSeconds(3f);

            Debug.Log("Connected : " + NetworkClient.isConnected);
            connected = NetworkClient.isConnected;

        }

        if (!NetworkClient.ready)
        {
            NetworkClient.Ready();
        }


        GameObject.Find("NetworkInfo").GetComponent<TextMeshProUGUI>().text = $@"
UnityServices initialized
Signed in: {AuthenticationService.Instance.IsSignedIn}
Join code: {joinCode}
NetworkClient.isConnected: {NetworkClient.isConnected}
Connection: {NetworkClient.connection}
Identity: {NetworkClient.connection?.identity}";

        yield return new WaitForSeconds(1f);

        HandleClientConnected();

        yield return new WaitForSeconds(1f);
    }


    private void HandleClientConnected()
    {
        Debug.Log("Waiting for message relay");
        StartCoroutine(WaitForIdentity());
        connected = true;
        isConnecting = false; //allow retries later
        canConnect = false;

    }

    private IEnumerator WaitForIdentity()
    {
        if(NetworkClient.ready == false)
            NetworkClient.Ready();
        while (NetworkClient.isConnected == false && (NetworkClient.connection == null || NetworkClient.connection.identity == null))
        {
            Debug.Log("MessageRelay is not ready");
            yield return null;
        }

        messageRelay = NetworkClient.connection.identity.GetComponent<NetworkRelay>();
        Debug.Log("MessageRelay is ready: " + messageRelay);

        //yield return new WaitForSeconds(1f);

        NetworkClient.Send<Notification>(new Notification { text = "connected" });

        //messageRelay.CmdSendMessageToServer("connected");

    }

    //Using new unity UCP connections with mirror
    public async void JoinRelayClient(string joinCode)
    {
        if (MatchSettings.instance.hosting == false) // if we are hosting, this will have already been handled
        {
            await UnityServices.InitializeAsync();

            GameObject.Find("NetworkInfo").GetComponent<TextMeshProUGUI>().text = "Client Signing in";

            if (!AuthenticationService.Instance.IsSignedIn && MatchSettings.instance.hosting == false) //only sign in if we are not hosting
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync(new SignInOptions { CreateAccount = true });
            }
        }

        var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        var relayServerData = AllocationUtils.ToRelayServerData(joinAllocation, "udp");

        Debug.Log("ConnectionData: " + Convert.ToBase64String(joinAllocation.ConnectionData));

        //var transport = NetworkManager.singleton.GetComponent<UtpTransport>();
        //transport.SetRelayServerData(relayServerData);
        //
        //NetworkManager.singleton.StartClient();

        var transport = NetworkManager.singleton.GetComponent<UtpTransport>();

        if (transport == null)
        {
            Debug.LogError("UtpTransport not found on NetworkManager");
            return;
        }

        NetworkManager.singleton.networkAddress = "relay"; // Bypass Mirror's check
        
        
        transport.useRelay = true;
        transport.ConfigureClientWithJoinCode(joinCode,
            onSuccess: () =>
            {
                //NetworkManager.singleton.StopClient();
                NetworkManager.singleton.StartClient();
            },
            onFailure: () => Debug.LogError("Relay join failed"));



        //Debug.Log("Transport connected: " + transport.ClientConnected());
        //Debug.Log("Mirror connected: " + NetworkClient.isConnected);


        //NetworkManager.singleton.networkAddress = ""; // Bypass Mirror's check

    }


    IEnumerator RestartClient()
    {
        NetworkManager.singleton.StopClient();
        yield return null; // Wait one frame
        NetworkManager.singleton.StartClient();
    }

    public void Disconnect()
    {
        NetworkManager.singleton.StopClient();
        NetworkClient.Disconnect();

        connected = false;
        isConnecting = false;
        //canConnect = false;
        joinCode = "";
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

    public void OnMessageRecieved(Notification msg)
    {
        Debug.Log("Message recieved from server " + msg.text);
        HandleMessage(msg.text);
    }

    public void MessageServer(string msg)
    {
        NetworkClient.Send(new Notification { text = msg });
        //messageRelay.CmdSendMessageToServer(msg);
    }

    public void RecievedSettings(GameSettings settings)
    {
        Debug.Log("Recieved Settings");
        MatchSettings.instance.Budget = settings.Budget;
        MatchSettings.instance.MapSeed = settings.MapSeed;
        MatchSettings.instance.size.x = settings.sizex;
        MatchSettings.instance.size.y = settings.sizey;
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
            Disconnect();
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            //Destroy(gameObject);
        }
        if (lines[0] == "team")
        {
            MatchSettings.instance.team = int.Parse(lines[1]);
        }
        if (lines[0] == "turn")
        {
            GameObject.Find("TurnText").GetComponent<TextMeshProUGUI>().text = "Turn: " + lines[1];
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
        //messageRelay.CmdSpawnUnit(position, rotation, team, v);

        //player = NetworkClient.localPlayer;
        Debug.Log("LOCAL PLAYER: " + NetworkClient.localPlayer);
        if (player != null)
        {
            messageRelay = NetworkClient.localPlayer.GetComponent<NetworkRelay>();

            if (messageRelay != null)
            {
                messageRelay.CmdSpawnUnit(position, rotation, team, v);
            }
            else
            {
                Debug.LogWarning("NetworkRelay not found on player.");
            }
        }
        else
        {
            Debug.LogWarning("Local player not available.");
        }

    }

    internal void DestroyUnit(GameObject unit)
    {
        player.GetComponent<NetworkRelay>().CmdDestroyUnit(unit);
    }
}

