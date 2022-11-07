using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class LaserPointerColliderSelector : MonoBehaviour
{

    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean preview;
    public SteamVR_Action_Boolean stopShowingLaser;
    public bool stopShowingOnUp = true;
    private SteamVR_Behaviour_Pose controllerPose;
    private VibrateController vibration;

    public MeshRenderer laserPrefab;
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
        laser = Instantiate(laserPrefab);
        laserTransform = laser.transform;
        controllerPose = GetComponent<SteamVR_Behaviour_Pose>();
        vibration = GetComponent<VibrateController>();
        HideLaser();
    }

    // Update is called once per frame
    void Update()
    {
        if( preview.GetStateDown( handType ) )
        {
            canShowPreview = true; 
        }

        if( preview.GetState( handType ) && !ShouldStopShowing() )
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


    bool ShouldStopShowing()
    {
        return !canShowPreview || ( stopShowingOnUp && stopShowingLaser.GetStateUp( handType ) )
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
        laser.gameObject.SetActive( true );
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
        laser.gameObject.SetActive( false ); 
        currentlyIntersecting = false;
        canShowPreview = false;
    }

    void OnDisable()
    {
        HideLaser();
    }
}
