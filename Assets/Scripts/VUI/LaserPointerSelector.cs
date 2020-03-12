using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class LaserPointerSelector : MonoBehaviour
{
    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean selectAction;
    private SteamVR_Behaviour_Pose controllerPose;
    private VibrateController vibration;

    public MeshRenderer laserPrefab;
    public Transform selectedPrefab;
    private static Transform theSelectionMarker = null;

    private MeshRenderer laser;
    private Transform laserTransform;

    private GameObject intersectingObjectRoot = null;
    private GameObject intersectingObject = null;
    private static GameObject selectedObject = null;

    public LayerMask mask;

    public Color notFound = Color.red, found = Color.green;

    private bool showingLaser = false;
    private bool canSelectThings = false;
    private bool previousWasFound = false;

    public float timeCutoffForMenu = 0.3f;
    private float clickStartTime = -10;

    // Start is called before the first frame update
    void Awake()
    {
        laser = Instantiate(laserPrefab);
        laserTransform = laser.transform;
        controllerPose = GetComponent<SteamVR_Behaviour_Pose>();
        vibration = GetComponent<VibrateController>();
        HideLaser();

        if( theSelectionMarker == null )
        {
            // create it
            theSelectionMarker = Instantiate( selectedPrefab );

            // and hide it
            UnselectObject();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if( selectAction.GetStateDown( handType ) )
        {
            clickStartTime = Time.time;
            canSelectThings = false;
            
        }

        if( selectAction.GetStateUp( handType ) )
        {
            if( canSelectThings )
            {
                if( intersectingObject != null )
                {
                    // now is when we try to select something
                    SelectObject();
                }
                else
                {
                    // unselect
                    UnselectObject();
                }

            }
            else
            {
                // TODO: just turn on instead?
                // toggle the menu
                ModeSwitcherController.ToggleEnabled();
            }
        }
        
        if( selectAction.GetState( handType )  )
        {
            // if we've been holding button too long, turn on ability to select thigns
            if( !canSelectThings && Time.time - clickStartTime >= timeCutoffForMenu )
            {
                canSelectThings = true;
            }

            // try to select things
            if( canSelectThings )
            {
                RaycastHit hit;
                // show laser
                if( Physics.Raycast( controllerPose.transform.position, controllerPose.transform.forward, out hit, 2000, mask ) )
                {
                    if( !previousWasFound || intersectingObject != hit.collider.gameObject )
                    {
                        // switched from unfound to found --> play a vibration
                        Vibrate();
                    }

                    intersectingObjectRoot = hit.collider.transform.root.gameObject;
                    intersectingObject = hit.collider.gameObject;
                    ShowLaser( hit );
                }
                else
                {
                    ShowUnfoundLaser();
                    intersectingObjectRoot = null;
                    intersectingObject = null;
                }
            }
        }
        else
        {
            // always hide laser if not holding button down
            HideLaser();
            intersectingObjectRoot = null;
            intersectingObject = null;
        }

    }

    void TurnOnSelectionMode()
    {
        if( !showingLaser )
        {
            // turn on laser
            ShowUnfoundLaser();

            showingLaser = true;
            canSelectThings = true;
        }
    }


    void Vibrate()
    {
        vibration.Vibrate( 0.05f, 100, 50 );
    }

    void SelectObject()
    {
        // check if it's a menu item
        SwitchToComponent menuItem = intersectingObject.GetComponent<SwitchToComponent>();
        
        if( menuItem )
        {
            // activate it
            menuItem.ActivateMode( gameObject );

            // for certain kinds of things, don't unselect the object
            switch( menuItem.switchTo )
            {
                case SwitchToComponent.InteractionType.CreatureClone:
                case SwitchToComponent.InteractionType.CreatureExampleRecord:
                case SwitchToComponent.InteractionType.CreatureCreate:
                    // for these, don't unselect selection
                    break;
                default:
                    // for all others, unselect selection
                    UnselectObject();
                    break;
            }   
        }
        else
        {
            // non menu items get selected as per usual
            SelectNewObject( intersectingObjectRoot );
        }

        // vibrate for everyone
        Vibrate();
    }

    static void UnselectObject()
    {
        // send message that selection is being unselected
        if( selectedObject != null )
        {
            LaserPointerSelectable selectable = selectedObject.GetComponent< LaserPointerSelectable >();
            if( selectable != null ) { selectable.Unselected(); }
        }

        // hide marker
        selectedObject = null;
        theSelectionMarker.parent = null;
        theSelectionMarker.gameObject.SetActive( false );
    }



    public static GameObject GetSelectedObject()
    {
        return selectedObject;
    }

    public static void AboutToDeleteSelectedObject()
    {
        UnselectObject();
    }

    public static void SelectNewObject( GameObject newObject )
    {
        if( selectedObject != null ) { UnselectObject(); }

        selectedObject = newObject;
        theSelectionMarker.position = selectedObject.transform.position;
        theSelectionMarker.parent = selectedObject.transform;
        theSelectionMarker.gameObject.SetActive( true );

        LaserPointerSelectable selectable = selectedObject.GetComponent< LaserPointerSelectable >();
        if( selectable != null ) { selectable.Selected(); }
    }


    private void ShowLaser( RaycastHit hit )
    {
        ShowLaser( hit.point, hit.distance, found );
        previousWasFound = true;
    }

    private void ShowUnfoundLaser()
    {
        float dist = 1000;
        Vector3 endPoint = controllerPose.transform.position + dist * controllerPose.transform.forward;
        ShowLaser( endPoint, dist, notFound );
        previousWasFound = false;
    }

    private void ShowLaser( Vector3 endPoint, float distance, Color c )
    {
        laser.gameObject.SetActive( true );
        laser.material.color = c;
        laserTransform.position = Vector3.Lerp( controllerPose.transform.position, endPoint, .5f );
        laserTransform.LookAt( endPoint );
        laserTransform.localScale = new Vector3(
            laserTransform.localScale.x,
            laserTransform.localScale.y,
            distance
        );
    }

    public void HideLaser()
    {
        laser.gameObject.SetActive( false );
        previousWasFound = false;
    }
}

public interface LaserPointerSelectable
{
    void Selected();
    void Unselected();
}
