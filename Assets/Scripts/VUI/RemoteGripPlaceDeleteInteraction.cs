using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class RemoteGripPlaceDeleteInteraction : MonoBehaviour
{
    public Transform currentPrefabToUse;

    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean gripPress;
    private SteamVR_Behaviour_Pose controllerPose;
    private GripPlaceDeleteInteractable selectedObject = null;
    private GameObject selectedGameObject = null;

    private RemoteTouchpadUpDownInteraction myUpDownInteraction;
    private RemoteTriggerGrabMoveInteraction myGrabMoveInteraction;
    private RemoteTouchpadLeftRightClickInteraction myLeftRightClickInteraction;



    public float objectPlaceDistance = 1.5f;



    // Start is called before the first frame update
    void Awake()
    {
        controllerPose = GetComponent<SteamVR_Behaviour_Pose>();
        myUpDownInteraction = GetComponent<RemoteTouchpadUpDownInteraction>();
        myGrabMoveInteraction = GetComponent<RemoteTriggerGrabMoveInteraction>();
        myLeftRightClickInteraction = GetComponent<RemoteTouchpadLeftRightClickInteraction>();
    }

    // Update is called once per frame
    void Update()
    {
        if( gripPress.GetStateDown( handType ) )
        {
            if( FindSelectedObject() )
            {
                DeleteSelectedObject();
            }
            else
            {
                PlaceObject();
            }
        }
    }

    // 3 methods for doing the interaction
    private void PlaceObject()
    {
        // don't place anything if we've been told not to
        if( currentPrefabToUse == null )
        {
            return;
        }

        // instantiate prefab
        Vector3 newPosition = controllerPose.transform.position + objectPlaceDistance * controllerPose.transform.forward;
        Transform newObject = Instantiate( currentPrefabToUse, newPosition, Quaternion.identity );

        // tell it that it has been instantiated
        GripPlaceDeleteInteractable shouldBeGrippable = newObject.GetComponent<GripPlaceDeleteInteractable>();
        if( shouldBeGrippable != null )
        {
            shouldBeGrippable.JustPlaced();
        }

        // select it
        LaserPointerSelector.SelectNewObject( newObject.gameObject );
    }


    private void DeleteSelectedObject()
    {
        // tell it it is about to be deleted (e.g. its controller should forget it)
        selectedObject.AboutToBeDeleted();

        // tell selector it is about to be deleted (i.e. unselect it)
        LaserPointerSelector.AboutToDeleteSelectedObject();

        // inform others
        myGrabMoveInteraction.GameObjectBeingDeleted( selectedGameObject );
        myUpDownInteraction.GameObjectBeingDeleted( selectedGameObject );
        myLeftRightClickInteraction.GameObjectBeingDeleted( selectedGameObject );

        // destroy it
        Destroy( selectedGameObject );

        // forget it
        selectedGameObject = null;
        selectedObject = null;
    }

    private bool FindSelectedObject()
    {
        selectedGameObject = LaserPointerSelector.GetSelectedObject();
        if( selectedGameObject )
        {
            selectedObject = selectedGameObject.GetComponentInParent<GripPlaceDeleteInteractable>();
        }
        else
        {
            selectedObject = null; 
        }
        
        return selectedObject != null;
    }

}
