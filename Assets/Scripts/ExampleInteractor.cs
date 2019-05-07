using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;



public class ExampleInteractor : MonoBehaviour
{

    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean grabAction;
    private SteamVR_Behaviour_Pose controllerPose;

    private GameObject collidingObject;
    private GameObject objectInHand;
    private Transform objectInHandOriginalParent = null;

    public TerrainController theTerrain;

    // Start is called before the first frame update
    void Start()
    {
        controllerPose = GetComponent<SteamVR_Behaviour_Pose>();
    }

    private void SetCollidingObject( Collider col )
    {
        if( collidingObject || !col.gameObject.CompareTag( "TerrainExample" ) )
        {
            return;
        }

        collidingObject = col.gameObject;
    }

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
        if( other.gameObject == collidingObject )
        {
            collidingObject = null;
        }
    }

    private void GrabObject()
    {
        if( collidingObject != null )
        {
            objectInHand = collidingObject;
            collidingObject = null;
            objectInHandOriginalParent = objectInHand.transform.parent;
            objectInHand.transform.parent = transform;
        }
    }

    private void ReleaseObject()
    {
        if( objectInHand != null )
        {
            // let go of object
            objectInHand.transform.parent = objectInHandOriginalParent;
            objectInHandOriginalParent = null;
            objectInHand = null;

            // tell the terrain to recompute
            theTerrain.RescanProvidedExamples();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if( grabAction.GetStateDown( handType ) )
        {
            GrabObject();
        }
        if( grabAction.GetStateUp( handType ) )
        {
            ReleaseObject();
        }
    }
}
