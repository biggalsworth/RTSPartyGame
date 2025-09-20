using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[System.Serializable]
public struct Notification : NetworkMessage
{
    public string text;
}

[System.Serializable]
public struct GameSettings : NetworkMessage
{
    public int Budget;
    public int MapSeed;
    public float sizex;
    public float sizey;
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

    public static NetworkRelay instance;

    public List<GameObject> prefabs = new List<GameObject>();

    private void Start()
    {
        instance = this;
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

        unit.GetComponent<UnitClass>().team = team;
        unit.GetComponent<UnitClient>().team = team;
        //unit.GetComponent<UnitClass>().data.team = team;
        

        NetworkServer.Spawn(unit);

        Debug.Log($"[Server] Spawned unit: {unit.name} at {spawnPos}");
    }

    [Command]
    internal void CmdDestroyUnit(GameObject unit)
    {
        NetworkServer.Destroy(unit);
    }

    internal void ApplyMovements(int turn)
    {
        foreach (var obj in GameObject.FindObjectsByType<UnitClient>(FindObjectsSortMode.None))
        {
            uint netId = obj.GetComponent<NetworkIdentity>().netId;

            if (obj.GetComponent<UnitClass>() && obj.GetComponent<UnitClass>().data != null)
                CmdUpdateUnitStats(netId, obj.GetComponent<UnitClass>().data.health);

            if (obj.GetComponent<UnitClass>().team != turn)
            {
                //send an invalid position so we dont update units positions
                CmdUpdateUnitPosition(netId, new Vector3(-1f, -1f, -1f));
                continue;
            }

            //If the unit is the locals client, update to our position and update other clients to use our stats for our units
            Vector3 finalPos = HexManager.instance.SnapToHexGrid(obj.transform.position, 2.0f);
            CmdUpdateUnitPosition(netId, finalPos);

        }
    }

    [Command]
    public void CmdUpdateUnitPosition(uint netId, Vector3 newPos)
    {
        if (newPos != new Vector3(-1f, -1f, -1f))
        {
            if (NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity identity))
            {
                UnitClient unit = identity.GetComponent<UnitClient>();
                if (unit != null)
                {
                    unit.targetHexPosition = newPos; // Server sets SyncVar
                    unit.GetComponent<UnitClass>()?.NewTurn();
                }
            }
        }
        //dont change units positions but do tell them its a new turn.
        if(newPos == new Vector3(-1f, -1f, -1f))
        {
            if (NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity identity))
            {
                UnitClient unit = identity.GetComponent<UnitClient>();
                if (unit != null)
                {
                    unit.GetComponent<UnitClass>()?.NewTurn();
                }
            }
        }
    }

    [Command]
    public void CmdUpdateUnitStats(uint netId, int health)
    {
        if (NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity identity))
        {
            UnitClass unit = identity.GetComponent<UnitClass>();
            if (unit != null)
            {
                unit.health = health;
                unit.data.health = health;
            }
        }
    }


    //[Command]
    //public void CmdApplyMovements(int turn)
    //{
    //    foreach (var obj in GameObject.FindObjectsByType<UnitClient>(FindObjectsSortMode.None))
    //    {
    //        //if (obj.GetComponent<UnitClass>() && obj.GetComponent<UnitClass>().team == turn)
    //        //{
    //            obj.ApplyUnitMovement(obj.GetComponent<NetworkIdentity>().netId, HexManager.instance.SnapToHexGrid(obj.transform.position, 2.0f));
    //            obj.GetComponent<UnitClass>().NewTurn();
    //        //}
    //        //else if (obj.GetComponent<BuildingClass>() && obj.GetComponent<BuildingClass>().team == turn)
    //        //{
    //        //    obj.ApplyServerMovement(HexManager.instance.SnapToHexGrid(obj.transform.position, 2.0f));
    //        //}
    //    }
    //
    //}
}