using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Photon.Pun;

public class RemoteGripPlaceDeleteInteraction : MonoBehaviour
{
    public Transform currentPrefabToUse;
    public bool isPrefabNetworked;

    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean gripPress;
    private SteamVR_Behaviour_Pose controllerPose;
    private GripPlaceDeleteInteractable selectedObject = null;
    private GameObject selectedGameObject = null;

    private RemoteTouchpadUpDownScrollInteraction myUpDownInteraction;
    private RemoteTriggerGrabMoveInteraction myGrabMoveInteraction;
    private RemoteTouchpadLeftRightClickInteraction myLeftRightClickInteraction;
    private RemoteCloneMoveInteraction myCloneMoveInteraction;



    public float objectPlaceDistance = 1.5f;

    public bool isDeleteEnabled = false;



    // Start is called before the first frame update
    void Awake()
    {
        controllerPose = GetComponent<SteamVR_Behaviour_Pose>();
        myUpDownInteraction = GetComponent<RemoteTouchpadUpDownScrollInteraction>();
        myGrabMoveInteraction = GetComponent<RemoteTriggerGrabMoveInteraction>();
        myLeftRightClickInteraction = GetComponent<RemoteTouchpadLeftRightClickInteraction>();
        myCloneMoveInteraction = GetComponent<RemoteCloneMoveInteraction>();
    }

    // Update is called once per frame
    void Update()
    {
        if( gripPress.GetStateDown( handType ) )
        {
            if( FindSelectedObject() && isDeleteEnabled )
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
        Transform newObject;
        if( isPrefabNetworked )
        {
            newObject = PhotonNetwork.Instantiate( currentPrefabToUse.name, newPosition, Quaternion.identity ).transform;
        }
        else
        {
            newObject = Instantiate( currentPrefabToUse, newPosition, Quaternion.identity );
        }

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
        if( selectedGameObject.GetComponent<PhotonView>() != null )
        {
            PhotonNetwork.Destroy( selectedGameObject );
        }
        else
        {
            Destroy( selectedGameObject );
        }

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
