using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Photon.Pun;

public class LaserPointerColliderSelector : MonoBehaviourPunCallbacks
{

    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean preview;
    public SteamVR_Action_Boolean stopShowingLaser;
    public bool stopShowingOnUp = true;
    private SteamVR_Behaviour_Pose controllerPose;
    private VibrateController vibration;

    public MeshRenderer laserPrefab;
    public bool isLaserNetworked;
    private bool haveInit = false;
    private MeshRenderer laser;
    private Transform laserTransform;
    private Vector3 hitPoint;
    private bool currentlyIntersecting;

    private GameObject mostRecentHitObject;

    public LayerMask mask;

    public Color notFound = Color.red, found = Color.green;

    private bool canShowPreview = false;
    private bool previousWasFound = false;

    // Start is called before the first frame update
    void Awake()
    {
        // init right away for non-networked
        if( !isLaserNetworked )
        {
            Init();
        }
        else
        {
            // need to remain enabled long enough to respond to OnJoinedRoom
            this.enabled = true;
        }
        // get references
        controllerPose = GetComponent<SteamVR_Behaviour_Pose>();
        vibration = GetComponent<VibrateController>();
    }

    // When networked, delay from Awake() to when we joined a room
    public override void OnJoinedRoom()
    {
        Init();
        // re-disable self
        this.enabled = false;
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
        HideLaser();
        haveInit = true;
    }

    // Update is called once per frame
    void Update()
    {
        // short circuit for non-init networked
        if( !haveInit ) { return; }

        if( IsAButtonPressedDown() )
        {
            canShowPreview = true; 
        }

        if( IsAButtonPressed() && !ShouldStopShowing() )
        {
            RaycastHit hit;
            // show laser
            if( Physics.Raycast( controllerPose.transform.position, controllerPose.transform.forward, out hit, 2000, mask ) )
            {
                hitPoint = hit.point;
                mostRecentHitObject = hit.collider.transform.root.gameObject;
                if( !previousWasFound )
                {
                    // switched from unfound to found --> play a vibration
                    vibration.Vibrate( 0.05f, 100, 50 );
                }
                ShowLaser( hit );
            }
            else
            {
                ShowUnfoundLaser();
            }
        }
        else
        {
            HideLaser();
        }
    }

    float triggerCutoff = 0.5f;
    bool IsAButtonPressedDown()
    {
        return preview.GetStateDown( handType );// || 
        // stopShowingLaser.GetStateDown( handType );

    }

    bool IsAButtonPressed()
    {
        return preview.GetState( handType );// ||
        // stopShowingLaser.GetState( handType );
    }


    bool ShouldStopShowing()
    {
        return !canShowPreview || ( stopShowingOnUp && preview.GetStateUp( handType ) )
            || ( !stopShowingOnUp && stopShowingLaser.GetState( handType ) );
    }

    public Vector3 GetMostRecentIntersectionPoint()
    {
        return hitPoint;
    }

    public GameObject GetMostRecentIntersectedObject()
    {
        return mostRecentHitObject;
    }

    public bool IsIntersecting()
    {
        return currentlyIntersecting;
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
        // enable / disable renderer, so the game object can continue running networked things
        laser.enabled = true;
        laser.material.color = c;
        laserTransform.position = Vector3.Lerp( controllerPose.transform.position, endPoint, .5f );
        laserTransform.LookAt( endPoint );
        laserTransform.localScale = new Vector3(
            laserTransform.localScale.x,
            laserTransform.localScale.y,
            distance
        );
        currentlyIntersecting = true;
    }

    public void HideLaser()
    {
        // enable / disable renderer (not game object), so the game object can continue running networked things
        laser.enabled = false;
        currentlyIntersecting = false;
        canShowPreview = false;
    }

    public override void OnDisable()
    {
        HideLaser();
        base.OnDisable();
    }
}
