using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class TerrainInteractor : MonoBehaviour
{
    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean triggerPress;
    private SteamVR_Behaviour_Pose controllerPose;


    public TerrainHeightExample examplePrefab;

    private HeightExampleInteractor terrainExampleDetector;



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
                // find a terrrain below or above us
                ConnectedTerrainController currentTerrain = FindTerrain();

                // if we found one, make an example and give it
                if( currentTerrain != null )
                {
                    TerrainHeightExample newExample = Instantiate( examplePrefab, controllerPose.transform.position, Quaternion.identity );
                    newExample.myTerrain = currentTerrain;
                    currentTerrain.ProvideExample( newExample );
                }
            }
        }
    }

    ConnectedTerrainController FindTerrain()
    {
        // Bit shift the index of the layer (8: Connected terrains) to get a bit mask
        int layerMask = 1 << 8;

        RaycastHit hit;
        // Check from a point really high above us, in the downward direction (in case we are below terrain)
        if( Physics.Raycast( transform.position + 400 * Vector3.up, Vector3.down, out hit, Mathf.Infinity, layerMask ) )
        {
            return hit.transform.GetComponentInParent<ConnectedTerrainController>();
        }
        return null;
    }
}
