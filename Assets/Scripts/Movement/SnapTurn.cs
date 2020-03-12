using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class SnapTurn : MonoBehaviour
{
    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean touchpadClicked;
    public SteamVR_Action_Vector2 touchpadXY;
    public Vector3 snapAmount = 15f * Vector3.up;
    public float leftRightButtonCutoff = 0.4f;
    public Transform room;
    

    // Update is called once per frame
    void Update()
    {
        if( touchpadClicked.GetStateDown( handType ) )
        {
            Vector2 touchpadPosition = touchpadXY.GetAxis( handType );
            if( touchpadPosition.x < -leftRightButtonCutoff )
            {
                room.Rotate( -snapAmount );
            }
            else if( touchpadPosition.x > leftRightButtonCutoff )
            {
                room.Rotate( snapAmount );
            }
        }    
    }
}
