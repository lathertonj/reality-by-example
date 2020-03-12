using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class TerrainGradualInteractor : MonoBehaviour
{
    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean triggerPress;
    private SteamVR_Behaviour_Pose controllerPose;


    public TerrainHeightExample examplePrefab;
    private TerrainHeightExample currentlyPlacingExample;
    public float gradualMoveSpeed = 2f;
    private float lazyRecomputeTime = 0.25f;
    private GripPlaceDeleteInteraction deleteDetector;


    void Start()
    {
        controllerPose = GetComponent<SteamVR_Behaviour_Pose>();
        deleteDetector = GetComponent<GripPlaceDeleteInteraction>();
    }

    void Update()
    {
        if( triggerPress.GetStateDown( handType ) )
        {
            if( !deleteDetector.ShouldDeleteObject() )
            {
                // we are not intersecting with another example that's about to be deleted, so
                // place a new example
                // find a terrrain below or above us, and place an example there if we can
                ConnectedTerrainController currentTerrain = FindTerrainAndPlaceExample();

                // start recomputing the terrain
                StartCoroutine( LazilyRecomputeTerrain() );

                // since this is a placement-over-time technique, disable the mode switcher
                ModeSwitcherController.SetEnabled( false );
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

        // reenable the mode switcher
        ModeSwitcherController.SetEnabled( true );
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
                currentlyPlacingExample.ManuallySpecifyTerrain( foundTerrain );
                foundTerrain.ProvideExample( currentlyPlacingExample );
                return foundTerrain;
            }
        }
        return null;
    }

    void OnDisable()
    {
        Abort();
    }
}
