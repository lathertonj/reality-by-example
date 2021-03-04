using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Photon.Pun;

public class DrawInAirController : MonoBehaviourPunCallbacks
{
    public SteamVR_Action_Boolean drawTrail;
    private SteamVR_Input_Sources handType;
    public ParticleSystem trailPrefab;
    public bool isPrefabNetworked;
    public Vector3 prefabLocalPosition = new Vector3( 0, -0.04f, 0.02f );
    private ParticleSystem myTrail;
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

        if( !isPrefabNetworked ) 
        {
            InitTrail();
        }
        else
        {
            // need to enable it so that it will here OnJoinedRoom
            this.enabled = true;
        }
    }

    public override void OnJoinedRoom()
    {
        InitTrail();
        this.enabled = false;
    }

    void InitTrail()
    {
        if( isPrefabNetworked )
        {
            myTrail = PhotonNetwork.Instantiate( trailPrefab.name, transform.position, Quaternion.identity )
                .GetComponent<ParticleSystem>();
            myTrail.GetComponent<PhotonLaserParticleEmitterView>().Init( this );
        }
        else
        {
            myTrail = Instantiate( trailPrefab );
        }
        myTrail.transform.parent = transform;
        myTrail.transform.localPosition = prefabLocalPosition;
        
        // don't render
        StopRenderingTrail();
    }

    // Update is called once per frame
    void Update()
    {
        // don't do anything until init is complete
        if( myTrail == null )
        {
            return;
        }

        if( drawTrail.GetStateDown( handType ) )
        {
            RenderTrail();
        }
        else if( drawTrail.GetStateUp( handType ) )
        {
            StopRenderingTrail();
        }
    }

    public override void OnDisable()
    {
        base.OnDisable();
        StopRenderingTrail();
    }

    void RenderTrail()
    {
        var emission = myTrail.emission;
        emission.enabled = true;
    }

    void StopRenderingTrail()
    {
        var emission = myTrail.emission;
        emission.enabled = false;
    }

    public bool GetEnabled()
    {
        var emission = myTrail.emission;
        return emission.enabled;
    }

    private Color myColor;
    public void SetColor( Color c )
    {
        var main = myTrail.main;
        main.startColor = c;
        myColor = c;
    }

    public Color GetColor()
    {
        return myColor;
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

    private float myStartSize = 0, myEmissionRate = 0;
    private void SetMode( DrawMode mode )
    {
        myMode = mode;
        switch( mode )
        {
            case DrawMode.Air:
                myStartSize = airStartSize;
                myEmissionRate = airEmissionRate;
                // parent it to me and disable any following
                ParentEmitterToTransform( transform );
                myLaserFollower.StopFollowing();
                break;
            case DrawMode.Ground:
                myStartSize = groundStartSize;
                myEmissionRate = groundEmissionRate;
                // "parent" my trail to the end of the laser
                ParentEmitterToTransform( null );
                myLaserFollower.FollowEndPoint( myTrail.transform );
                break;
        }
        var main = myTrail.main;
        main.startSize = myStartSize;
        var emission = myTrail.emission;
        emission.rateOverDistance = myEmissionRate;
    }

    public float GetSize()
    {
        return myStartSize;
    }

    public float GetEmissionRate()
    {
        return myEmissionRate;
    }

    private void ParentEmitterToTransform( Transform t )
    {
        Transform emitter = myTrail.transform;
        // parent to new object
        emitter.parent = t;
        // preserve local position
        emitter.localPosition = prefabLocalPosition;
    }

}
