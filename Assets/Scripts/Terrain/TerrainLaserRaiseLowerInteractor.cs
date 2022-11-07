using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class TerrainLaserRaiseLowerInteractor : MonoBehaviour
{
    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean placeExample;
    private SteamVR_Behaviour_Pose controllerPose;

    private LaserPointerColliderSelector laser;

    public TerrainHeightExample examplePrefab;
    private TerrainHeightExample currentlyPlacingExample;
    
    private float lazyRecomputeTime = 0.25f;

    public float movementAmplification = 10f;
    private Vector3 lastHandPos;


    void Start()
    {
        controllerPose = GetComponent<SteamVR_Behaviour_Pose>();
        // hacky way to distinguish between two laser pointers :(
        foreach( LaserPointerColliderSelector l in GetComponents<LaserPointerColliderSelector>() )
        {
            if( !l.stopShowingOnUp )
            {
                laser = l;
            }
        }
    }

    void Update()
    {
        if( placeExample.GetStateDown( handType ) && laser.IsIntersecting() )
        {
                // place a new example
                // find a terrrain below or above us, and place an example there if we can
                ConnectedTerrainController currentTerrain = FindTerrainAndPlaceExample(
                    laser.GetMostRecentIntersectionPoint()
                );

                // start recomputing the terrain
                StartCoroutine( LazilyRecomputeTerrain() );

                // remember
                lastHandPos = transform.position;

                // since this is a placement-over-time technique, disable the mode switcher
                ModeSwitcherController.SetEnabled( false );
        }
        else if( currentlyPlacingExample != null && placeExample.GetState( handType ) )
        {
            // move currentlyPlacingExample according to hand pos
            Vector3 currentHandPos = transform.position;
            float movement = currentHandPos.y - lastHandPos.y;
            currentlyPlacingExample.transform.position += movement * movementAmplification * Vector3.up;

            // remember
            lastHandPos = currentHandPos;
        }
        else if( placeExample.GetStateUp( handType ) )
        {
            Abort();
        }
    }

    public void Abort()
    {
        if( currentlyPlacingExample != null )
        {
            // finalize terrain
            currentlyPlacingExample.myTerrain.RescanProvidedExamples();

            // stop moving currentlyPlacingExample
            currentlyPlacingExample = null;

        }

        // reenable the mode switcher
        // DisableModeSwitcher.SetEnabled( true );
    }

    private IEnumerator LazilyRecomputeTerrain()
    {
        yield return new WaitForSecondsRealtime( lazyRecomputeTime );

        while( currentlyPlacingExample != null )
        {
            // lazily recompute terrain
            currentlyPlacingExample.myTerrain.RescanProvidedExamples( true );

            yield return new WaitForSecondsRealtime( lazyRecomputeTime );
        }
    }

    ConnectedTerrainController FindTerrainAndPlaceExample( Vector3 startPos )
    {
        Vector3 hitPoint;
        ConnectedTerrainController foundTerrain = TerrainUtility.FindTerrain<ConnectedTerrainController>( startPos, out hitPoint );
        if( foundTerrain != null )
        {
            currentlyPlacingExample = Instantiate( examplePrefab, hitPoint, Quaternion.identity );
            currentlyPlacingExample.ManuallySpecifyTerrain( foundTerrain );
            foundTerrain.ProvideExample( currentlyPlacingExample );
        }
        return foundTerrain;
    }

    void OnDisable()
    {
        Abort();
    }
}
