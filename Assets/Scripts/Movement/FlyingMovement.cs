using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class FlyingMovement : MonoBehaviour
{

    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean fly;
    public SteamVR_Action_Boolean touchpadPreview;
    public SteamVR_Action_Vector2 touchpadXY;
    private SteamVR_Behaviour_Pose controllerPose;

    public GameObject laserPrefab;
    private GameObject laser;
    private Transform laserTransform;

    public float minFlyShown, maxFlyShown;
    public float percentDistancePerSecond = 0.3f;

    public Transform room, head;
    public GameObject teleportLaserEndPrefab;
    private GameObject teleportLaserEnd;

    private Vector3 flyOffset = Vector3.zero;


    // Start is called before the first frame update
    void Awake()
    {
        // grab reference
        controllerPose = GetComponent<SteamVR_Behaviour_Pose>();

        // make objects
        laser = Instantiate( laserPrefab );
        laserTransform = laser.transform;
        teleportLaserEnd = Instantiate( teleportLaserEndPrefab );
        laser.SetActive( false );
        teleportLaserEnd.SetActive( false );
    }

    // Update is called once per frame
    void Update()
    {
        if( touchpadPreview.GetState( handType ) )
        {
            ShowLaser();
        
            if( fly.GetStateDown( handType ) )
            {
                SetFlyAmount();
            }
        }
        else
        {
            HideLasers();
        }

        // actually do the flying
        room.position += flyOffset * percentDistancePerSecond * Time.deltaTime;
    }

    private float GetLaserLength()
    {
        return touchpadXY.GetAxis( handType ).y.PowMapClamp( -0.6f, 0.6f, minFlyShown, maxFlyShown, 3f );
    }

    private Vector3 GetTeleportPosition()
    {
        return controllerPose.transform.position + GetLaserLength() * controllerPose.transform.forward;
    }

    private void SetFlyAmount()
    {
        float length = GetLaserLength();
        if( length < minFlyShown * 1.05f )
        {
            // below 5% threshold, just stop
            flyOffset = Vector3.zero;
        }
        else
        {
            flyOffset = length * controllerPose.transform.forward;
        }
    }

    private void ShowLaser()
    {
        // show the laser
        laser.SetActive( true );

        // make the laser the right length and orientation
        Vector3 endpoint = GetTeleportPosition();
        laserTransform.position = Vector3.Lerp( controllerPose.transform.position, endpoint, 0.5f );
        laserTransform.LookAt( endpoint );
        laserTransform.localScale = new Vector3( laserTransform.localScale.x, laserTransform.localScale.y, GetLaserLength() );

        // show the laser end bit
        teleportLaserEnd.SetActive( true );
        teleportLaserEnd.transform.position = endpoint;
    }

    public void HideLasers()
    {
        // hide lasers
        if( laser!= null ) { laser.SetActive( false ); }
        if( teleportLaserEnd != null ) { teleportLaserEnd.SetActive( false ); }
    }


    void OnDisable()
    {
        HideLasers();
        flyOffset = Vector3.zero;
    }
}
