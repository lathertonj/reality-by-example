using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Photon.Pun;

public class LaserPointerSelector : MonoBehaviourPunCallbacks
{
    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean selectAction;
    private SteamVR_Behaviour_Pose controllerPose;
    private VibrateController vibration;

    public MeshRenderer laserPrefab;
    public ParticleSystemFollower selectedPrefab;
    public bool isLaserNetworked, isSelectionNetworked;
    private static ParticleSystemFollower theSelectionMarker = null;

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

    private bool haveInit = false;

    private static bool mostRecentButtonPressWasMenu = false;

    private static Vector3 originalScale;
    private static Vector3 smallScale;

    public void Start()
    {
        // only if nothing is networked, init everything now. otherwise do it in OnJoinedRoom()
        if( !isLaserNetworked && !isSelectionNetworked )
        {
            Init();
        }
    }

    // When networked, delay from Start() to when we joined a room
    public override void OnJoinedRoom()
    {
        Init();
    }

    void Init()
    {
        if( isLaserNetworked )
        {
            laser = PhotonNetwork.Instantiate( laserPrefab.name, Vector3.zero, Quaternion.identity )
                .GetComponent<MeshRenderer>();
        }
        else
        {
            laser = Instantiate(laserPrefab);
        }

        laserTransform = laser.transform;
        controllerPose = GetComponent<SteamVR_Behaviour_Pose>();
        vibration = GetComponent<VibrateController>();
        HideLaser();

        if( theSelectionMarker == null )
        {
            // create it
            if( isSelectionNetworked )
            {
                theSelectionMarker = PhotonNetwork.Instantiate( selectedPrefab.name, Vector3.zero, Quaternion.identity )
                    .GetComponent<ParticleSystemFollower>();
            }
            else
            {
                theSelectionMarker = Instantiate( selectedPrefab );
            }
                

            // and hide it
            UnselectObject();

            // and store its properties
            originalScale = theSelectionMarker.transform.localScale;
            smallScale = 0.25f * originalScale;

        }
        haveInit = true;
    }

    // Update is called once per frame
    void Update()
    {
        if( !haveInit ) { return; }
        
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
                mostRecentButtonPressWasMenu = false;
            }
            else
            {
                // different people get to see different menus
                if( PhotonNetwork.IsMasterClient )
                {
                    // turn on / off or cycle between modes
                    ModeSwitcherController.CycleToNextMode();
                }
                else
                {
                    // non-main player only gets to see the communication menu
                    ModeSwitcherController.SetMode( ModeSwitcherController.Mode.Communication );
                    ModeSwitcherController.ToggleEnabled();
                }
                mostRecentButtonPressWasMenu = true;
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

                    intersectingObjectRoot = null;
                    intersectingObject = hit.collider.gameObject;

                    // look upward for a custom interface
                    // it is annoyingly difficult to get the game object by finding 
                    // the interface using Unity's builtin methods
                    Transform currentTransform = intersectingObject.transform;
                    while( currentTransform != null && intersectingObjectRoot == null )
                    {
                        if( currentTransform.GetComponent<LaserPointerSelectable>() != null )
                        {
                            intersectingObjectRoot = currentTransform.gameObject;
                        }
                        currentTransform = currentTransform.parent;
                    }

                    // if we didn't find one, just select the top level
                    if( intersectingObjectRoot == null ) 
                    {
                        intersectingObjectRoot = intersectingObject.transform.root.gameObject;
                    }

                    // show laser
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
        // reset scale
        theSelectionMarker.transform.localScale = originalScale;

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
                case SwitchToComponent.InteractionType.CreatureExampleClone:
                case SwitchToComponent.InteractionType.CreatureExampleDelete:
                case SwitchToComponent.InteractionType.MoveFollowCreature:
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

            // if we selected something that can be interacted with with touchpad,
            TouchpadLeftRightClickInteractable leftRight = intersectingObjectRoot.GetComponentInChildren<TouchpadLeftRightClickInteractable>();
            TouchpadUpDownInteractable upDown = intersectingObjectRoot.GetComponentInChildren<TouchpadUpDownInteractable>();
            if( leftRight != null || upDown != null )
            {    
                // disable touchpad things
                SwitchToComponent.DisableTouchpadAuxiliaryInteractors( gameObject );   

                // and enable touchpad interactors
                SwitchToComponent.EnableTouchpadPrimaryInteractors( gameObject );

            }

            // if we selected a high level method,
            // make the selection graphic much smaller as it's very close to us
            if( intersectingObject.GetComponent<HighLevelMethods>() != null )
            {
                theSelectionMarker.transform.localScale = smallScale;
            }
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

        // potentially change menu back to what it was before animation was selected
        if( SelectedObjectRequiresAnimationMenu() )
        {
            ModeSwitcherController.ResetMode();
        }

        // hide marker
        selectedObject = null;
        theSelectionMarker.objectToFollow = null;

        // hide via photon or regular unity
        PhotonView maybePhoton = theSelectionMarker.GetComponent<PhotonView>();
        if( maybePhoton != null )
        {
            maybePhoton.RPC( "PhotonEnabledViewSetActive", RpcTarget.All, false );
        }
        else
        {
            theSelectionMarker.gameObject.SetActive( false );
        }
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
        theSelectionMarker.objectToFollow = selectedObject.transform;

        // show via photon or regular unity
        PhotonView maybePhoton = theSelectionMarker.GetComponent<PhotonView>();
        if( maybePhoton != null )
        {
            maybePhoton.RPC( "PhotonEnabledViewSetActive", RpcTarget.All, true );
        }
        else
        {
            theSelectionMarker.gameObject.SetActive( true );
        }

        LaserPointerSelectable selectable = selectedObject.GetComponent< LaserPointerSelectable >();
        if( selectable != null ) { selectable.Selected(); }

        // potentially change menu to animation menu
        if( SelectedObjectRequiresAnimationMenu() )
        {
            ModeSwitcherController.SetMode( ModeSwitcherController.Mode.Animation );
        }
    }


    public static bool WasPressMenu()
    {
        return mostRecentButtonPressWasMenu;
    }

    private static bool SelectedObjectRequiresAnimationMenu()
    {
        // only master client gets to see the animation menu
        return PhotonNetwork.IsMasterClient && selectedObject != null && 
            (  selectedObject.GetComponent<AnimationByRecordedExampleController>() != null
            || selectedObject.GetComponent<AnimationExample>() != null );
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
