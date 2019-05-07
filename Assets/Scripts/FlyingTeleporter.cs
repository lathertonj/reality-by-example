using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class FlyingTeleporter : MonoBehaviour
{

    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean touchpadClick;
    public SteamVR_Action_Vector2 touchpadXY;
    public SteamVR_Behaviour_Pose controllerPose;

    public GameObject laserPrefab;
    private GameObject laser;
    private Transform laserTransform;

    public float minTeleport, maxTeleport;

    public Transform room, head;
    public GameObject teleportLaserEndPrefab;
    private GameObject teleportLaserEnd;


    // Start is called before the first frame update
    void Start()
    {
        laser = Instantiate( laserPrefab );
        laserTransform = laser.transform;
        teleportLaserEnd = Instantiate( teleportLaserEndPrefab );
        laser.SetActive( false );
        teleportLaserEnd.SetActive( false );
    }

    // Update is called once per frame
    void Update()
    {
        if( ClickDown() )
        {
            //Debug.Log("click down!");
        }

        if( ClickCurrentlyDown() )
        {
            ShowLaser();
        }

        if( ClickUp() )
        {
            Teleport();
        }
    }

    private float GetLaserLength()
    {
        return touchpadXY.GetAxis( handType ).y.PowMapClamp( -1, 1, minTeleport, maxTeleport, 3f );
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

    private void Teleport()
    {
        // hide lasers
        laser.SetActive( false );
        teleportLaserEnd.SetActive( false );

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

    private bool ClickDown()
    {
        return touchpadClick.GetStateDown( handType );
    }

    private bool ClickCurrentlyDown()
    {
        return touchpadClick.GetState( handType );
    }

    private bool ClickUp()
    {
        return touchpadClick.GetStateUp( handType );
    }
}
