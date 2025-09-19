using Mirror;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer.Internal;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static UnityEngine.UI.CanvasScaler;

public class PlayerInteractions : MonoBehaviour
{
    RaycastHit hit;

    public int team;
    public int budget;

    ControlScheme controls;

    public bool busy;

    bool building = false;
    internal GameObject currBuild = null;
    Quaternion buildRot = new Quaternion(0,0,0,0);

    Vector2 teamTile;
    internal GameObject selected;
    int ground_layer;

    public GameObject unavailableIcon;
    public GameObject MoveIcon;
    public GameObject SelectedIcon;
    public LineRenderer pathLine;

    List<Vector2> path = null;
    List<Vector2> reachablePath = new List<Vector2>();

    public GameObject BuildPreview;
    public Material canBuild;
    public Material cannotBuild;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        busy = false;

        //ground_layer = LayerMask.NameToLayer("Ground");
        ground_layer = LayerMask.GetMask("Ground");
        controls = Controls.instance.input;
        controls.Mouse.Click.performed += CheckClick;
        controls.Player.Rotate.performed += RotateBuild;

        team = MatchSettings.instance.team;
        budget = MatchSettings.instance.Budget;

        unavailableIcon.SetActive(false);
        MoveIcon.SetActive(false);
        pathLine.enabled = false;

        buildRot = Quaternion.identity;

        teamTile = new Vector2(Mathf.RoundToInt(MatchSettings.instance.size.x / 2), 0);
        if(team == 1)
            teamTile.x = -teamTile.x;

    }

    private void OnEnable()
    {
        if(Controls.instance)
        {
            controls = Controls.instance.input;
            controls.Mouse.Click.performed += CheckClick;
            controls.Player.Rotate.performed += RotateBuild;
        }
    }

    private void OnDisable()
    {
        controls = Controls.instance.input;
        controls.Mouse.Click.performed -= CheckClick;
        controls.Player.Rotate.performed -= RotateBuild;
    }

    void ClearSelection()
    {
        if(currBuild != null)
            Destroy(currBuild);

        currBuild = null;
        building = false;

        if (selected != null && selected.GetComponent<UnitClass>())
            selected.GetComponent<UnitClass>().Selected(false);
        selected = null;

        SelectedIcon.SetActive(false);
        unavailableIcon.SetActive(false);
        MoveIcon.SetActive(false);
        pathLine.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(GameplayManager.instance.turn != team)
        {
            ClearSelection();
            return;
        }

        if (busy)
        {
            busy = false;
            foreach (UnitClass unit in GameObject.FindObjectsByType<UnitClass>(FindObjectsSortMode.None))
            {
                if (unit.team == team)
                {
                    if (unit.busy)
                    {
                        busy = true;
                    }
                }
            }
        }

        CheckInput();

        //CheckClick();

        CheckDrag();

        CheckStates();

        CheckHover();

    }

    Vector3 SnapToGrid(Vector3 position, float gridSize)
    {
        float x = Mathf.Round(position.x / gridSize) * gridSize;
        float y = Mathf.Round(position.y / gridSize) * gridSize;
        float z = Mathf.Round(position.z / gridSize) * gridSize;
        return new Vector3(x, y, z);
    }

    void CheckInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClearSelection();

            //currBuild = null; if (currBuild != null)
            //    Destroy(currBuild);
            //
            //building = false;
            //
            //if (selected != null && selected.GetComponent<UnitClass>())
            //    selected.GetComponent<UnitClass>().Selected(false);
            //selected = null;
        }
    }

    private void RotateBuild(InputAction.CallbackContext context)
    {
        Debug.Log("Before: " + buildRot);
        buildRot *= Quaternion.Euler(0, 30, 0); //increase the rotation by 1 hex angle
        BuildPreview.transform.rotation = buildRot;
        Debug.Log("After: " + buildRot);
    }

    void CheckClick(InputAction.CallbackContext context)
    {
        //if(Input.GetMouseButton(0) && EventSystem.current.IsPointerOverGameObject() == false)
        if(EventSystem.current.IsPointerOverGameObject() == false)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit, Mathf.Infinity))//, ~ground_layer))
            {
                bool HitGround = LayerMask.GetMask(LayerMask.LayerToName(hit.transform.gameObject.layer)) == ground_layer;

                //Debug.Log("Selected Tile : " + HexManager.instance.WorldToHex(hit.point, 2.0f) + "\n" + 
                //    "Occupied " + HexManager.instance.FindHex(hit.point, 2.0f).CheckOccupy());

                if (HexManager.instance.FindHex(hit.point, 2.0f).CheckOccupy() == 0)
                {
                    if (currBuild != null && currBuild.GetComponent<BuildingClass>().cost <= budget && HexManager.instance.HexDistance(teamTile, HexManager.instance.WorldToHex(hit.point, 2f)) <= MatchSettings.instance.size.x / 4)
                    {
                        budget -= currBuild.GetComponent<BuildingClass>().cost;

                        GameObject newObj = Instantiate(currBuild.GetComponent<BuildingClass>().building, HexManager.instance.SnapToHexGrid(hit.point, 2f), buildRot, HexManager.instance.FindHex(hit.point, 2f).Tile.transform);
                        
                        //ServerClient.instance.SpawnUnit(HexManager.instance.SnapToHexGrid(hit.point, 2f), buildRot, team, currBuild.GetComponent<BuildingClass>().building.GetComponent<UnitClient>().prefabID);
                       
                        if(newObj.GetComponent<UnitClass>())
                        {
                            newObj.GetComponent<UnitClass>().team = team;
                            newObj.GetComponent<UnitClient>().team = team;
                            newObj.transform.parent = null;
                        }
                        else if(newObj.GetComponent<BuildingClass>())
                        {
                            newObj.GetComponent<BuildingClass>().team = team;
                        }

                        GameplayManager.instance.NewUnit(newObj);
                        //if (NetworkClient.active && !NetworkServer.active)
                        //{
                        //    ServerClient.instance.SpawnUnit(HexManager.instance.SnapToHexGrid(hit.point, 2f), buildRot, team, currBuild.GetComponent<BuildingClass>().building.GetComponent<UnitClient>().prefabID); // Send to server
                        //}


                        ClearSelection();
                        selected = null;
                        currBuild = null;
                        building = false;
                        buildRot = Quaternion.identity;
                    }
                    if (selected != null)
                    {
                        if(selected.GetComponent<UnitClass>())
                            selected.GetComponent<UnitClass>().Selected(false);
                    }
                    selected = null;

                    return;
                }

                //Select a unit
                if ( 
                    HexManager.instance.FindHex(hit.point, 2.0f).Occupied != null && 
                    selected != HexManager.instance.FindHex(hit.point, 2.0f).Occupied && 
                    HexManager.instance.FindHex(hit.point, 2.0f).Occupied.GetComponent<UnitClass>().team == team
                    )
                {
                    ClearSelection();

                    selected = HexManager.instance.FindHex(hit.point, 2.0f).Occupied;
                    if (selected.GetComponent<UnitClass>().busy)
                        selected = null;
                    else
                        selected.GetComponent<UnitClass>().Selected(true);
                }
                else if (HexManager.instance.FindHex(hit.point, 2.0f).Building != null && HexManager.instance.FindHex(hit.point, 2.0f).Building.GetComponent<UnitClass>().team == team) //prioritise picking a unit over the building
                {
                    ClearSelection();

                    selected = HexManager.instance.FindHex(hit.point, 2.0f).Building;
                    if (selected.GetComponent<UnitClass>().busy)
                        selected = null;
                    else
                        selected.GetComponent<UnitClass>().Selected(true);
                }
                //else if(HexManager.instance.FindHex(hit.point, 2.0f).CheckOccupy() == 3 && HexManager.instance.FindHex(hit.point, 2.0f).Occupied.GetComponent<BuildingClass>().team == team)
                //{
                //    selected = HexManager.instance.FindHex(hit.point, 2.0f).Occupied;
                //}
                else
                {
                    if (selected != null)
                        selected.GetComponent<UnitClass>().Selected(false);

                    selected = null;
                }

            }
            else
            {
                if (selected != null)
                    selected.GetComponent<UnitClass>().Selected(false);

                selected = null;
            }
        }
    }


    void CheckHover()
    {
        if (building)
        {
            if (!BuildPreview.activeSelf)
            {
                BuildPreview.transform.localScale = currBuild.transform.localScale;
                BuildPreview.transform.rotation = currBuild.transform.rotation;
                buildRot = Quaternion.identity;
                //BuildPreview.GetComponent<MeshFilter>().mesh = currBuild.Model.GetComponent<MeshFilter>().sharedMesh;
                BuildPreview.SetActive(true);
            }


            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                BuildPreview.transform.position = HexManager.instance.SnapToHexGrid(hit.point, 2f);

                int tileCode = HexManager.instance.FindHex(hit.point, 2f).CheckOccupy();

                if (tileCode == 0)
                {
                    //BuildPreview.GetComponent<MeshRenderer>().materials = currBuild.Model.GetComponent<MeshRenderer>().sharedMaterials;
                    unavailableIcon.SetActive(false);
                    currBuild.gameObject.SetActive(true);
                }

                if (tileCode != 0 || HexManager.instance.HexDistance(teamTile, HexManager.instance.WorldToHex(hit.point, 2f)) > MatchSettings.instance.size.x / 4)
                {
                    //Material[] mats = BuildPreview.GetComponent<MeshRenderer>().materials;

                    //for (int i = 0; i < BuildPreview.GetComponent<MeshRenderer>().materials.Length; i++)
                    //    mats[i] = cannotBuild;

                    //BuildPreview.GetComponent<MeshRenderer>().materials = mats;
                    currBuild.gameObject.SetActive(false);
                    unavailableIcon.SetActive(true);
                    unavailableIcon.transform.position = BuildPreview.transform.position;
                }
                
            }
        }
        else
        {
            if (BuildPreview.activeSelf)
                BuildPreview.SetActive(false);
        }

    }
    
    //// Hexagonal Checks ////
    void CheckDrag()
    {
        if (Input.GetMouseButton(1) && selected != null)
        {
            if (!MoveIcon.activeSelf)
            {
                MoveIcon.SetActive(true);
                MoveIcon.transform.position = selected.transform.position;

                unavailableIcon.SetActive(false);
                unavailableIcon.transform.position = selected.transform.position;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, ground_layer))
            {
                Vector3 movePos = HexManager.instance.SnapToHexGrid(hit.point, 2f);

                Vector2 originHex = HexManager.instance.WorldToHex(selected.transform.position, 2f);
                Vector2 targetHex = HexManager.instance.WorldToHex(movePos, 2f);

                reachablePath = new List<Vector2>();

                path = null;
                path = HexPathfinder.FindPath(originHex, targetHex);

                //if not selected a building, check movement path
                if (selected.GetComponent<BuildingUnit>() == null)
                {
                    if (path != null && path.Count > 0)
                    {
                        int movesLeft = selected.GetComponent<UnitClass>().movesLeft;

                        int totalCost = 0; //dont count the starting position
                        foreach (Vector2 step in path)
                        {
                            int newCost = HexManager.instance.Hexes[step].TraverseCost();
                            

                            if (totalCost + newCost > movesLeft)
                                break;

                            if (newCost > 0)
                                totalCost += newCost;

                            reachablePath.Add(step);

                        }

                        RenderPath(reachablePath);

                        if (reachablePath.Count > 0)
                        {
                            MoveIcon.transform.position = HexManager.instance.HexToWorld(reachablePath[reachablePath.Count - 1], 2.0f);

                            if (reachablePath[reachablePath.Count - 1] != targetHex)
                            {
                                unavailableIcon.SetActive(true);
                                unavailableIcon.transform.position = HexManager.instance.HexToWorld(targetHex, 2f);
                            }
                            else
                            {
                                unavailableIcon.SetActive(false);
                            }
                        }
                        else
                        {
                            unavailableIcon.SetActive(true);
                            unavailableIcon.transform.position = HexManager.instance.HexToWorld(targetHex, 2f);
                            MoveIcon.transform.position = selected.transform.position;
                        }

                        MoveIcon.transform.position = HexManager.instance.HexToWorld(reachablePath[reachablePath.Count - 1], 2.0f);

                    }
                    else
                    {
                        unavailableIcon.SetActive(true);
                    }

                    //Dont show a path if hovering over an enemy
                    if (HexManager.instance.Hexes[targetHex].CheckOccupy() > 1)
                    {
                        if (HexManager.instance.CheckTeam(selected, HexManager.instance.Hexes[targetHex]) == false)
                        {

                            unavailableIcon.SetActive(true);

                            if (HexManager.instance.HexDistance(targetHex, HexManager.instance.WorldToHex(selected.transform.position, 2.0f))
                                <= selected.GetComponent<UnitClass>().attackRange)
                            {
                                MoveIcon.SetActive(true);
                                unavailableIcon.transform.position = HexManager.instance.HexToWorld(targetHex, 2f);
                                MoveIcon.transform.position = HexManager.instance.HexToWorld(targetHex, 2f);
                            }
                            else 
                            {
                                unavailableIcon.transform.position = HexManager.instance.HexToWorld(targetHex, 2f);
                                MoveIcon.SetActive(false);
                            }

                            path.Clear();
                            reachablePath.Clear();
                            //pathLine.positionCount = 0;// = false;
                            pathLine.positionCount = path.Count;
                            pathLine.enabled = false;
                            //MoveIcon.SetActive(false);
                        }
                    }
                }
                else //if building
                {
                    pathLine.enabled = false;
                    MoveIcon.SetActive(true);
                    
                    unavailableIcon.SetActive(true);
                    if(HexManager.instance.HexDistance(targetHex, HexManager.instance.WorldToHex(selected.transform.position, 2.0f)) 
                        <= selected.GetComponent<BuildingClass>().attackRange)
                    {
                        unavailableIcon.transform.position = HexManager.instance.HexToWorld(targetHex, 2f);
                        MoveIcon.transform.position = HexManager.instance.HexToWorld(targetHex, 2f);
                    }
                }
            }
        }
        else
        {
            MoveIcon.SetActive(false);
            pathLine.enabled = false;
            unavailableIcon.SetActive(false);
        }

        if (Input.GetMouseButtonUp(1) && selected && busy == false)
        {
            Vector2 originHex = HexManager.instance.WorldToHex(selected.transform.position, 2f);
            Vector2 targetHex = HexManager.instance.WorldToHex(hit.point, 2f);

            if (
                HexManager.instance.FindHex(hit.point, 2.0f).CheckOccupy() > 1 &&
                HexManager.instance.HexDistance(originHex, targetHex) <= selected.GetComponent<UnitClass>().attackRange &&
                HexManager.instance.CheckTeam(selected, HexManager.instance.FindHex(hit.point, 2.0f)) == false)
            {
                if (HexManager.instance.FindHex(hit.point, 2.0f).CheckOccupy() == 2)
                {
                    selected.GetComponent<UnitClass>().Battle(HexManager.instance.FindHex(hit.point, 2.0f).Occupied.GetComponent<UnitClass>());
                }
                else if (HexManager.instance.FindHex(hit.point, 2.0f).CheckOccupy() == 3)
                {
                    selected.GetComponent<UnitClass>().Battle(HexManager.instance.FindHex(hit.point, 2.0f).Building.GetComponent<UnitClass>());
                }
            }
            else
            {
                if (reachablePath != null || reachablePath.Count > 0)
                {
                    int totalCost = 1;
                    foreach (Vector2 step in reachablePath)
                    {
                        totalCost += HexManager.instance.Hexes[step].TraverseCost();
                    }

                    if (selected.GetComponent<UnitClass>() != null && selected.GetComponent<UnitClass>().AttemptMoveAlongPath(reachablePath, totalCost))
                    {
                        selected = null;
                        Debug.Log("Moved to: " + targetHex);
                        Debug.Log("Moved from: " + originHex);
                    }
                }
            }

            path = null;
            reachablePath = new List<Vector2>();
            
        }
    }
    void RenderPath(List<Vector2> path)
    {
        if (path != null && path.Count > 0)
        {
            pathLine.positionCount = path.Count;

            for (int i = 0; i < path.Count; i++)
            {
                Vector3 worldPos = HexManager.instance.HexToWorld(path[i], 2f);
                worldPos.y = 0.01f; // Slightly above ground
                pathLine.SetPosition(i, worldPos);
            }

            pathLine.enabled = true;
        }
        else
        {
            pathLine.enabled = false;
        }

    }

    /* before custom pathfinding
     * 
    void CheckDrag()
    {

        if (Input.GetMouseButton(1) && selected != null)
        {
            if (!MoveIcon.activeSelf)
                MoveIcon.SetActive(true);

            Ray ray = Camera.main.ScreenPointToRay(controls.Mouse.Position.ReadValue<Vector2>());

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, ground_layer))
            {
                Vector3 movePos = HexManager.instance.SnapToHexGrid(hit.point, 2f);

                Vector2 originHex = HexManager.instance.WorldToHex(selected.transform.position, 2f);
                Vector2 targetHex = HexManager.instance.WorldToHex(movePos, 2f);
                int spacesMoved = HexManager.instance.HexDistance(originHex, targetHex);

                if (spacesMoved > 0 && spacesMoved <= selected.GetComponent<UnitClass>().movesLeft) //+1 because we need to contribute for moving nowhere, so that doesnt count as a space to move
                {
                    MoveIcon.transform.position = movePos;
                    Debug.Log("Move cost: " + spacesMoved);
                
                
                }
                else
                {
                    movePos = selected.transform.position;
                    MoveIcon.transform.position = movePos;
                
                }

            }
        }
        else
        {
            MoveIcon.SetActive(false);
        }

        if(Input.GetMouseButtonUp(1) && selected)
        {
            Vector2 originHex = HexManager.instance.WorldToHex(selected.transform.position, 2f);
            Vector2 targetHex = HexManager.instance.WorldToHex(MoveIcon.transform.position, 2f);
            int spacesMoved = HexManager.instance.HexDistance(originHex, targetHex);

            if (selected.GetComponent<UnitClass>().AttemptMove(MoveIcon.transform.position, spacesMoved))
            {
                selected = null;

                Debug.Log("Moved to: " + targetHex);
                Debug.Log("Moved from: " + originHex);
            }
        }

    }
    */


    //// with squares ////

    //void CheckDrag()
    //{

    //    if (Input.GetMouseButton(1) && selected != null)
    //    {
    //        if (!MoveIcon.activeSelf)
    //            MoveIcon.SetActive(true);

    //        Ray ray = Camera.main.ScreenPointToRay(controls.Mouse.Position.ReadValue<Vector2>());

    //        if (Physics.Raycast(ray, out hit, Mathf.Infinity, ground_layer))
    //        {
    //            Vector3 movePos = SnapToGrid(hit.point, 5f);
    //            float Distance = Vector3.Distance(selected.transform.position, hit.point);

    //            if (Distance <= selected.GetComponent<UnitClass>().movesLeft) //+1 because we need to contribute for moving nowhere, so that doesnt count as a space to move
    //            {
    //                MoveIcon.transform.position = movePos;
    //                int spacesMoved = Mathf.RoundToInt(Distance / 5);
    //                Debug.Log("Move cost: " + Distance);


    //            }
    //            else if (Distance < 5)
    //            {
    //                movePos = transform.position;
    //                MoveIcon.transform.position = movePos;

    //            }

    //        }
    //    }
    //    else
    //    {
    //        MoveIcon.SetActive(false);
    //    }

    //    if(Input.GetMouseButtonUp(1) && selected)
    //    {
    //        int spacesMoved = Mathf.RoundToInt(Vector3.Distance(selected.transform.position, MoveIcon.transform.position) / 5);
    //        selected.GetComponent<UnitClass>().Move(MoveIcon.transform.position, spacesMoved);
    //        selected = null;
    //    }

    //}

    private void CheckStates()
    {

        SelectedIcon.SetActive(selected == null ? false : true);
        if(selected)
        {
            SelectedIcon.transform.position = new Vector3(selected.transform.position.x, 0, selected.transform.position.z);
        }

    }



    public void SelectBuilding(BuildingClass prefab)
    {
        if(currBuild != null)
        {
            Destroy(currBuild);
        }
        if(prefab.cost <= budget)
        {
            Debug.Log("Selceted: " + prefab.name);
            building = true;
            //currBuild = prefab;
            currBuild = Instantiate(prefab.gameObject, BuildPreview.transform);
            buildRot = Quaternion.identity;
            BuildPreview.transform.rotation = buildRot;
            selected = null;
        }
    }

}
