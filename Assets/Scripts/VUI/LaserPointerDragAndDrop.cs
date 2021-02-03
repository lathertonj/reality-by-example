using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class LaserPointerDragAndDrop : MonoBehaviour
{

    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean preview, click;
    private SteamVR_Behaviour_Pose controllerPose;

    public GameObject laserPrefab;
    private GameObject laser;
    private Transform laserTransform;
    private MeshRenderer laserMesh;
    private bool currentlyIntersecting;

    public Color start, foundFrom, foundTo;
    private Color currentColor;

    private Collider firstCollidedObject = null, lastCollidedObject = null;
    private float forwardLaserLength = 20;
    private VibrateController vibrateController;

    public LayerMask mask;

    // Start is called before the first frame update
    void Awake()
    {
        laser = Instantiate(laserPrefab);
        laserTransform = laser.transform;
        laserMesh = laser.GetComponent<MeshRenderer>();
        controllerPose = GetComponent<SteamVR_Behaviour_Pose>();
        vibrateController = GetComponent<VibrateController>();
        HideLaser();
    }

    // Update is called once per frame
    void Update()
    {
        if( click.GetStateUp( handType ) )
        {
            HideLaser();
        }
        else if( click.GetStateDown( handType ) ) 
        {
            StartCopy();
        }
        else if( click.GetState( handType ) )
        {
            RaycastHit hit;
            // show laser
            if( Physics.Raycast( controllerPose.transform.position, transform.forward, out hit, Mathf.Infinity, mask ) )
            {
                if( firstCollidedObject == null && !HitDummyTerrain( hit ) )
                {
                    // we found a first collided object!
                    firstCollidedObject = hit.collider;

                    // update our color
                    currentColor = foundFrom;

                    // haptic feedback 
                    HapticFeedback();
                }
                else if( hit.collider != firstCollidedObject && hit.collider != lastCollidedObject && !HitDummyTerrain( hit ) )
                {
                    // update the "to" object
                    lastCollidedObject = hit.collider;
                    
                    // update our color
                    currentColor = foundTo;

                    // haptic feedback 
                    HapticFeedback();
                }
                else if( hit.collider == firstCollidedObject && lastCollidedObject != null )
                {
                    // unset the "to" object
                    lastCollidedObject = null;

                    // roll back color
                    currentColor = foundFrom;

                    // haptic feedback
                    HapticFeedback();
                }

                ShowLaser( hit );
            }
            else
            {
                ShowForwardLaser( forwardLaserLength );
            }
        }
        // preview: show the laser
        else if( preview.GetState( handType ) )
        {
            StartCopy();
        }
        else
        {
            HideLaser();
        }
    }


    private void HapticFeedback()
    {
        // vibrate controller (TODO values?)
        vibrateController.Vibrate( 0.1f, 100, 50 );
    }

    private bool HitDummyTerrain( RaycastHit hit )
    {
        return hit.collider.gameObject.GetComponent<DummyTerrain>() != null;
    }

    private void StartCopy()
    {
        // laser color set to red, display forward
        currentColor = start;
        ShowForwardLaser( forwardLaserLength );

        // reset object references
        firstCollidedObject = null;
        lastCollidedObject = null;
    }


    private void ShowLaser( RaycastHit hit )
    {
        ShowLaser( hit.point, hit.distance );
    }

    private void ShowForwardLaser( float distanceForward )
    {
        ShowLaser( controllerPose.transform.position + controllerPose.transform.forward.normalized * distanceForward, distanceForward );
    }

    private void ShowLaser( Vector3 endPoint, float distanceToEndPoint )
    {
        laser.SetActive( true );
        laserTransform.position = Vector3.Lerp( controllerPose.transform.position, endPoint, .5f );
        laserTransform.LookAt( endPoint );
        laserTransform.localScale = new Vector3(
            laserTransform.localScale.x,
            laserTransform.localScale.y,
            distanceToEndPoint
        );
        currentlyIntersecting = true;

        // set color
        laserMesh.material.color = currentColor;
    }

    public void HideLaser()
    {
        laser.SetActive( false ); 
        currentlyIntersecting = false;
    }

    public void OnDisable()
    {
        HideLaser();
    }

    public T GetStartObject<T>()
    {
        return (firstCollidedObject != null) ? firstCollidedObject.GetComponentInParent<T>() : default(T);
    }

    public T GetEndObject<T>()
    {
        return (lastCollidedObject != null) ? lastCollidedObject.GetComponentInParent<T>() : default(T);
    }
}
