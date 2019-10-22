using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class TerrainGradualInteractor : MonoBehaviour
{
    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean triggerPress;
    private SteamVR_Behaviour_Pose controllerPose;

    private HeightExampleInteractor terrainExampleDetector;


    public TerrainHeightExample examplePrefab;
    private TerrainHeightExample currentlyPlacingExample;
    public float gradualMoveSpeed = 2f;
    private float lazyRecomputeTime = 0.25f;


    void Start()
    {
        controllerPose = GetComponent<SteamVR_Behaviour_Pose>();
        terrainExampleDetector = GetComponent<HeightExampleInteractor>();
    }

    void Update()
    {
        if( triggerPress.GetStateDown( handType ) )
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
            else
            {
                // place a new example
                // find a terrrain below or above us, and place an example there if we can
                ConnectedTerrainController currentTerrain = FindTerrainAndPlaceExample();

                // start recomputing the terrain
                StartCoroutine( LazilyRecomputeTerrain() );
            }
        }
        else if( currentlyPlacingExample != null && triggerPress.GetState( handType ) )
        {
            // move currentlyPlacingExample toward us
            currentlyPlacingExample.transform.position = 
                Vector3.MoveTowards( currentlyPlacingExample.transform.position, 
                    transform.position, gradualMoveSpeed * Time.deltaTime );
        }
        else if( triggerPress.GetStateUp( handType ) )
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

    ConnectedTerrainController FindTerrainAndPlaceExample()
    {
        // Bit shift the index of the layer (8: Connected terrains) to get a bit mask
        int layerMask = 1 << 8;

        RaycastHit hit;
        // Check from a point really high above us, in the downward direction (in case we are below terrain)
        if( Physics.Raycast( transform.position + 400 * Vector3.up, Vector3.down, out hit, Mathf.Infinity, layerMask ) )
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
