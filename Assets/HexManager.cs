using System;
using System.Collections.Generic;
using UnityEngine;

public class HexManager : MonoBehaviour
{
    public static HexManager instance;

    public Dictionary<Vector2, TileClass> Hexes;

    internal void Start()
    {
        instance = this;
        Hexes = new Dictionary<Vector2, TileClass>();

        //EVERYONE GENERATES MAP NOW THAT WE HAVE A SEED TO KEEP CONSTANT MAP

        //if(MatchSettings.instance.hosting)
        //{
            HexGenerator generator = GetComponent<HexGenerator>();

            generator.gridWidth = Mathf.RoundToInt(MatchSettings.instance.size.x);
            generator.gridHeight = Mathf.RoundToInt(MatchSettings.instance.size.y);

            generator.mountainChance = MatchSettings.instance.MountainChance;
            generator.hillChance = MatchSettings.instance.HillChance;
            generator.waterChance = MatchSettings.instance.WaterChance;

            generator.GenerateHexGrid();
        //}
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    internal Vector3 SnapToHexGrid(Vector3 position, float hexSize)
    {
        Vector2 axial = WorldToHex(position, hexSize);
        return HexToWorld(axial, hexSize);
    }

    internal int HexDistance(Vector2 a, Vector2 b)
    {
        int dx = (int)(a.x - b.x);
        int dy = (int)(a.y - b.y);
        int dz = -dx - dy;
        return (Mathf.Abs(dx) + Mathf.Abs(dy) + Mathf.Abs(dz)) / 2;

    }

    //Get the neighbour tiles
    public List<Vector2> GetNeighbors(Vector2 coord)
    {
        List<Vector2> neighbors = new List<Vector2>();
        Vector2[] directions = new Vector2[]
        {
        new Vector2(1, 0), new Vector2(1, -1), new Vector2(0, -1),
        new Vector2(-1, 0), new Vector2(-1, 1), new Vector2(0, 1)
        };

        foreach (var dir in directions)
        {
            Vector2 neighborCoord = coord + dir;
            if (Hexes.ContainsKey(neighborCoord))
                neighbors.Add(neighborCoord);
        }

        return neighbors;
    }


    internal Vector2 HexRound(Vector2 axial)
    {
        float x = axial.x;
        float y = axial.y;
        float z = -x - y;

        int rx = Mathf.RoundToInt(x);
        int ry = Mathf.RoundToInt(y);
        int rz = Mathf.RoundToInt(z);

        float x_diff = Mathf.Abs(rx - x);
        float y_diff = Mathf.Abs(ry - y);
        float z_diff = Mathf.Abs(rz - z);

        if (x_diff > y_diff && x_diff > z_diff)
            rx = -ry - rz;
        else if (y_diff > z_diff)
            ry = -rx - rz;
        else
            rz = -rx - ry;

        return new Vector2(rx, ry);
    }

    internal Vector2 WorldToHex(Vector3 position, float hexSize)
    {
        float q = (Mathf.Sqrt(3f) / 3f * position.x - 1f / 3f * position.z) / hexSize;
        float r = (2f / 3f * position.z) / hexSize;
        return HexRound(new Vector2(q, r));
    }
    internal Vector3 HexToWorld(Vector2 axial, float hexSize)
    {
        float x = hexSize * Mathf.Sqrt(3f) * (axial.x + axial.y / 2f);
        float z = hexSize * 3f / 2f * axial.y;
        return new Vector3(x, 0, z); // Assuming y = 0 for ground level
    }

    internal void MovedToHex(Vector2 hexPosition, GameObject unit)
    {
        Hexes[hexPosition].Occupy(unit);
    }

    //Called when units are first made if they spawn on a unoccupiable space, just clear it for now
    internal void ForceMove(Vector2 hexPosition, GameObject unit)
    {
        Destroy(Hexes[hexPosition].Occupied);
        Hexes[hexPosition].Occupy(unit);
    }

    public TileClass FindHex(Vector3 pos, float hexSize)
    {
        Vector2 coord = WorldToHex(pos, hexSize);
        return Hexes[coord];
    }

    internal bool CheckTeam(GameObject selected, TileClass tile)
    {

        if (tile.Occupied)
        {
            if (tile.Occupied.GetComponent<UnitClass>() == null || selected.GetComponent<UnitClass>() == null)
                return false;

            return selected.GetComponent<UnitClass>().team == tile.Occupied.GetComponent<UnitClass>().team;
        }
        if (tile.Building)
        {
            return selected.GetComponent<UnitClass>().team == tile.Building.GetComponent<UnitClass>().team;
        }



        return true;
    }
}
