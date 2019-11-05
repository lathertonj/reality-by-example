using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class TouchpadLeftRightClickInteraction : MonoBehaviour
{

    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean click;
    public SteamVR_Action_Vector2 touchpadXY;
    private TouchpadLeftRightClickInteractable collidingObject = null;
    private GameObject collidingGameObject = null;




    // Start is called before the first frame update
    void Awake()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if( click.GetStateDown( handType ) && collidingObject != null )
        {
            if( touchpadXY.GetAxis( handType ).x <= -0.4f )
            {
                collidingObject.InformOfLeftClick();
            }
            else if( touchpadXY.GetAxis( handType ).x >= 0.4f )
            {
                collidingObject.InformOfRightClick();
            }
        }
    }

    private void SetCollidingObject( Collider col )
    {
        if( collidingObject != null )
        {
            return;
        }

        TouchpadLeftRightClickInteractable maybeCollidingObject = col.GetComponentInParent<TouchpadLeftRightClickInteractable>();
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

public interface TouchpadLeftRightClickInteractable
{
    void InformOfLeftClick();
    void InformOfRightClick();
}
