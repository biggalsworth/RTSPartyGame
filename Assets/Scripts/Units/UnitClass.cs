using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
public enum UnitTypes
{
    Footman,
    Knight,
    Building
}


public class UnitClass : MonoBehaviour
{
    internal UnitTypes UnitType;

    public string UnitName;

    public GameObject Mesh;

    internal bool busy = false;

    public int cost;

    public int team;
    public int moveDistance = 1;
    private bool attacked;
    public int movesLeft;
    bool stopMove = false;

    public int maxHealth = 10;
    internal int health;

    public int defenceRating = 2;
    public int offenceRating = 4;

    public int attackRange = 1;
    public int damage = 1;

    internal CombatData data;

    NavMeshAgent agent;

    internal Vector2 HexPosition;
    internal bool defending = false;
    internal bool attacking = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        //agent.isStopped = true;

        //movesLeft = moveDistance;
        movesLeft = 0;
        attacked = true;
        busy = false;
    }

    internal void Start()
    {
        //team = MatchSettings.instance.team;

        HexPosition = HexManager.instance.WorldToHex(transform.position, 2f);
        transform.position = HexManager.instance.HexToWorld(HexPosition, 2f);

        //HexManager.instance.ForceMove(HexPosition, gameObject);
        HexManager.instance.Hexes[HexPosition].Occupy(gameObject);

        Debug.Log(gameObject.name + "IS OCCUPYING TILE " + HexPosition);
        Debug.Log(HexPosition + " IS CODE " + HexManager.instance.Hexes[HexPosition].CheckOccupy());

        health = maxHealth;
        data = new CombatData(team, health, defenceRating, offenceRating, damage, attackRange);
        data.owner = gameObject;

        UnitStart();
    }

    public virtual void UnitStart() { }

    // Update is called once per frame
    void Update()
    {
        //if (GameObject.FindWithTag("Player") && GameObject.FindWithTag("Player").GetComponent<PlayerInteractions>().busy == false && busy)
        //    GameObject.FindWithTag("Player").GetComponent<PlayerInteractions>().busy = true;

        UnitUpdate();

        health = data.health;

        if (data.health <= 0)
        {
            Debug.Log(gameObject.name + " IS DEAD");
            GameplayManager.instance.AddDestroy(gameObject);
            gameObject.SetActive(false);
        }
    }
    private void OnDisable()
    {
        HexManager.instance.Hexes[HexPosition].UnOccupy();
        UnitDestroy();
    }
    void OnDestroy()
    {
        HexManager.instance.Hexes[HexPosition].UnOccupy();
        UnitDestroy();
    }

    public virtual void UnitUpdate() { }
    public virtual void UnitDestroy() { }



    public void Selected(bool active)
    {
        if(UnitType == UnitTypes.Building)
            return;

        if(active)
        {
            if (HexManager.instance.Hexes[HexPosition].TileType == TileTypes.Building)
            {
                HexManager.instance.Hexes[HexPosition].Building.GetComponent<UnitClass>().Mesh.SetActive(false);
            }

            if (Mesh.activeSelf == false)
            {
                Mesh.SetActive(true);
                //Mesh.GetComponent<MeshRenderer>().enabled = true;
            }
        }
        else
        {
            if (HexManager.instance.Hexes[HexPosition].TileType == TileTypes.Building && Mesh.activeSelf == true)
            {
                Mesh.SetActive(false);
            }

            if (HexManager.instance.Hexes[HexPosition].TileType == TileTypes.Building)
            {
                HexManager.instance.Hexes[HexPosition].Building.GetComponent<UnitClass>().Mesh.SetActive(true);
            }
        }
    }

    public void NewTurn()
    {
        attacked = false;
        movesLeft = moveDistance;
    }

    public void Move(Vector3 dest, int cost)
    {
        agent.isStopped = false;
        dest.y = 1;
        agent.SetDestination(dest);

        movesLeft -= cost;
    }

    internal bool AttemptMove(Vector3 position, int spacesMoved)
    {
        Vector2 HexPos = HexManager.instance.WorldToHex(position, 2f);
        int tileStatus = HexManager.instance.Hexes[HexPos].CheckOccupy();
        if (tileStatus == 0)
        {
            Move(position, spacesMoved);
            HexManager.instance.Hexes[HexPos].Occupy(gameObject);
            return true;
        }
        else if(tileStatus == 2)
        {
            if (HexManager.instance.Hexes[HexPos].Occupied.GetComponent<UnitClass>().team != team)
            {
                Debug.Log("BATTLE");
                return false;
            }
            else
            {
                Debug.Log("Friendly Unit Already Occupies");
                return false;
            }
        }
        return false;
    }

    //Custom Pathfinding 
    public void MoveAlongPath(List<Vector2> path)
    {
        StartCoroutine(FollowPath(path));
    }
    private IEnumerator FollowPath(List<Vector2> path)
    {
        stopMove = false;
        busy = true;
        Vector2 hex;
        for (int i = 1; i < path.Count; i++)
        {
            hex = path[i];
            Vector3 worldPos = HexManager.instance.HexToWorld(hex, 2f);
            worldPos.y = 1;

            HexManager.instance.Hexes[HexPosition].UnOccupy();

            agent.SetDestination(worldPos);
            while (Vector3.Distance(transform.position, worldPos) > 0.175f)
            {
                yield return new WaitForSeconds(0.05f);

                HexPosition = hex;
                //HexPosition = HexManager.instance.WorldToHex(worldPos, 2.0f);

                //Is currently location a building
                if (HexManager.instance.Hexes[HexPosition].TileType == TileTypes.Building)
                {
                    //Mesh.GetComponent<MeshRenderer>().enabled = false;
                    Mesh.SetActive(false);

                    if (HexManager.instance.Hexes[HexPosition].TileType == TileTypes.Building)
                    {
                        HexManager.instance.Hexes[HexPosition].Building.SetActive(true);
                    }
                }
                else
                {
                    Mesh.SetActive(true);
                    //GetComponent<MeshRenderer>().enabled = true;
                }
            }

            movesLeft -= Mathf.Clamp(HexManager.instance.Hexes[HexPosition].TraverseCost(), 0, 5);

            HexManager.instance.Hexes[HexPosition].Occupy(gameObject);

            if(stopMove)
            {
                busy = false;
                stopMove = false;
                break;
            }    
        }
        //movesLeft -= path.Sum(h => HexManager.instance.Hexes[h].TraverseCost());

        busy = false;

    }


    public bool AttemptMoveAlongPath(List<Vector2> path, int cost)
    {
        if (UnitType == UnitTypes.Building)
            return true;

        if (path == null || path.Count == 0)
        {
            Debug.LogWarning("Attempted to move along an empty or null path.");
            return false;
        }

        if (movesLeft <= 0)
            return false;

        Vector2 finalHex = path[path.Count - 1];

        Debug.Log("Attempting to move to final hex: " + finalHex);

        int tileStatus = HexManager.instance.Hexes[finalHex].CheckOccupy();

        Debug.Log("Tile status: " + tileStatus);

        if (tileStatus == 0 || tileStatus == 1 || tileStatus == 3)
        {
            Selected(false);

            MoveAlongPath(path);

            return true;
        }
        else if (tileStatus == 2)
        {
            if (HexManager.instance.Hexes[finalHex].Occupied.GetComponent<UnitClass>().team != team)
            {
                //Debug.Log("BATTLE");
                return false;
            }
            else
            {
                //Debug.Log("Friendly Unit Already Occupies");
                return false;
            }
        }

        return false;
    }

    internal void SeenNewEnemy()
    {
        if(busy)
            stopMove = true;
    }

    internal bool Battle(UnitClass target)
    {
        Debug.Log("Attackingggg");
        Debug.Log(team + " | " + target.team);
        if (target.team == team)
            return false;

        //make sure we aren't attacking a second time
        if (attacked)
            return false;

        attacked = true;

        if (HexManager.instance.HexDistance(HexPosition, target.HexPosition) <= attackRange)
        {
            Debug.Log("Can Attack");

            target.StartCoroutine(target.LookAt(transform.position));
            StartCoroutine(LookAt(target.transform.position));
            data.SimulateBattle(target.data);
            return true;
        }
        else
            Debug.Log("Target outside attack range");

        return false;

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

    public IEnumerator LookAt(Vector3 target)
    {
        agent.updateRotation = false;

        while (true)
        {
            Vector3 direction = (target - transform.position).normalized;
            direction.y = 0; // Keep rotation flat

            if (direction.magnitude < 0.01f)
                break; // Already facing or too close

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);

            // Optional: break when close enough to target rotation
            if (Quaternion.Angle(transform.rotation, targetRotation) < 1f)
                break;

            yield return null;
        }

        agent.updateRotation = true;
    }
}
