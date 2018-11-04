﻿using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Mouse : MonoBehaviour
{
    #region Variables
    public ArrayList[] Grouping = new ArrayList[10];
    private KeyCode[] keyCodes = {
         KeyCode.Keypad1,
         KeyCode.Keypad2,
         KeyCode.Keypad3,
         KeyCode.Keypad4,
         KeyCode.Keypad5,
         KeyCode.Keypad6,
         KeyCode.Keypad7,
         KeyCode.Keypad8,
         KeyCode.Keypad9,
     };

    RaycastHit hit;

    public Vector3 RightClickPoint;
    public static ArrayList CurrentlySelectedUnits = new ArrayList();
    public static GameObject CurrentlyFocusedUnit;
    public static ArrayList UnitsOnScreen = new ArrayList();
    public static ArrayList UnitsInDrag = new ArrayList();
    private bool FinishedDragOnThisFrame;
    private bool StartedDrag;

    public GUIStyle MouseDragSkin;

    private static Vector3 mouseDownPoint;
    public static Vector3 currentMousePoint; //in World Space

    public static bool UserIsDragging;
    private static float TimeLimitBeforeDeclareDrag = 1f;
    private static float TimeLeftBeforeDeclareDrag;
    private static Vector2 MouseDragStart;
    private static float clickDragZone = 1.3f;
    public LayerMask MouseLayerMask;

    //GUI
    private float BoxWidth;
    private float BoxHeight;
    private float BoxTop;
    private float BoxLeft;
    private static Vector2 BoxStart;
    private static Vector2 BoxFinish;

    //DragMesh
    public LayerMask SelectMeshLayerMask;
    public float DistanceToGround;
    public LayerMask TerrainOnly;
    public GameObject DragSelectMesh;
    public GameObject Pointer;
    public Material DragSelectMeshMat;

    #endregion

    // Update is called once per frame
    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, MouseLayerMask))
        {
            currentMousePoint = hit.point;
            #region Visszavonások
            if (GUISetup.GhostActive)
                return;
            #endregion
            //Store point at mouse button down
            if (Input.GetMouseButtonDown(0))
            {
                mouseDownPoint = hit.point;
                TimeLeftBeforeDeclareDrag = TimeLimitBeforeDeclareDrag;
                MouseDragStart = Input.mousePosition;
                StartedDrag = true;
            }
            else if (Input.GetMouseButton(0))
            {
                //If the user is not dragging, lets do the tests
                if (!UserIsDragging)
                {
                    TimeLeftBeforeDeclareDrag = Time.deltaTime;
                    if (TimeLeftBeforeDeclareDrag <= 0f || UserDraggingByPosition(MouseDragStart, Input.mousePosition))
                        UserIsDragging = true;
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                //User is dragging
                if (UserIsDragging)
                    FinishedDragOnThisFrame = true;
                UserIsDragging = false;
            }

            //Mouse click
            if (!UserIsDragging)
            {
                //Debug.Log(hit.collider.name);
                if (Input.GetMouseButtonDown(1) && !EventSystem.current.IsPointerOverGameObject())
                {
                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Unit"))
                    {
                            SelectTargets(hit);
                    }
                    else if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Resources"))
                    {
                        Debug.Log("Hit Resource");
                        SelectGatherTargets(hit);
                    }
                    else if (hit.collider.name == "TerrainMain")
                    {
                        SelectRally(hit);
                        RightClickPoint = hit.point;
                        DeselectTargets();
                    }

                    else if (Input.GetMouseButtonUp(0) && DidUserClickLeftMouse(mouseDownPoint))
                    {
                        if (!Common.ShiftKeysDown())
                            DeselectGameObjectsIfSelected();
                    }
                }
                // End of Terrain
                else
                {
                    //Hitting other objects
                    if (Input.GetMouseButtonUp(0) && DidUserClickLeftMouse(mouseDownPoint))
                    {
                        //Is the user hitting a unit?
                        if (hit.collider.gameObject.GetComponent<Unit>() || hit.collider.gameObject.layer == LayerMask.NameToLayer("SelectMesh"))
                        {
                            Transform UnitGameObject;
                            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("SelectMesh"))
                                UnitGameObject = hit.collider.transform.parent.transform;
                            else
                                UnitGameObject = hit.collider.transform;

                            //Are we selecting a different object?
                            if (!UnitAlreadyInCurrentySelectedUnits(UnitGameObject.gameObject))
                            {
                                //If shift key is not down, remove the rest of the units
                                if (!Common.ShiftKeysDown())
                                    DeselectGameObjectsIfSelected();

                                GameObject SelectedObj = UnitGameObject.Find("Selected").gameObject;
                                SelectedObj.SetActive(true);

                                //Add unit to currently selected units
                                CurrentlySelectedUnits.Add(UnitGameObject.gameObject);

                                //Change the unit selected value to true
                                UnitGameObject.gameObject.GetComponent<Unit>().Selected = true;

                            }
                            else
                            {
                                //Unit is currently in the selected units arraylist
                                //Remove the units
                                if (Common.ShiftKeysDown())
                                    RemoveUnitFromCurrentlySelectedUnits(UnitGameObject.gameObject);
                                else if (!Common.ShiftKeysDown())
                                {
                                    DeselectGameObjectsIfSelected();

                                    GameObject SelectedObj = UnitGameObject.Find("Selected").gameObject;
                                    SelectedObj.SetActive(true);
                                    UnitGameObject.gameObject.GetComponent<Unit>().Selected = true;

                                    //Add unit to currently selected units
                                    CurrentlySelectedUnits.Add(UnitGameObject.gameObject);
                                }
                            }
                        }
                        else
                        {
                            //If this object is not a unit
                            if (!Common.ShiftKeysDown() && !EventSystem.current.IsPointerOverGameObject())
                                DeselectGameObjectsIfSelected();
                        }
                    }
                }
            }
            else
            {
                if (Input.GetMouseButtonUp(0) && DidUserClickLeftMouse(mouseDownPoint) && !EventSystem.current.IsPointerOverGameObject())
                {
                    if (!Common.ShiftKeysDown())
                        DeselectGameObjectsIfSelected();
                }
            }
            //End of dragging     

        } //End of raycasthit

        if (CurrentlySelectedUnits.Count != 0)
            CurrentlyFocusedUnit = CurrentlySelectedUnits[0] as GameObject;
        else CurrentlyFocusedUnit = null;

        if (!Common.ShiftKeysDown() && StartedDrag && UserIsDragging)
        {
            DeselectGameObjectsIfSelected();
            StartedDrag = false;
        }
        //Group creation
        if (CurrentlySelectedUnits.Count != 0 && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            for (int i = 1; i < 10; i++)
            {
                if (Input.GetKey(keyCodes[i - 1]))
                {
                    Grouping[i] = new ArrayList();
                    for (int j = 0; j < CurrentlySelectedUnits.Count; j++)
                    {
                        Grouping[i].Add(CurrentlySelectedUnits[j]);
                    }
                    Debug.Log(Grouping[i].Count);
                }
            }
        }

        //Group selection
        if (GetAnyKey(keyCodes))
        {
            for (int i = 1; i < 10; i++)
            {
                if (Input.GetKey(keyCodes[i - 1]) && Grouping[i] != null)
                {
                    DeselectGameObjectsIfSelected();
                    for (int j = 0; j < Grouping[i].Count; j++)
                    {
                        CurrentlySelectedUnits.Add((Grouping[i])[j]);
                    }
                }
            }
            Debug.Log(CurrentlySelectedUnits.Count);
            for (int j = 0; j < CurrentlySelectedUnits.Count; j++)
            {
                GameObject SelectedObj = CurrentlySelectedUnits[j] as GameObject;
                SelectedObj.GetComponent<Unit>().Selected = true;
                SelectedObj.transform.Find("Selected").gameObject.SetActive(true);
                Debug.Log(j + ". aktív");
            }
        }

        Debug.DrawRay(ray.origin, ray.direction * 1000, Color.yellow);
        //GUI Variables

        if (UserIsDragging && currentMousePoint != Vector3.positiveInfinity)
        {
            BoxWidth = Camera.main.WorldToScreenPoint(mouseDownPoint).x - Camera.main.WorldToScreenPoint(currentMousePoint).x;
            BoxHeight = Camera.main.WorldToScreenPoint(mouseDownPoint).y - Camera.main.WorldToScreenPoint(currentMousePoint).y;

            BoxLeft = Input.mousePosition.x;
            BoxTop = (Screen.height - Input.mousePosition.y) - BoxHeight;

            if (Common.FloatToBool(BoxWidth))
                if (Common.FloatToBool(BoxHeight))
                    BoxStart = new Vector2(Input.mousePosition.x, Input.mousePosition.y + BoxHeight);
                else
                    BoxStart = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            else
                if (!Common.FloatToBool(BoxWidth))
            {
                if (Common.FloatToBool(BoxHeight))
                    BoxStart = new Vector2(Input.mousePosition.x + BoxWidth, Input.mousePosition.y + BoxHeight);
                else
                    BoxStart = new Vector2(Input.mousePosition.x + BoxWidth, Input.mousePosition.y);
            }

            BoxFinish = new Vector2(BoxStart.x + Mathf.Abs(BoxWidth), BoxStart.y - Mathf.Abs(BoxHeight));
        }
    }

    void LateUpdate()
    {
        UnitsInDrag.Clear();
        if ((UserIsDragging || FinishedDragOnThisFrame) && UnitsOnScreen.Count > 0)
        {
            //Loop through units on the screen
            for (int i = 0; i < UnitsOnScreen.Count; i++)
            {
                GameObject UnitObj = UnitsOnScreen[i] as GameObject;
                if (UnitObj != null)
                {
                    Unit UnitScript = UnitObj.GetComponent<Unit>();
                    if (UnitScript.Owner == Game.currentPlayer.empireName)
                    {
                        GameObject SelectedObj = UnitObj.transform.Find("Selected").gameObject;

                        //If not already in the dragged units
                        if (!UnitAlreadyInDraggedUnits(UnitObj))
                        {
                            if (UnitInsideDrag(UnitScript.ScreenPos))
                            {
                                SelectedObj.SetActive(true);
                                UnitsInDrag.Add(UnitObj);
                            } //unit is not in drag
                            else
                            {
                                //remove the selected graphic, if unit is not already in CurrentlySelectedUnits
                                if (!UnitAlreadyInCurrentySelectedUnits(UnitObj))
                                    SelectedObj.SetActive(false);
                            }
                        }
                    }
                }
            }
        }
        if (FinishedDragOnThisFrame)
        {
            FinishedDragOnThisFrame = false;
            PutDraggedUnitsInCurrentlySelectedUnits();
        }
    }

    void OnGUI()
    {
        if (UserIsDragging)
        {
            GUI.Box(new Rect(BoxLeft, BoxTop, BoxWidth, BoxHeight), "", MouseDragSkin);
        }
    }

    #region Helper
    bool GetAnyKey(KeyCode[] aKeys)
    {
        foreach (var key in aKeys)
            if (Input.GetKey(key))
                return true;
        return false;
    }

    //Is the user dragging relative to mouse drag start point
    public bool UserDraggingByPosition(Vector2 DragStartPoint, Vector2 NewPoint)
    {
        if ((NewPoint.x > DragStartPoint.x + clickDragZone || NewPoint.x < DragStartPoint.x - clickDragZone) ||
            (NewPoint.y > DragStartPoint.y + clickDragZone || NewPoint.y < DragStartPoint.y - clickDragZone)) return true;
        else return false;
    }

    public bool DidUserClickLeftMouse(Vector3 hitPoint)
    {
        if (
            (mouseDownPoint.x < hitPoint.x + clickDragZone && mouseDownPoint.x > hitPoint.x - clickDragZone) &&
            (mouseDownPoint.y < hitPoint.y + clickDragZone && mouseDownPoint.y > hitPoint.y - clickDragZone) &&
            (mouseDownPoint.z < hitPoint.z + clickDragZone && mouseDownPoint.z > hitPoint.z - clickDragZone))
            return true;
        else
            return false;
    }

    public static void DeselectGameObjectsIfSelected()
    {
        if (CurrentlySelectedUnits.Count > 0)
        {
            if (CurrentlyFocusedUnit.GetComponent<Unit>() != null)
                CurrentlyFocusedUnit.GetComponent<Unit>().ShowBuildables = false;
            for (int i = 0; i < CurrentlySelectedUnits.Count; i++)
            {
                GameObject ArrayListUnit = CurrentlySelectedUnits[i] as GameObject;
                if (ArrayListUnit != null)
                {
                    ArrayListUnit.transform.Find("Selected").gameObject.SetActive(false);
                    ArrayListUnit.GetComponent<Unit>().Selected = false;
                }
            }
        }
        CurrentlySelectedUnits.Clear();
        GameObject.Find("Game").GetComponent<GUISetup>().DeleteIcons();
    }

    public static void SelectRally(RaycastHit hit)
    {
        if (CurrentlySelectedUnits.Count != 0)
        {
            for (int i = 0; i < CurrentlySelectedUnits.Count; i++)
            {
                GameObject CurrentObject = CurrentlySelectedUnits[i] as GameObject;
                if (CurrentObject != null)
                {
                    Structure currentStructure = CurrentObject.GetComponent<Structure>();
                    if (currentStructure != null)
                    {
                        if (CurrentObject.transform == hit.collider.transform)
                        {
                            currentStructure.RallyTarget = null;
                            currentStructure.RallyPoint = CurrentObject.transform.position;
                            Debug.Log(CurrentObject.name + ": " + currentStructure.RallyPoint);
                            continue;
                        }
                        else if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Unit"))
                        {
                            currentStructure.RallyPoint = Vector3.positiveInfinity;
                            currentStructure.RallyTarget = hit.collider.transform;
                            Debug.Log(CurrentObject.name + ": " + currentStructure.RallyPoint);
                        }
                        else
                        {
                            currentStructure.RallyTarget = null;
                            currentStructure.RallyPoint = hit.point;
                            Debug.Log(CurrentObject.name + ": " + currentStructure.RallyPoint);
                        }
                    }
                }
            }
        }
    }

    public static void SelectTargets(RaycastHit hit)
    {
        if (CurrentlySelectedUnits.Count != 0)
        {
            for (int i = 0; i < CurrentlySelectedUnits.Count; i++)
            {
                GameObject CurrentObject = CurrentlySelectedUnits[i] as GameObject;
                if (CurrentObject != null)
                {
                    AIDestinationSetter setter = CurrentObject.GetComponent<AIDestinationSetter>();
                    if (CurrentObject.GetComponent<Unit>() != null && CurrentObject.GetComponent<Structure>() == null && CurrentObject.GetComponent<Unit>().Owner == Game.currentPlayer.empireName)
                        if (Common.ShiftKeysDown())
                        {
                            CurrentObject.GetComponent<Unit>().ActionsQueue.Enqueue(hit.collider.gameObject.transform);
                        }
                        else
                        {
                            setter.target = hit.collider.gameObject.transform;
                            CurrentObject.GetComponent<Unit>().ActionsQueue.Clear();
                        }
                }

            }
        }
    }

    public static void SelectGatherTargets(RaycastHit hit)
    {
        if (CurrentlySelectedUnits.Count != 0)
        {
            for (int i = 0; i < CurrentlySelectedUnits.Count; i++)
            {
                GameObject CurrentObject = CurrentlySelectedUnits[i] as GameObject;
                if (CurrentObject != null && CurrentObject.GetComponent<Unit>().isGatherer)
                    CurrentObject.GetComponent<AIDestinationSetter>().target = hit.collider.gameObject.transform;
            }
        }
    }

    public static void DeselectTargets()
    {
        if (CurrentlySelectedUnits.Count != 0)
        {
            for (int i = 0; i < CurrentlySelectedUnits.Count; i++)
            {
                GameObject CurrentObject = CurrentlySelectedUnits[i] as GameObject;
                if (CurrentObject != null && CurrentObject.GetComponent<AIDestinationSetter>() != null)
                {
                    CurrentObject.GetComponent<AIDestinationSetter>().target = null;
                    CurrentObject.GetComponent<AIDestinationSetter>().ai.isStopped = false;
                }
            }
        }
    }

    public static bool UnitAlreadyInCurrentySelectedUnits(GameObject Unit)
    {
        if (CurrentlySelectedUnits.Count > 0)
        {
            for (int i = 0; i < CurrentlySelectedUnits.Count; i++)
            {
                GameObject ArrayListUnit = CurrentlySelectedUnits[i] as GameObject;
                if (ArrayListUnit == Unit)
                    return true;
            }
            return false;
        }
        else return false;
    }

    public void RemoveUnitFromCurrentlySelectedUnits(GameObject Unit)
    {
        if (CurrentlySelectedUnits.Count > 0)
        {
            for (int i = 0; i < CurrentlySelectedUnits.Count; i++)
            {
                GameObject ArrayListUnit = CurrentlySelectedUnits[i] as GameObject;
                if (ArrayListUnit == Unit)
                {
                    CurrentlySelectedUnits.RemoveAt(i);
                    ArrayListUnit.transform.Find("Selected").gameObject.SetActive(false);
                }
            }
            return;
        }
        else return;
    }

    //Check if unit is within screen space
    public static bool UnitWithinScreenSpace(Vector2 UnitScreenPos)
    {
        if ((UnitScreenPos.x < Screen.width && UnitScreenPos.y < Screen.height) && (UnitScreenPos.x > 0f && UnitScreenPos.y > 0f))
            return true;
        else return false;
    }

    public static void RemoveFromOnScreenUnits(GameObject Unit)
    {
        for (int i = 0; i < UnitsOnScreen.Count; i++)
        {
            GameObject UnitObj = UnitsOnScreen[i] as GameObject;
            if (Unit == UnitObj)
            {
                UnitsOnScreen.RemoveAt(i);
                UnitObj.GetComponent<Unit>().OnScreen = false;
                return;
            }
            return;
        }
    }

    //Is the unit inside drag?
    public static bool UnitInsideDrag(Vector2 UnitScreenPos)
    {
        if ((UnitScreenPos.x > BoxStart.x && UnitScreenPos.y < BoxStart.y) &&
           (UnitScreenPos.x < BoxFinish.x && UnitScreenPos.y > BoxFinish.y))
            return true;
        else return false;
    }

    //Check if unit is in UnitsInDrag array list
    public static bool UnitAlreadyInDraggedUnits(GameObject Unit)
    {
        if (UnitsInDrag.Count > 0)
        {
            for (int i = 0; i < UnitsInDrag.Count; i++)
            {
                GameObject ArrayListUnit = UnitsInDrag[i] as GameObject;
                if (ArrayListUnit == Unit)
                    return true;
            }
            return false;
        }
        else return false;
    }

    //Take all units from UnitsInDrag into CurrentlySelected Units
    public static void PutDraggedUnitsInCurrentlySelectedUnits()
    {
        if (UnitsInDrag.Count > 0)
        {
            for (int i = 0; i < UnitsInDrag.Count; i++)
            {
                GameObject UnitObj = UnitsInDrag[i] as GameObject;

                //If unit is not in CurrentlySelectedUnits, add it
                if (!UnitAlreadyInCurrentySelectedUnits(UnitObj))
                {
                    CurrentlySelectedUnits.Add(UnitObj);
                    UnitObj.GetComponent<Unit>().Selected = true;
                }
            }
            UnitsInDrag.Clear();
        }
    }
}

#endregion
