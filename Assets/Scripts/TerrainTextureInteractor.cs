using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class TerrainTextureInteractor : MonoBehaviour
{
    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean triggerPress;
    private SteamVR_Behaviour_Pose controllerPose;


    public TerrainTextureExample examplePrefab;


    void Start()
    {
        controllerPose = GetComponent<SteamVR_Behaviour_Pose>();
    }

    // TODO: more complex interactions such as
    // - ability to delete the terrain examples
    void Update()
    {
        if( triggerPress.GetStateDown( handType ) )
        {
            ConnectedTerrainTextureController possibleTerrain = FindTerrain();
            if( possibleTerrain )
            {
                TerrainTextureExample newExample = Instantiate( examplePrefab, controllerPose.transform.position, Quaternion.identity );
                newExample.myTerrain = possibleTerrain;
                possibleTerrain.ProvideExample( newExample );
            }
        }
    }

    ConnectedTerrainTextureController FindTerrain()
    {
        // Bit shift the index of the layer (8: Connected terrains) to get a bit mask
        int layerMask = 1 << 8;

        RaycastHit hit;
        // Check from a point really high above us, in the downward direction (in case we are below terrain)
        if( Physics.Raycast( transform.position + 400 * Vector3.up, Vector3.down, out hit, Mathf.Infinity, layerMask ) )
        {
            return hit.transform.GetComponentInParent<ConnectedTerrainTextureController>();
        }
        return null;
    }
}
