using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class TriggerGrabMoveInteraction : MonoBehaviour
{

    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean triggerPress;
    private SteamVR_Behaviour_Pose controllerPose;
    private TriggerGrabMoveInteractable collidingObject = null, interactingObject = null;
    private GameObject collidingGameObject = null;
    private Transform interactingTransform = null, interactingOriginalParent = null;



    // Start is called before the first frame update
    void Awake()
    {
        controllerPose = GetComponent<SteamVR_Behaviour_Pose>();
    }

    // Update is called once per frame
    void Update()
    {
        if( triggerPress.GetStateDown( handType ) && collidingObject != null )
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
        interactingObject = collidingObject;
        interactingTransform = collidingGameObject.transform;
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

    private void SetCollidingObject( Collider col )
    {
        if( collidingObject != null )
        {
            return;
        }

        TriggerGrabMoveInteractable maybeCollidingObject = col.GetComponentInParent<TriggerGrabMoveInteractable>();
        if( maybeCollidingObject != null )
        {
            collidingObject = maybeCollidingObject;
            // there is no way to get to the came object from the Interface
            // --> just assume that the collider is one level down from the interface
            collidingGameObject = col.transform.parent.gameObject;
        }
    }

    public void GameObjectBeingDeleted( GameObject other )
    {
        if( other == collidingGameObject )
        {
            // forget we are colliding with this object
            ForgetCollidingObject();

            // stop the gesture
            if( interactingObject != null )
            {
                EndMoveGesture();
            }
        }
    }

    // 3 methods for detecting whether we are colliding into the thing to interact with
    public void OnTriggerEnter( Collider other )
    {
        SetCollidingObject( other );
    }

    public void OnTriggerStay( Collider other )
    {
        SetCollidingObject( other );
    }

    public void OnTriggerExit( Collider other )
    {
        if( other.transform.parent.gameObject == collidingGameObject )
        {
            ForgetCollidingObject();
        }
    }

    private void ForgetCollidingObject()
    {
        collidingObject = null;
        collidingGameObject = null;
    }

}

// interface to implement if you want your collider object to be able to be interacted with in this way

public interface TriggerGrabMoveInteractable
{
    void InformOfTemporaryMovement( Vector3 currentPosition );
    void FinalizeMovement( Vector3 endPosition );
}
