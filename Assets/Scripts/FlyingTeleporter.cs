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

    // Start is called before the first frame update
    void Start()
    {
        laser = Instantiate( laserPrefab );
        laserTransform = laser.transform;
    }

    // Update is called once per frame
    void Update()
    {
        if( ClickDown() )
        {
            Debug.Log("click down!");
        }

        if( ClickCurrentlyDown() )
        {
            ShowLaser( GetLaserLength() );
        }

        if( ClickUp() )
        {
            Teleport( GetLaserLength() );
        }
    }

    private float GetLaserLength()
    {
        return touchpadXY.GetAxis( handType ).y.Map( -1, 1, minTeleport, maxTeleport );
    }

    private void ShowLaser( float length )
    {
        laser.SetActive( true );
        Vector3 endpoint = controllerPose.transform.position + length * controllerPose.transform.forward;
        laserTransform.position = Vector3.Lerp( controllerPose.transform.position, endpoint, 0.5f );
        laserTransform.LookAt( endpoint );
        laserTransform.localScale = new Vector3( laserTransform.localScale.x, laserTransform.localScale.y, length );
    }

    private void Teleport( float length )
    {
        laser.SetActive( false );
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
