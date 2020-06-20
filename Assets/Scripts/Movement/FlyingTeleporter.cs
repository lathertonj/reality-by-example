using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class FlyingTeleporter : MonoBehaviour
{

    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean teleport;
    public SteamVR_Action_Boolean touchpadPreview;
    public SteamVR_Action_Vector2 touchpadXY;
    private SteamVR_Behaviour_Pose controllerPose;

    public GameObject laserPrefab;
    private GameObject laser;
    private Transform laserTransform;

    public float minTeleport, maxTeleport;

    public Transform room, head;
    public GameObject teleportLaserEndPrefab;
    private GameObject teleportLaserEnd;


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
        if( ShouldShowLaser() )
        {
            ShowLaser();
        
            if( teleport.GetStateDown( handType ) )
            {
                Teleport();
            }
        }
        else
        {
            HideLasers();
        }

    }

    private bool ShouldShowLaser()
    {
        return touchpadPreview.GetState( handType ) || ( touchpadXY.GetAxis( handType ) != Vector2.zero );
    }

    private float GetLaserLength()
    {
        return touchpadXY.GetAxis( handType ).y.PowMapClamp( -0.6f, 0.6f, minTeleport, maxTeleport, 3f );
    }

    private Vector3 GetTeleportPosition()
    {
        return controllerPose.transform.position + GetLaserLength() * controllerPose.transform.forward;
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

    private void Teleport()
    {
        HideLasers();

        // we want to put the user's body in this position, not the center of the room
        // Vector3 headRoomOffset = room.position - head.position;
        // headRoomOffset.y = 0;
        // room.position = GetTeleportPosition() + headRoomOffset;
        
        // this version: put the HAND in the spot. move the room 
        // by the difference between where the hand is now and
        // where the teleportation shows the hand will be.
        // AND, don't zero out the y. we want the hand to be
        // exactly where it shows it will be
        room.position += GetTeleportPosition() - controllerPose.transform.position;
    }

    void OnDisable()
    {
        HideLasers();
    }
}
