using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class RemoteCloneMoveInteraction : MonoBehaviour
{
    // NOTE: this class is largely copied from TriggerGrabMoveInteraction
    // any bugfixes here should be propagated there, and vice versa

    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean vrAction;
    private SteamVR_Behaviour_Pose controllerPose;
    private CloneMoveInteractable selectedObject = null, interactingObject = null;
    private Transform interactingTransform = null, interactingOriginalParent = null;



    // Start is called before the first frame update
    void Awake()
    {
        controllerPose = GetComponent<SteamVR_Behaviour_Pose>();
    }

    // Update is called once per frame
    void Update()
    {
        if( vrAction.GetStateDown( handType ) && FindSelectedObject() )
        {
            CloneAndStartMoveGesture();
        }
        if( vrAction.GetState( handType ) && interactingObject != null )
        {
            ContinueMoveGesture();
        }
        if( vrAction.GetStateUp( handType ) && interactingObject != null )
        {
            EndMoveGesture();
        }
    }

    // 3 methods for doing the interaction
    private void CloneAndStartMoveGesture()
    {
        // clone
        interactingObject = selectedObject.Clone( out interactingTransform );

        // store
        interactingOriginalParent = interactingTransform.parent;

        // parent it to me
        interactingTransform.parent = transform;
    }

    private void ContinueMoveGesture()
    {
        // notify
        interactingObject.InformOfTemporaryMovement( interactingTransform.position );
    }

    private void EndMoveGesture()
    {
        // unparent
        interactingTransform.parent = interactingOriginalParent;

        // notify
        interactingObject.FinalizeMovement( interactingTransform.position );

        // forget
        interactingObject = null;
        interactingTransform = null;
        interactingOriginalParent = null;
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


    public void GameObjectBeingDeleted( GameObject other )
    {
        // stop the gesture
        if( interactingTransform && other == interactingTransform.gameObject )
        {
            EndMoveGesture();
        }
    }

}

