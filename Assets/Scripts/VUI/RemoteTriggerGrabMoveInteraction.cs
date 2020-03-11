using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class RemoteTriggerGrabMoveInteraction : MonoBehaviour
{

    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean triggerPress;
    private SteamVR_Behaviour_Pose controllerPose;
    private TriggerGrabMoveInteractable selectedObject = null, interactingObject = null;
    private Transform interactingTransform = null, interactingOriginalParent = null;



    // Start is called before the first frame update
    void Awake()
    {
        controllerPose = GetComponent<SteamVR_Behaviour_Pose>();
    }

    // Update is called once per frame
    void Update()
    {
        if( triggerPress.GetStateDown( handType ) && FindSelectedObject() )
        {
            StartMoveGesture();
        }
        if( triggerPress.GetState( handType ) && interactingObject != null )
        {
            ContinueMoveGesture();
        }
        if( triggerPress.GetStateUp( handType ) && interactingObject != null )
        {
            EndMoveGesture();
        }
    }

    // 3 methods for doing the interaction
    private void StartMoveGesture()
    {
        // store
        interactingObject = selectedObject;
        interactingTransform = LaserPointerSelector.GetSelectedObject().transform;
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
            selectedObject = so.GetComponentInParent<TriggerGrabMoveInteractable>();
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
