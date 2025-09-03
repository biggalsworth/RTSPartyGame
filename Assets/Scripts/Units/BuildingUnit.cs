using UnityEngine;
using System;
using UnityEngine.AI;

public class BuildingUnit : UnitClass
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void UnitStart()
    {
        UnitType = UnitTypes.Building;

        HexPosition = HexManager.instance.WorldToHex(transform.position, 2f);
        transform.position = HexManager.instance.HexToWorld(HexPosition, 2f);
        HexManager.instance.Hexes[HexPosition].UnOccupy();
        HexManager.instance.Hexes[HexPosition].Build(gameObject);
        HexManager.instance.Hexes[HexPosition].standable = standable;
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

    public override void UnitDestroy()
    {
        HexManager.instance.Hexes[HexPosition].standable = true;
        HexManager.instance.Hexes[HexPosition].Building = null;
        HexManager.instance.Hexes[HexPosition].TileType = TileTypes.Flat;
    }

}
