using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class TerrainTextureInteractor : MonoBehaviour
{
    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean triggerPress;
    private SteamVR_Behaviour_Pose controllerPose;

    public TerrainTextureController theTerrain;

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
            TerrainTextureExample newExample = Instantiate( examplePrefab, controllerPose.transform.position, Quaternion.identity );
            theTerrain.ProvideExample( newExample );
        }
    }
}
