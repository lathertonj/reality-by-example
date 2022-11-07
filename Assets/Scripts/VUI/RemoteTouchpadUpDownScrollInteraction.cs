using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class RemoteTouchpadUpDownScrollInteraction : MonoBehaviour
{

    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean touchpadEngaged;
    public SteamVR_Action_Vector2 touchpadXY;

    private TouchpadUpDownInteractable selectedObject = null;
    private TouchpadUpDownInteractable interactingObject = null;
    private GameObject interactingGameObject = null;
    private Vector2 updownStartPosition, updownPreviousPosition;
    private RemoteTriggerGrabMoveInteraction mover;


    public bool joystickScroll = true;
    public float joystickSensitivity = 1f; 


    // Start is called before the first frame update
    void Awake()
    {
        mover = GetComponent<RemoteTriggerGrabMoveInteraction>();
    }

    // Update is called once per frame
    void Update()
    {
        if( touchpadEngaged.GetStateDown( handType ) && !mover.TouchpadInUse() && FindSelectedObject() )
        {
            StartUpDownGesture();
        }
        if( touchpadEngaged.GetState( handType ) && interactingObject != null )
        {
            ContinueUpDownGesture();
        }
        if( touchpadEngaged.GetStateUp( handType ) && interactingObject != null )
        {
            EndUpDownGesture();
        }
    }

    float currentJoystick = 0;
    // 3 methods for doing the interaction
    private void StartUpDownGesture()
    {
        interactingObject = selectedObject;
        interactingGameObject = LaserPointerSelector.GetSelectedObject();
        updownStartPosition = touchpadXY.GetAxis( handType );
        currentJoystick = 0;
    }

    private void ContinueUpDownGesture()
    {
        if( joystickScroll )
        {
            float joystickY = touchpadXY.GetAxis( handType ).y;
            float displacementThisFrame = joystickSensitivity * Time.deltaTime * joystickY;
            float nextY = currentJoystick + displacementThisFrame;
            if( interactingGameObject != null ) { interactingObject.InformOfUpOrDownMovement( nextY, displacementThisFrame ); }
            currentJoystick = nextY;
        }
        else
        {
            Vector2 currentPosition = touchpadXY.GetAxis( handType );
            Vector2 displacementSinceBeginning = currentPosition - updownStartPosition;
            Vector2 displacementThisFrame = currentPosition - updownPreviousPosition;
            if( interactingGameObject != null ){ interactingObject.InformOfUpOrDownMovement( displacementSinceBeginning.y, displacementThisFrame.y ); }
            updownPreviousPosition = currentPosition;
        }
    }

    private void EndUpDownGesture()
    {
        if( interactingGameObject != null ){ interactingObject.FinalizeMovement(); }
        interactingObject = null;
        interactingGameObject = null;
    }

    private bool FindSelectedObject()
    {
        GameObject so = LaserPointerSelector.GetSelectedObject();
        if( so )
        {
            selectedObject = so.GetComponentInParent<TouchpadUpDownInteractable>();
        }
        else
        {
            selectedObject = null; 
        }
        
        return selectedObject != null;
    }

    public void GameObjectBeingDeleted( GameObject other )
    {
        if( other == interactingGameObject )
        {
            // stop the gesture
            EndUpDownGesture();
        }
    }


}
