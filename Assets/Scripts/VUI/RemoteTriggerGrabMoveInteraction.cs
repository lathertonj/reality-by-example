using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class RemoteTriggerGrabMoveInteraction : MonoBehaviour
{

    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean triggerPress;
    public SteamVR_Action_Boolean touchpadPreview;
    public SteamVR_Action_Vector2 touchpadXY;
    private SteamVR_Behaviour_Pose controllerPose;
    private TriggerGrabMoveInteractable selectedObject = null, interactingObject = null;
    private Transform interactingTransform = null, interactingOriginalParent = null;

    public bool touchpadMovingEnabled = false;
    public float touchpadMovingMinDistance = 1f;
    private Vector2 prevTouchpadPosition;


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
        // move it toward / away from us
        if( touchpadMovingEnabled )
        {
            if( touchpadPreview.GetStateDown( handType ) )
            {
                prevTouchpadPosition = touchpadXY.GetAxis( handType );
            }
            else if( touchpadPreview.GetState( handType ) )
            {
                Vector2 currentTouchpadPosition = touchpadXY.GetAxis( handType );

                // difference in y == movement in world away / toward self
                float difference = currentTouchpadPosition.y - prevTouchpadPosition.y;
                Vector3 offset = ( interactingTransform.position - transform.position );
                float distance = offset.magnitude;
                Vector3 direction = offset.normalized;


                if( difference >= 0 )
                {
                    // move it away from us
                    float amount = difference.PowMapClamp( 0, 0.1f, 0, 4, 3 );
                    interactingTransform.position += amount * direction;
                }   
                else
                {
                    // move it towards us
                    float amount = difference.PowMapClamp( 0, -0.1f, 0, 4, 3 );

                    // but no closer than min distance
                    if( distance - amount <= touchpadMovingMinDistance )
                    {
                        // place at min distance
                        interactingTransform.position = transform.position + touchpadMovingMinDistance * direction;
                    }
                    else
                    {
                        // move as normal
                        interactingTransform.position -= amount * direction;
                    }
                }

                prevTouchpadPosition = currentTouchpadPosition;
            }
        }
        
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
