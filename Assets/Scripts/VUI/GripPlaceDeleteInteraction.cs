using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Photon.Pun;

public class GripPlaceDeleteInteraction : MonoBehaviour
{
    public Transform currentPrefabToUse;
    public bool isPrefabNetworked;

    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean gripPress;
    private SteamVR_Behaviour_Pose controllerPose;
    private GripPlaceDeleteInteractable collidingObject = null;
    private GameObject collidingGameObject = null;

    private TouchpadUpDownInteraction myUpDownInteraction;
    private TriggerGrabMoveInteraction myGrabMoveInteraction;
    private TouchpadLeftRightClickInteraction myLeftRightClickInteraction;

    private GameObject mostRecentlyCreated;



    // Start is called before the first frame update
    void Awake()
    {
        controllerPose = GetComponent<SteamVR_Behaviour_Pose>();
        myUpDownInteraction = GetComponent<TouchpadUpDownInteraction>();
        myGrabMoveInteraction = GetComponent<TriggerGrabMoveInteraction>();
        myLeftRightClickInteraction = GetComponent<TouchpadLeftRightClickInteraction>();
    }

    // Update is called once per frame
    void Update()
    {
        if( gripPress.GetStateDown( handType ) )
        {
            if( ShouldDeleteObject() )
            {
                DeleteCollidingObject();
            }
            else
            {
                PlaceObject();
            }
        }
    }

    public bool ShouldDeleteObject()
    {
        return collidingObject != null;
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
        Transform newObject; 
        if( isPrefabNetworked )
        {
            newObject = PhotonNetwork.Instantiate( currentPrefabToUse.name, transform.position, Quaternion.identity ).transform;
        }
        else
        {
            newObject = Instantiate( currentPrefabToUse, transform.position, Quaternion.identity );
        }
        mostRecentlyCreated = newObject.gameObject;

        // tell it that it has been instantiated
        GripPlaceDeleteInteractable shouldBeGrippable = newObject.GetComponent<GripPlaceDeleteInteractable>();
        if( shouldBeGrippable != null )
        {
            shouldBeGrippable.JustPlaced();
        }
    }


    private void DeleteCollidingObject()
    {
        // tell it it is about to be deleted (e.g. its controller should forget it)
        collidingObject.AboutToBeDeleted();

        // inform others
        myGrabMoveInteraction.GameObjectBeingDeleted( collidingGameObject );
        myUpDownInteraction.GameObjectBeingDeleted( collidingGameObject );
        myLeftRightClickInteraction.GameObjectBeingDeleted( collidingGameObject );


        // destroy it
        if( collidingGameObject.GetComponent<PhotonView>() != null )
        {
            PhotonNetwork.Destroy( collidingGameObject );
        }
        else
        {
            Destroy( collidingGameObject );
        }

        // forget it
        ForgetCollidingObject();
    }

    private void SetCollidingObject( Collider col )
    {
        if( collidingObject != null )
        {
            return;
        }

        GripPlaceDeleteInteractable maybeCollidingObject = col.GetComponentInParent<GripPlaceDeleteInteractable>();
        PhotonView maybePhotonView = col.GetComponentInParent<PhotonView>();
        if( maybeCollidingObject != null &&
            // only delete objects we own
            ( maybePhotonView == null || maybePhotonView.IsMine ) )
        {
            collidingObject = maybeCollidingObject;
            // there is no way to get to the came object from the Interface
            // --> just assume that the collider is one level down from the interface
            collidingGameObject = col.transform.parent.gameObject;
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
        if( other.transform.parent != null && other.transform.parent.gameObject == collidingGameObject )
        {
            ForgetCollidingObject();
        }
    }

    private void ForgetCollidingObject()
    {
        collidingObject = null;
        collidingGameObject = null;
    }

    public GameObject GetRecentlyCreated()
    {
        return mostRecentlyCreated;
    }

}

// interface to implement if you want your collider object to be able to be interacted with in this way

public interface GripPlaceDeleteInteractable
{
    void JustPlaced();
    void AboutToBeDeleted();
}
