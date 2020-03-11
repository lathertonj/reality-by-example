using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class RemoteTouchpadUpDownInteraction : MonoBehaviour
{

    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean click;
    private SteamVR_Behaviour_Pose controllerPose;

    private TouchpadUpDownInteractable selectedObject = null;
    private TouchpadUpDownInteractable interactingObject = null;
    private GameObject interactingGameObject = null;
    private Vector3 updownStartPosition, updownPreviousPosition;



    // Start is called before the first frame update
    void Awake()
    {
        controllerPose = GetComponent<SteamVR_Behaviour_Pose>();
    }

    // Update is called once per frame
    void Update()
    {
        if( click.GetStateDown( handType ) && FindSelectedObject() )
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
        interactingObject = selectedObject;
        interactingGameObject = LaserPointerSelector.GetSelectedObject();
        updownStartPosition = updownPreviousPosition = controllerPose.transform.position;
    }

    private void ContinueUpDownGesture()
    {
        Vector3 currentPosition = controllerPose.transform.position;
        Vector3 displacementSinceBeginning = currentPosition - updownStartPosition;
        Vector3 displacementThisFrame = currentPosition - updownPreviousPosition;
        if( interactingGameObject != null ){ interactingObject.InformOfUpOrDownMovement( displacementSinceBeginning.y, displacementThisFrame.y ); }
        updownPreviousPosition = currentPosition;
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


}
