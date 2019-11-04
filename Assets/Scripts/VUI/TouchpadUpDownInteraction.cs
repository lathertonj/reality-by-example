using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class TouchpadUpDownInteraction : MonoBehaviour
{

    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean click;
    private SteamVR_Behaviour_Pose controllerPose;
    private TouchpadUpDownInteractable collidingObject = null, interactingObject = null;
    private GameObject collidingGameObject = null;
    private Vector3 updownStartPosition, updownPreviousPosition;



    // Start is called before the first frame update
    void Awake()
    {
        controllerPose = GetComponent<SteamVR_Behaviour_Pose>();
    }

    // Update is called once per frame
    void Update()
    {
        if( click.GetStateDown( handType ) && collidingObject != null )
        {
            StartUpDownGesture();
        }
        if( click.GetState( handType ) && interactingObject != null )
        {
            ContinueUpDownGesture();
        }
        if( click.GetStateUp( handType ) && interactingObject != null )
        {
            EndUpDownGesture();
        }
    }

    // 3 methods for doing the interaction
    private void StartUpDownGesture()
    {
        interactingObject = collidingObject;
        updownStartPosition = updownPreviousPosition = controllerPose.transform.position;
    }

    private void ContinueUpDownGesture()
    {
        Vector3 currentPosition = controllerPose.transform.position;
        Vector3 displacementSinceBeginning = currentPosition - updownStartPosition;
        Vector3 displacementThisFrame = currentPosition - updownPreviousPosition;
        if( interactingObject != null ){ interactingObject.InformOfUpOrDownMovement( displacementSinceBeginning.y, displacementThisFrame.y ); }
        updownPreviousPosition = currentPosition;
    }

    private void EndUpDownGesture()
    {
        if( interactingObject != null ){ interactingObject.FinalizeMovement(); }
        interactingObject = null;
    }

    private void SetCollidingObject( Collider col )
    {
        if( collidingObject != null )
        {
            return;
        }

        TouchpadUpDownInteractable maybeCollidingObject = col.GetComponentInParent<TouchpadUpDownInteractable>();
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
                EndUpDownGesture();
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

public interface TouchpadUpDownInteractable
{
    void InformOfUpOrDownMovement( float verticalDisplacementSinceBeginning, float verticalDisplacementThisFrame );
    void FinalizeMovement();
}
