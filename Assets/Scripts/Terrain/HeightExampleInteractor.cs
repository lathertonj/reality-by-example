using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;



public class HeightExampleInteractor : MonoBehaviour
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

    public GameObject GetCollidingObject()
    {
        return collidingObject;
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
            TerrainHeightExample theExample = objectInHand.GetComponent<TerrainHeightExample>();
            ConnectedTerrainController oldTerrain = theExample.myTerrain;

            // determine whether we are over a new terrain
            ConnectedTerrainController newTerrain = FindTerrain();

            // let go of object
            objectInHand.transform.parent = objectInHandOriginalParent;
            objectInHandOriginalParent = null;
            objectInHand = null;

            // switching terrains?
            if( newTerrain != oldTerrain )
            {
                theExample.myTerrain = newTerrain;
                oldTerrain.ForgetExample( theExample );
                newTerrain.ProvideExample( theExample );
            }
            else
            {
                // tell the terrain to recompute
                newTerrain.RescanProvidedExamples();
            }
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

    ConnectedTerrainController FindTerrain()
    {
        // Bit shift the index of the layer (8: Connected terrains) to get a bit mask
        int layerMask = 1 << 8;

        RaycastHit hit;
        // Check from a point really high above us, in the downward direction (in case we are below terrain)
        if( Physics.Raycast( transform.position + 400 * Vector3.up, Vector3.down, out hit, Mathf.Infinity, layerMask ) )
        {
            ConnectedTerrainController foundTerrain = hit.transform.GetComponentInParent<ConnectedTerrainController>();
            if( foundTerrain != null )
            {
                return foundTerrain;
            }
        }
        return null;
    }
}
