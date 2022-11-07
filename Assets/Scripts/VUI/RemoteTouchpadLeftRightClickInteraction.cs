using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class RemoteTouchpadLeftRightClickInteraction : MonoBehaviour
{

    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean leftClick;
    public SteamVR_Action_Boolean rightClick;

    private TouchpadLeftRightClickInteractable selectedObject = null;



    // Update is called once per frame
    void Update()
    {
        if( leftClick.GetStateDown( handType ) && FindSelectedObject() )
        {
            selectedObject.InformOfLeftClick();
        }
        else if( rightClick.GetStateDown( handType ) && FindSelectedObject() )
        {
            selectedObject.InformOfRightClick();
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

    public void GameObjectBeingDeleted( GameObject other )
    {
        // nothing to forget as this interaction is not held over time
    }

}
