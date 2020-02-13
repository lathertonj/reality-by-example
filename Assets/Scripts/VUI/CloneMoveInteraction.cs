using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class CloneMoveInteraction : MonoBehaviour
{
    // NOTE: this class is largely copied from TriggerGrabMoveInteraction
    // any bugfixes here should be propagated there, and vice versa

    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean vrAction;
    private SteamVR_Behaviour_Pose controllerPose;
    private CloneMoveInteractable collidingObject = null, interactingObject = null;
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
        if( vrAction.GetStateDown( handType ) && collidingObject != null )
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
        interactingObject = collidingObject.Clone( out interactingTransform );

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

    private void SetCollidingObject( Collider col )
    {
        if( collidingObject != null )
        {
            return;
        }

        CloneMoveInteractable maybeCollidingObject = col.GetComponentInParent<CloneMoveInteractable>();
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

public interface CloneMoveInteractable
{
    CloneMoveInteractable Clone( out Transform t );
    void InformOfTemporaryMovement( Vector3 currentPosition );
    void FinalizeMovement( Vector3 endPosition );
}
