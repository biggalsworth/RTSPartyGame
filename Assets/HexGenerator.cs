using System;
using System.Collections.Generic;
using UnityEngine;

public static class TerrainUtils
{
    public static Dictionary<TileTypes, int> CreateFeatureBiasMap()
    {
        var biasMap = new Dictionary<TileTypes, int>();
        foreach (TileTypes type in Enum.GetValues(typeof(TileTypes)))
        {
            biasMap[type] = 0;
        }
        return biasMap;
    }

    public static Dictionary<TileTypes, int> GetBaseRarityMap(int mountainRarity, int hillRarity, int waterRarity)
    {
        return new Dictionary<TileTypes, int>
        {
            { TileTypes.Mountain, mountainRarity },
            { TileTypes.Hill, hillRarity },
            { TileTypes.Water, waterRarity }
        };
    }


}


public class HexGenerator : MonoBehaviour
{
    public GameObject hexPrefab; // Assign a hex tile prefab in the inspector
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float hexSize = 2f;

    [SerializeField]
    [Header("Feature Generation")]
    public int mountainChance = 10;
    public int waterChance = 5;
    public int hillChance = 5;

    private System.Random randomiser;

    Dictionary<TileTypes, int> featureBias;
    Dictionary<TileTypes, int> baseRarityMap;

    public GameObject TeamA;
    public GameObject TeamB;

    internal GameObject BaseA;
    internal GameObject BaseB;

    public GameObject flatModel;
    public GameObject mountainModel;
    public GameObject hillModel;
    public GameObject waterModel;

    //called from hex manager
    internal void GenerateHexGrid()
    {
        randomiser = new System.Random(MatchSettings.instance.MapSeed);

        baseRarityMap = TerrainUtils.GetBaseRarityMap(mountainChance, waterChance, hillChance);

        for (int q = -gridWidth; q <= gridWidth; q++)
        {
            int r1 = Mathf.Max(-gridHeight, -q - gridHeight);
            int r2 = Mathf.Min(gridHeight, -q + gridHeight);
            for (int r = r1; r <= r2; r++)
            {
                Vector3 pos = HexToWorld(new Vector2(q, r), hexSize);
                GameObject hexGO;


                if (q == Mathf.RoundToInt(gridWidth / 2) && r == 0)
                {
                    hexGO = Instantiate(flatModel, pos, TeamB.transform.rotation, transform);

                    BaseA = Instantiate(TeamA, pos, TeamA.transform.rotation, hexGO.transform);
                    BaseA.transform.localScale = Vector3.one;

                    GenerateTile(hexGO, new Vector2(q, r), pos, true);
                }
                else if (q == -Mathf.RoundToInt(gridWidth / 2) && r == 0)
                {
                    hexGO = Instantiate(flatModel, pos, TeamB.transform.rotation, transform);

                    BaseB = Instantiate(TeamB, pos, TeamB.transform.rotation, hexGO.transform);
                    BaseB.transform.localScale = Vector3.one;

                    GenerateTile(hexGO, new Vector2(q, r), pos, true);
                }
                else
                {
                    hexGO = Instantiate(hexPrefab, pos, Quaternion.identity, transform);
                    //generate tile data
                    GenerateTile(hexGO, new Vector2(q, r), pos);
                }
                hexGO.name = $"Hex_{q}_{r}";


            }
        }
    }

    //This will do the checks for the tile terrain type
    private void GenerateTile(GameObject tile, Vector2 Coord, Vector3 tilePos, bool NoFeatures = false)
    {
        
        TileClass currTile = new TileClass(Coord, tile);

        if (NoFeatures)
        {
            HexManager.instance.Hexes.Add(Coord, currTile);
            return;
        }

        Dictionary<TileTypes, int> biasMap = GetNeighbourBias(Coord);
        baseRarityMap = TerrainUtils.GetBaseRarityMap(mountainChance, hillChance, waterChance);

        //default as a flat terrain
        TileTypes chosenType = TileTypes.Flat;
        int highestFinalChance = 0; //what the current highest bias is

        //check the base rarity of each feature 
        foreach (var kvp in baseRarityMap)
        {
            //get the current terrain type to be compared and the rarity
            TileTypes type = kvp.Key;
            int baseChance = kvp.Value;

            if (baseChance == 0)
                continue;

            //Gets the bias for this terrain type based on nearby tiles. If no bias found, default to 0
            int bias = biasMap.ContainsKey(type) ? biasMap[type] : 0;

            //Combine the base rarit and the bias to find the final chance of this terrain type
            int finalChance = Mathf.Clamp(baseChance + bias, 1, 100);

            //Check for the current type to be generated
            //If the terrain rolls as generated, make this the new most likely terrain type
            //but this can be overwritten if another terrain with a higher bias might come later
            if (randomiser.Next(1, 101) <= finalChance && finalChance > highestFinalChance) //UnityEngine.Random.Range(1, 101), using system random now, is meant to be more efficient
            {
                chosenType = type;
                highestFinalChance = finalChance;
            }
        }

        Quaternion rot = Quaternion.Euler(0, randomiser.Next(0, 6) * 60, 0); //make a random rotation that still keeps hexagons aligned
        currTile.TileType = chosenType;

        if (chosenType == TileTypes.Flat)
        {
            GameObject obstacle = Instantiate(flatModel, tilePos, flatModel.transform.rotation, tile.transform);
        }
        else if (chosenType == TileTypes.Mountain)
        {
            GameObject obstacle = Instantiate(mountainModel, tilePos, mountainModel.transform.rotation * rot, tile.transform);
            currTile.Occupy(obstacle);
        }
        else if (chosenType == TileTypes.Hill)
        {
            GameObject obstacle = Instantiate(hillModel, tilePos, hillModel.transform.rotation, tile.transform);
            currTile.Occupy(obstacle);
        }
        else if (chosenType == TileTypes.Water)
        {
            GameObject obstacle = Instantiate(waterModel, tilePos, waterModel.transform.rotation, tile.transform);
            currTile.Occupy(obstacle);
        }



        /*Old way of doing feature generation

            if (Random.Range(1, 101) <= finalChance)
            {
                currTile.TileType = TileTypes.Mountain;
                tilePos.y = 2;
                GameObject obstacle = Instantiate(mountainModel, tilePos, mountainModel.transform.rotation, tile.transform);
                currTile.Occupy(obstacle);

            }
            else
            {
                currTile.TileType = TileTypes.Flat;
            }
            */

        HexManager.instance.Hexes.Add(Coord, currTile);
    }

    //checks neighbours and varies the bias for certain features more depending
    private Dictionary<TileTypes, int> GetNeighbourBias(Vector2 coord)
    {
        var biasMap = TerrainUtils.CreateFeatureBiasMap();

        Vector2[] directions = new Vector2[]
        {
        new Vector2(1, 0), new Vector2(1, -1), new Vector2(0, -1),
        new Vector2(-1, 0), new Vector2(-1, 1), new Vector2(0, 1)
        };

        //Check each neighbours terrain type
        //Increase chances of neighbours terrain type
        foreach (var dir in directions)
        {
            Vector2 neighborCoord = coord + dir;
            if (HexManager.instance.Hexes.TryGetValue(neighborCoord, out TileClass neighbor))
            {
                //If our neighbour is water, increase water bias by 1
                if (biasMap.ContainsKey(neighbor.TileType))
                    biasMap[neighbor.TileType] += 1; //bias for this terrain type go up
            }
        }

        return biasMap;
    }

    /* basic feature bias check 
    private float GetFeatureBias(Vector2 coord)
    {
        float bias = 0;
        Vector2[] directions = new Vector2[]
        {
        new Vector2(1, 0), new Vector2(1, -1), new Vector2(0, -1),
        new Vector2(-1, 0), new Vector2(-1, 1), new Vector2(0, 1)
        };

        foreach (var dir in directions)
        {
            Vector2 neighborCoord = coord + dir;
            if (HexManager.instance.Hexes.TryGetValue(neighborCoord, out TileClass neighbor))
            {
                if (neighbor.TileType == TileTypes.Mountain)
                {
                    bias += 1f; // Increase chance if neighbor is a mountain
                }
                else
                {
                    bias -= 1f; // Slightly decrease if neighbor is flat
                }
            }
        }

        return bias;
    }
    */

    Vector3 HexToWorld(Vector2 axial, float size)
    {
        float x = size * Mathf.Sqrt(3f) * (axial.x + axial.y / 2f);
        float z = size * 3f / 2f * axial.y;
        return new Vector3(x, 0, z);
    }


}
