using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class SnapTurn : MonoBehaviour
{
    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean leftClick, rightClick;
    
    public Vector3 snapAmount = 15f * Vector3.up;
    public Transform room;
    

    // Update is called once per frame
    void Update()
    {
        if( leftClick.GetStateDown( handType ) )
        {
            room.Rotate( -snapAmount );
        } 
        else if( rightClick.GetStateDown( handType ) )
        {
            room.Rotate( snapAmount );
        }
    }
}
