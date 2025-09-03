using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public struct Notification : NetworkMessage
{
    public string text;
}
public struct GameSettings : NetworkMessage
{
    public int Budget;
    public int MapSeed;
    public Vector2 size;
    public int MountainChance;
    public int HillChance;
    public int WaterChance;
}


//public struct ClientUnit : NetworkMessage
//{
//    public GameObject Unit;
//    public CombatData UnitData;
//    public Vector3 position;
//}


public class NetworkRelay : NetworkBehaviour
{

    //public static NetworkRelay instance;

    public List<GameObject> prefabs = new List<GameObject>();

    private void Start()
    {
        //instance = this;
    }

    [ClientRpc]
    public void RpcSendMessage(string message)
    {
        Debug.Log("Server message: " + message);
        // You can trigger UI updates or other client-side logic here
    }

    [ClientRpc]
    public void RpcKickToHome()
    {
        // Load home scene on client
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");

        // Disconnect client
        NetworkManager.singleton.StopClient();
    }


    [Command]
    public void CmdSendMessageToServer(string message)
    {
        Debug.Log("Client sent to server: " + message);
        NetworkClient.Send<Notification>(new Notification { text = message });
    }

    [Command(requiresAuthority = false)]
    public void CmdSpawnUnit(Vector3 spawnPos, Quaternion rotation, int team, int unitType)
    {
        GameObject unit = Instantiate(prefabs[unitType], spawnPos, rotation);

        try
        {
            unit.GetComponent<UnitClass>().team = team;
            //unit.GetComponent<UnitClass>().data.team = team;
        }
        catch { }

        NetworkServer.Spawn(unit);


        Debug.Log($"[Server] Spawned unit: {unit.name} at {spawnPos}");
    }

    [Command]
    internal void CmdDestroyUnit(GameObject unit)
    {
        NetworkServer.Destroy(unit);
    }
}