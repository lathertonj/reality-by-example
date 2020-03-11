using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class RemoteTouchpadLeftRightClickInteraction : MonoBehaviour
{

    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean click;
    public SteamVR_Action_Vector2 touchpadXY;
    private TouchpadLeftRightClickInteractable selectedObject = null;



    // Update is called once per frame
    void Update()
    {
        if( click.GetStateDown( handType ) && FindSelectedObject() )
        {
            if( touchpadXY.GetAxis( handType ).x <= -0.4f )
            {
                selectedObject.InformOfLeftClick();
            }
            else if( touchpadXY.GetAxis( handType ).x >= 0.4f )
            {
                selectedObject.InformOfRightClick();
            }
        }
    }

    private bool FindSelectedObject()
    {
        GameObject so = LaserPointerSelector.GetSelectedObject();
        if( so )
        {
            selectedObject = so.GetComponentInParent<TouchpadLeftRightClickInteractable>();
        }
        else
        {
            selectedObject = null; 
        }
        
        return selectedObject != null;
    }

}
