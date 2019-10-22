using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class TerrainLaserRaiseLowerInteractor : MonoBehaviour
{
    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean deleteExample;
    public SteamVR_Action_Boolean placeExample;
    private SteamVR_Behaviour_Pose controllerPose;

    private HeightExampleInteractor terrainExampleDetector;

    private LaserPointerColliderSelector laser;

    public TerrainHeightExample examplePrefab;
    private TerrainHeightExample currentlyPlacingExample;
    
    private float lazyRecomputeTime = 0.25f;

    public float movementAmplification = 10f;
    private Vector3 lastHandPos;


    void Start()
    {
        controllerPose = GetComponent<SteamVR_Behaviour_Pose>();
        terrainExampleDetector = GetComponent<HeightExampleInteractor>();
        laser = GetComponent<LaserPointerColliderSelector>();
    }

    void Update()
    {
        if( deleteExample.GetStateDown( handType ) )
        {
            // are we currently intersecting with an example?
            GameObject maybeTerrainExample = terrainExampleDetector.GetCollidingObject();
            if( maybeTerrainExample != null )
            {
                TerrainHeightExample heightExample = maybeTerrainExample.GetComponentInParent<TerrainHeightExample>();
                // remove it
                heightExample.myTerrain.ForgetExample( heightExample );
                Destroy( maybeTerrainExample );
            }
        }
        else if( placeExample.GetStateDown( handType ) && laser.IsIntersecting() )
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
                DisableModeSwitcher.SetEnabled( false );
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
        DisableModeSwitcher.SetEnabled( true );
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
        // Bit shift the index of the layer (8: Connected terrains) to get a bit mask
        int layerMask = 1 << 8;

        RaycastHit hit;
        // Check from a point really high above us, in the downward direction (in case we are below terrain)
        if( Physics.Raycast( startPos + 400 * Vector3.up, Vector3.down, out hit, Mathf.Infinity, layerMask ) )
        {
            ConnectedTerrainController foundTerrain = hit.transform.GetComponentInParent<ConnectedTerrainController>();
            if( foundTerrain != null )
            {
                currentlyPlacingExample = Instantiate( examplePrefab, hit.point, Quaternion.identity );
                currentlyPlacingExample.myTerrain = foundTerrain;
                foundTerrain.ProvideExample( currentlyPlacingExample );
                return foundTerrain;
            }
        }
        return null;
    }
}
