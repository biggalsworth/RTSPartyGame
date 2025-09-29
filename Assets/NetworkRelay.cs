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

    [Command(requiresAuthority = false)]
    internal void CmdDestroyUnit(GameObject obj)
    {
        uint netId = obj.GetComponent<NetworkIdentity>().netId;

        if (NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity identity))
        {
            GameObject unit = identity.gameObject;

            // optional: cleanup hex before destroy
            UnitClass u = unit.GetComponent<UnitClass>();
            if (u != null)
            {
                RpcUnoccupyHex(u.HexPosition);
            }

            Debug.Log($"[Server] Destroying {unit.name}, netId={unit.GetComponent<NetworkIdentity>().netId}");

            NetworkServer.Destroy(unit);
        }

        //UnitClass u = unit.GetComponent<UnitClass>();
        //if (u != null)
        //{
        //    // Tell all clients to clean up the hex
        //    RpcUnoccupyHex(u.HexPosition);
        //}
        //
        //NetworkServer.Destroy(unit);
    }

    [ClientRpc]
    void RpcUnoccupyHex(Vector2 hexPos)
    {
        HexManager.instance.Hexes[hexPos].UnOccupy();
    }


    internal void ApplyMovements(int turn)
    {
        foreach (var obj in GameObject.FindObjectsByType<UnitClient>(FindObjectsSortMode.None))
        {
            uint netId = obj.GetComponent<NetworkIdentity>().netId;

            //if (obj.GetComponent<UnitClass>() && !obj.GetComponent<UnitClass>().data.Equals(default(UnitStats)))
            //    CmdUpdateUnitStats(netId, obj.GetComponent<UnitClass>().data.health);

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

    internal void ApplyStats()
    {
        foreach (var obj in GameObject.FindObjectsByType<UnitClient>(FindObjectsSortMode.None))
        {
            uint netId = obj.GetComponent<NetworkIdentity>().netId;

            if (obj.GetComponent<UnitClass>() && !obj.GetComponent<UnitClass>().data.Equals(default(UnitStats)))
                CmdUpdateUnitStats(netId, obj.GetComponent<UnitClass>().data.health);

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
            UnitClass unitClass = identity.GetComponent<UnitClass>();
            UnitClient unitClient = identity.GetComponent<UnitClient>();

            if (unitClient != null && unitClass != null)
            {
                unitClass.health = health;

                UnitStats updatedStats = unitClient.data;
                updatedStats.health = health;

                unitClient.data = updatedStats; // This triggers SyncVar hook
            }

        }
    }

    [Command]
    public void CmdSpawnBases(int team, Vector3 pos)
    {
        GameObject Base = Instantiate(prefabs[prefabs.Count - (1 + team)], pos, prefabs[prefabs.Count - (1 + team)].transform.rotation);

        //Base.transform.localScale = Vector3.one;

        NetworkServer.Spawn(Base);

        if (team == 0)
            NetworkServer.SendToAll<Notification>(new Notification { text = $"assigned\nHex_{Mathf.RoundToInt(MatchSettings.instance.size.x / 2)}_0\n0" });
        if (team == 1)
            NetworkServer.SendToAll<Notification>(new Notification { text = $"assigned\nHex_{-Mathf.RoundToInt(MatchSettings.instance.size.x / 2)}_0\n1" });
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