using UnityEngine;

public enum TileTypes
{
    Flat,
    Mountain,
    Water,
    Hill,
    Building
}

public class TileClass
{
    Vector2 ID;
    internal GameObject Tile;
    internal TileTypes TileType;
    internal GameObject Building;
    internal GameObject Occupied;
    internal bool standable = true;

    public TileClass(Vector2 _ID, GameObject _tile)
    {
        ID = _ID;
        Tile = _tile;
        Occupied = null;
    }

    public void Occupy(GameObject obj)
    {
        Occupied = obj;
    }
    public void Build(GameObject obj)
    {
        Building = obj;
        TileType = TileTypes.Building;
    }
    public void UnOccupy()
    {
        Occupied = null;
    }

    public bool UnitOccupies()
    {
        if(Occupied && Occupied.GetComponent<UnitClass>())
        {
            return true;
        }
        return false;
    }

    //return a code to give more detail on occupying options. 0 = can occupy, 1 = obstacle is here, 2 = a unit is here, 3 = a building is here
    public int CheckOccupy()
    {
        if(standable == false || TileType == TileTypes.Mountain || TileType == TileTypes.Hill || TileType == TileTypes.Water)
            return 1;

        if (Occupied == null && Building == null)
        {
            return 0;
        }
        else if(Occupied && Occupied.GetComponent<UnitClass>() && TileType != TileTypes.Building)
        {
            return 2;
        }
        else if(TileType == TileTypes.Building || (Building && Building.GetComponent<UnitClass>()) )
        {
            return 3;
        }
        Debug.Log("Occupied by unknown check: " + Occupied.name);
        return 1; //something is wrong, do not move here
    }

    public int TraverseCost()
    {
        if (standable == false || Occupied != null && Occupied.GetComponent<UnitClass>() && TileType != TileTypes.Building)
            return -1;// Cannot traverse

        if (Building != null && Building.GetComponent<UnitClass>().team != MatchSettings.instance.team)
            return -1;// Cannot traverse

        if (TileType == TileTypes.Mountain)
            return -1;// Cannot traverse

        if (TileType == TileTypes.Hill)
            return 2;

        if (TileType == TileTypes.Water)
            return 3;

        else
            return 1;

    }
}
