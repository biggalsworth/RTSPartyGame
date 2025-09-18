using System.Collections.Generic;
using UnityEngine;

public class UnitDetection : MonoBehaviour
{
    public int detectionRange = 3;
    GameObject currCheck;
    UnitClass currUnit;

    HashSet<GameObject> seenUnits = new HashSet<GameObject>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (UnitClass unit in GameObject.FindObjectsByType<UnitClass>(FindObjectsSortMode.None))
        {
            if (unit.team != MatchSettings.instance.team)
            {
                unit.Mesh.SetActive(false);
                HexManager.instance.FindHex(unit.transform.position, 2.0f).UnOccupy();
                if(HexManager.instance.FindHex(unit.transform.position, 2.0f).TileType == TileTypes.Building)
                {
                    HexManager.instance.FindHex(unit.transform.position, 2.0f).standable = true;
                    HexManager.instance.FindHex(unit.transform.position, 2.0f).Building = null;
                    HexManager.instance.FindHex(unit.transform.position, 2.0f).TileType = TileTypes.Flat;
                }
            }
        }
    }

    private void OnDestroy()
    {
        foreach (GameObject unit in seenUnits)
        {
            unit.GetComponent<UnitClass>().Mesh.SetActive(false);

            HexManager.instance.FindHex(unit.transform.position, 2.0f).UnOccupy();
            if (HexManager.instance.FindHex(unit.transform.position, 2.0f).TileType == TileTypes.Building)
            {
                HexManager.instance.FindHex(unit.transform.position, 2.0f).standable = true;
                HexManager.instance.FindHex(unit.transform.position, 2.0f).Building = null;
                HexManager.instance.FindHex(unit.transform.position, 2.0f).TileType = TileTypes.Flat;
            }
            
        }
    }

    // Update is called once per frame
    void Update()
    {
        HashSet<GameObject> currentlySeen = new HashSet<GameObject>();

        try //avoid calls if empty/no units are detected
        {
            foreach (Collider hit in Physics.OverlapSphere(transform.position, 20))
            {
                if (HexManager.instance.HexDistance(
                    HexManager.instance.WorldToHex(transform.position, 2.0f), HexManager.instance.WorldToHex(hit.transform.position, 2.0f))
                    > detectionRange)
                {
                    continue;
                }

                currCheck = hit.gameObject;
                currUnit = null;

                if (currCheck.GetComponent<UnitClass>())
                    currUnit = currCheck.GetComponent<UnitClass>();

                if (currUnit != null && currUnit.team != GetComponent<UnitClass>().team)
                {
                    if (currUnit.Mesh.activeSelf == false)
                    {
                        currUnit.Mesh.SetActive(true);
                        if (currCheck.GetComponent<BuildingUnit>())
                        {
                            HexManager.instance.FindHex(currUnit.transform.position, 2.0f).Build(currCheck);
                        }
                        else
                        {
                            HexManager.instance.FindHex(currUnit.transform.position, 2.0f).Occupy(currCheck);
                        }

                        HexManager.instance.FindHex(currUnit.transform.position, 2.0f).standable = currUnit.standable;

                        Debug.Log("Seen new enemy");
                        GetComponent<UnitClass>().SeenNewEnemy();
                    }

                    currentlySeen.Add(currCheck);
                }
            }

            //check previously seen
            foreach (GameObject unit in seenUnits)
            {
                if (!currentlySeen.Contains(unit))
                {
                    unit.GetComponent<UnitClass>().Mesh.SetActive(false);

                    HexManager.instance.FindHex(unit.transform.position, 2.0f).UnOccupy();
                    if (HexManager.instance.FindHex(unit.transform.position, 2.0f).TileType == TileTypes.Building)
                    {
                        HexManager.instance.FindHex(unit.transform.position, 2.0f).standable = true;
                        HexManager.instance.FindHex(unit.transform.position, 2.0f).Building = null;
                        HexManager.instance.FindHex(unit.transform.position, 2.0f).TileType = TileTypes.Flat;
                    }
                }
            }

            seenUnits = currentlySeen;
        }
        catch { };
    }

}
