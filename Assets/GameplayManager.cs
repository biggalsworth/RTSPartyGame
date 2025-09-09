using NUnit.Framework;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

        foreach (GameObject unit in SpawnQueue)
        {
            Vector3 spawnPos = unit.transform.position;
            int team = turn;

            Debug.Log("Spawning " + unit.name);

            ServerClient.instance.SpawnUnit(unit.transform.position, unit.transform.rotation, MatchSettings.instance.team, unit.GetComponent<UnitClient>().prefabID); // Send to server
            Destroy(unit);
        }

        SpawnQueue.Clear(); // Reset for next turn

        foreach (GameObject unit in DestroyQueue)
        {
            ServerClient.instance.DestroyUnit(unit); // Send to server
        }

        DestroyQueue.Clear(); // Reset for next turn


        foreach (var obj in GameObject.FindObjectsByType<UnitClient>(FindObjectsSortMode.None))
        {
            if (obj.GetComponent<UnitClass>() && obj.GetComponent<UnitClass>().team == turn)
            {
                obj.gameObject.GetComponent<UnitClient>().ApplyTurnUpdate(HexManager.instance.SnapToHexGrid(obj.transform.position, 2.0f));
                obj.gameObject.GetComponent<UnitClass>().NewTurn();
            }
            if(obj.GetComponent<BuildingClass>() && obj.GetComponent<BuildingClass>().team == turn)
            {
                obj.gameObject.GetComponent<UnitClient>().ApplyTurnUpdate(HexManager.instance.SnapToHexGrid(obj.transform.position, 2.0f));
            }
        }

        turn = turn == 0 ? 1 : 0;

        ServerClient.instance.MessageServer("turn\n" + turn);
        GameObject.Find("TurnText").GetComponent<TextMeshProUGUI>().text = "Turn: " + turn;

    }

    internal void AddDestroy(GameObject gameObject)
    {
        DestroyQueue.Add(gameObject);
    }
}
