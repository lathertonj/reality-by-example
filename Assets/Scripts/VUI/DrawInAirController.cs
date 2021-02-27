using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class DrawInAirController : MonoBehaviour
{
    public SteamVR_Action_Boolean drawTrail;
    private SteamVR_Input_Sources handType;
    private ParticleSystem myTrail;
    private ParticleSystem.EmissionModule myTrailEmission;
    private FollowColliderLaserEndPoint myLaserFollower;
    public float airEmissionRate = 200f;
    public float airStartSize = 0.035f;
    public float groundEmissionRate = 10f;
    public float groundStartSize = 0.7f;
    private Vector3 trailLocalPosition;

    public enum DrawMode { Air, Ground }; 
    DrawMode myMode;

    void Awake()
    {
        handType = GetComponent<SteamVR_Behaviour_Pose>().inputSource;
        myLaserFollower = GetComponent<FollowColliderLaserEndPoint>();
        myTrail = GetComponentInChildren<ParticleSystem>();
        myTrailEmission = myTrail.emission;
        trailLocalPosition = myTrail.transform.localPosition;
        StopRenderingTrail();
    }

    // Update is called once per frame
    void Update()
    {
        if( drawTrail.GetStateDown( handType ) )
        {
            RenderTrail();
        }
        else if( drawTrail.GetStateUp( handType ) )
        {
            StopRenderingTrail();
        }
    }

    void OnDisable()
    {
        StopRenderingTrail();
    }

    void RenderTrail()
    {
        myTrailEmission.enabled = true;
    }

    void StopRenderingTrail()
    {
        myTrailEmission.enabled = false;
    }

    public void SetColor( Color c )
    {
        ParticleSystem.MainModule m = myTrail.main;
        m.startColor = c;
    }

    public void SetMode( SwitchToComponent.InteractionType mode )
    {
        // translate to our enum
        switch( mode )
        {
            case SwitchToComponent.InteractionType.DrawInAir:
                SetMode( DrawMode.Air );
                break;
            case SwitchToComponent.InteractionType.DrawOnGround:
                SetMode( DrawMode.Ground );
                break;
            default:
                Debug.LogError( "unknown drawing mode" );
                break;
        }
    }

    private void SetMode( DrawMode mode )
    {
        myMode = mode;
        ParticleSystem.MainModule m = myTrail.main;
        switch( mode )
        {
            case DrawMode.Air:
                m.startSize = airStartSize;
                myTrailEmission.rateOverDistance = airEmissionRate;
                // parent it to me and disable any following
                ParentEmitterToTransform( transform );
                myLaserFollower.StopFollowing();
                break;
            case DrawMode.Ground:
                m.startSize = groundStartSize;
                myTrailEmission.rateOverDistance = groundEmissionRate;
                // "parent" my trail to the end of the laser
                ParentEmitterToTransform( null );
                myLaserFollower.FollowEndPoint( myTrail.transform );
                break;
        }
    }

    private void ParentEmitterToTransform( Transform t )
    {
        Transform emitter = myTrail.transform;
        // parent to new object
        emitter.parent = t;
        // preserve local position
        emitter.localPosition = trailLocalPosition;
    }
}
