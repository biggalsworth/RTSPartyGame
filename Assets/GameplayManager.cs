using Mirror;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class GameplayManager : MonoBehaviour
{
    public static GameplayManager instance;

    public string GameStage = "Build";

    public int turn = 0; //turn 0 for team A turn 1 for team B

    public List<GameObject> SpawnQueue = new List<GameObject>();
    public List<GameObject> DestroyQueue = new List<GameObject>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        instance = this;

        turn = 0;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void NewUnit(GameObject unit)
    {
        SpawnQueue.Add(unit);
    }

    public void NextTurn()
    {
        //if(ServerHost.instance == null)
        //{
        //    Debug.Log("Not Online");
        //    return;
        //}
        if(MatchSettings.instance.team != turn)
        {
            return;
        }
        List<GameObject> workingQueue = SpawnQueue;
        //foreach (GameObject unit in SpawnQueue)
        for (int i = SpawnQueue.Count - 1; i >= 0; i--)
        {
            GameObject unit = SpawnQueue[i];
            Vector3 spawnPos = unit.transform.position;
            Quaternion spawnRot = unit.transform.rotation;
            int team = unit.gameObject.GetComponent<UnitClass>().team;
            int prefabID = unit.GetComponent<UnitClient>().prefabID;

            Debug.Log("Spawning " + unit.name);

            //DestroyQueue.Add(unit);

            //if (NetworkClient.active && !NetworkServer.active)
            //{
            //    ServerClient.instance.SpawnUnit(spawnPos, spawnRot, team, prefabID); // Send to server
            //}

            //Debug.Log("LOCAL PLAYER: " + NetworkClient.localPlayer);
            //
            //NetworkRelay messageRelay = NetworkClient.localPlayer.GetComponent<NetworkRelay>();
            //
            //if (messageRelay != null)
            //{
            //    messageRelay.CmdSpawnUnit(spawnPos, spawnRot, team, prefabID);
            //}
            //else
            //{
            //    Debug.LogWarning("NetworkRelay not found on player.");
            //}

            if (NetworkServer.active)
            {
                // Host is server -> spawn directly, no command
                GameObject newUnit = Instantiate(NetworkManager.singleton.spawnPrefabs[prefabID], spawnPos, spawnRot);

                newUnit.GetComponent<UnitClass>().team = team;
                newUnit.GetComponent<UnitClient>().team = team;
                //unit.GetComponent<UnitClass>().data.team = team;


                NetworkServer.Spawn(newUnit);

                Debug.Log($"[Server] Spawned unit: {newUnit.name} at {spawnPos}");
            }
            else
            {
                // Non-host client -> send command to server
                NetworkClient.localPlayer.GetComponent<NetworkRelay>().CmdSpawnUnit(spawnPos, spawnRot, team, prefabID);
            }

            Destroy(unit);
            SpawnQueue.RemoveAt(i);
        }

        SpawnQueue.Clear(); // Reset for next turn

        foreach (GameObject unit in DestroyQueue)
        {
            ServerClient.instance.DestroyUnit(unit); // Send to server
        }

        DestroyQueue.Clear(); // Reset for next turn


        //foreach (var obj in GameObject.FindObjectsByType<UnitClient>(FindObjectsSortMode.None))
        //{
        //    if (obj.GetComponent<UnitClass>() && obj.GetComponent<UnitClass>().team == turn)
        //    {
        //        obj.gameObject.GetComponent<UnitClient>().ApplyTurnUpdate(HexManager.instance.SnapToHexGrid(obj.transform.position, 2.0f));
        //        obj.gameObject.GetComponent<UnitClass>().NewTurn();
        //    }
        //    if(obj.GetComponent<BuildingClass>() && obj.GetComponent<BuildingClass>().team == turn)
        //    {
        //        obj.gameObject.GetComponent<UnitClient>().ApplyTurnUpdate(HexManager.instance.SnapToHexGrid(obj.transform.position, 2.0f));
        //    }
        //}

        //ServerClient.instance.messageRelay.ApplyMovements(turn);

        NetworkRelay relay = NetworkClient.localPlayer.GetComponent<NetworkRelay>();
        relay.ApplyMovements(turn);

        turn = turn == 0 ? 1 : 0;

        ServerClient.instance.MessageServer("turn\n" + turn);
        GameObject.Find("TurnText").GetComponent<TextMeshProUGUI>().text = "Turn: " + turn;

    }

    IEnumerator DestroyAfterSpawn(GameObject placeholder)
    {
        yield return new WaitForSeconds(0.5f); // Adjust based on spawn timing
        Destroy(placeholder);
    }

    internal void AddDestroy(GameObject gameObject)
    {
        DestroyQueue.Add(gameObject);
    }
}
