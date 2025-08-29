using System;
using UnityEngine;

public class BuildingClass : MonoBehaviour
{
    public GameObject Model;

    public GameObject building;

    internal Vector2 HexPosition;

    public int cost = 50;

    public int team;
    public int MaxHealth = 20;
    internal int health;

    public int defenceRating = 5;
    public int offenceRating = 2;

    public int damage = 3;
    public int attackRange = 3;

    internal CombatData data;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        health = MaxHealth;

        data = new CombatData(team, health, defenceRating, offenceRating, damage, attackRange);
        data.owner = this.gameObject;

        HexPosition = HexManager.instance.WorldToHex(transform.position, 2f);
        transform.position = HexManager.instance.HexToWorld(HexPosition, 2f);
        //HexManager.instance.Hexes[HexPosition].Occupy(gameObject);
        //HexManager.instance.Hexes[HexPosition].TileType = TileTypes.Building;
    }

    // Update is called once per frame
    void Update()
    {
        if (data.health <= 0)
        {
            GameplayManager.instance.AddDestroy(gameObject);
            gameObject.SetActive(false);
        }
    }

    internal void Battle(UnitClass target)
    {
        Debug.Log("Attackingggg");
        Debug.Log(team + " | " + target.team);
        if (target.team == team)
            return;

        if (HexManager.instance.HexDistance(HexPosition, target.HexPosition) <= attackRange)
        {
            Debug.Log("Can Attack");
            data.SimulateBattle(target.data);
        }
        else
            Debug.Log("Target outside attack range");

    }
    internal void Battle(BuildingClass target)
    {
        Debug.Log("Attackingggg");
        Debug.Log(team + " | " + target.team);
        if (target.team == team)
            return;

        if (HexManager.instance.HexDistance(HexPosition, target.HexPosition) <= attackRange)
        { 
            Debug.Log("Can Attack");
            data.SimulateBattle(target.data);
        }
        else
            Debug.Log("Target outside attack range");

    }

    void SimulateBattle()
    {

    }
}
