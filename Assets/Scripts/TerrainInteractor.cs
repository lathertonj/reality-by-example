using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class TerrainInteractor : MonoBehaviour
{
    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean triggerPress;
    private SteamVR_Behaviour_Pose controllerPose;

    public TerrainController theTerrain;


    void Start()
    {
        controllerPose = GetComponent<SteamVR_Behaviour_Pose>();
    }

    // TODO: more complex interactions such as
    // - showing a little translucent object for the terrain examples
    // - ability to delete the terrain examples
    void Update()
    {
        if( triggerPress.GetStateDown( handType ) )
        {
            theTerrain.ProvideExample( controllerPose.transform.position );
        }
    }
}
