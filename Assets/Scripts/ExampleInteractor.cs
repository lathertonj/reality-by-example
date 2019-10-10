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
            // the Example prefab is the PARENT of the collider
            objectInHand = collidingObject.transform.parent.gameObject;
            collidingObject = null;
            objectInHandOriginalParent = objectInHand.transform.parent;
            objectInHand.transform.parent = transform;
        }
    }

    private void ReleaseObject()
    {
        if( objectInHand != null )
        {
            // save reference to terrain
            ConnectedTerrainController theTerrain = objectInHand.GetComponent<TerrainHeightExample>().myTerrain;

            // let go of object
            objectInHand.transform.parent = objectInHandOriginalParent;
            objectInHandOriginalParent = null;
            objectInHand = null;

            // tell the terrain to recompute
            theTerrain.RescanProvidedExamples();

            // TODO: if we move far away enough from old terrain, then remove it from that terrain,
            // rescan that terrain, find new terrain, and add it to that terrain, and rescan that terrain.
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
