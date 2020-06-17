using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class RemoteCloneMoveInteraction : MonoBehaviour
{
    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean vrAction;
    private SteamVR_Behaviour_Pose controllerPose;
    private CloneMoveInteractable selectedObject = null;
    private bool triggerIsMovingForUs = false;
    private RemoteTriggerGrabMoveInteraction myTriggerMover = null;



    // Start is called before the first frame update
    void Awake()
    {
        controllerPose = GetComponent<SteamVR_Behaviour_Pose>();
        myTriggerMover = GetComponent<RemoteTriggerGrabMoveInteraction>();
    }

    // Update is called once per frame
    void Update()
    {
        if( vrAction.GetStateDown( handType ) && FindSelectedObject() )
        {
            CloneAndStartMoveGesture();
        }
        if( vrAction.GetState( handType ) && triggerIsMovingForUs )
        {
            myTriggerMover.ContinueMoveGestureExternally();
        }
        if( vrAction.GetStateUp( handType ) && triggerIsMovingForUs )
        {
            myTriggerMover.EndMoveGestureExternally();
            triggerIsMovingForUs = false;
        }
    }

    // 3 methods for doing the interaction
    private void CloneAndStartMoveGesture()
    {
        // if we were moving another object, stop it
        myTriggerMover.EndMoveGestureExternally(); 
        
        // clone
        Transform newObject = null;
        selectedObject.Clone( out newObject );
        
        // tell trigger to move it
        TriggerGrabMoveInteractable interactableReference = newObject.GetComponent<TriggerGrabMoveInteractable>();
        myTriggerMover.StartMoveGestureExternally( interactableReference, newObject );

        // select it
        LaserPointerSelector.SelectNewObject( newObject.gameObject );

        // remember
        triggerIsMovingForUs = true;
    }


    private bool FindSelectedObject()
    {
        GameObject so = LaserPointerSelector.GetSelectedObject();
        if( so )
        {
            selectedObject = so.GetComponentInParent<CloneMoveInteractable>();
        }
        else
        {
            selectedObject = null; 
        }
        
        return selectedObject != null;
    }

}

