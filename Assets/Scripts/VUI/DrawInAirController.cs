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
    private float emissionRate = 0;

    void Awake()
    {
        handType = GetComponent<SteamVR_Behaviour_Pose>().inputSource;
        myTrail = GetComponentInChildren<ParticleSystem>();
        myTrailEmission = myTrail.emission;
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
}
