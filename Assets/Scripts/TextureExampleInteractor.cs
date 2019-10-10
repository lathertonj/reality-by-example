﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;



public class TextureExampleInteractor : MonoBehaviour
{

    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean grabAction;
    public SteamVR_Action_Boolean touchpadClick;
    public SteamVR_Action_Vector2 touchpadXY;
    private SteamVR_Behaviour_Pose controllerPose;

    private GameObject collidingObject;
    private GameObject objectInHand;
    private Transform objectInHandOriginalParent = null;

    public TerrainTextureController theTerrain;

    // Start is called before the first frame update
    void Start()
    {
        controllerPose = GetComponent<SteamVR_Behaviour_Pose>();
    }

    private void SetCollidingObject( Collider col )
    {
        if( collidingObject || !col.gameObject.CompareTag( "TerrainTextureExample" ) )
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

    private bool TryToAdvanceToPrevious( GameObject o )
    {
        if( o != null )
        {
            TerrainTextureExample example = o.GetComponentInParent<TerrainTextureExample>();
            if( example != null )
            {
                // previous material
                example.SwitchToPreviousMaterial();

                // recalculate everything
                theTerrain.RescanProvidedExamples();

                return true;
            }
        }
        return false;
    }

    private bool TryToAdvanceToNext( GameObject o )
    {
        if( o != null )
        {
            TerrainTextureExample example = o.GetComponentInParent<TerrainTextureExample>();
            if( example != null )
            {
                // previous material
                example.SwitchToNextMaterial();

                // recalculate everything
                theTerrain.RescanProvidedExamples();

                return true;
            }
        }
        return false;
    }

    private bool TryUpdatePosition( GameObject o )
    {
        if( o != null )
        {
            TerrainTextureExample example = o.GetComponentInParent<TerrainTextureExample>();
            if( example != null )
            {
                // update position
                example.UpdatePosition();
                return true;
            }
        }
        return false;
    }

    private void AdvanceToPrevious()
    {
        // first try colliding object, then object in hand
        if( ! TryToAdvanceToPrevious( collidingObject ) )
        {
            TryToAdvanceToPrevious( objectInHand );
        }
    }

    private void AdvanceToNext()
    {
        // first try colliding object, then object in hand
        if( ! TryToAdvanceToNext( collidingObject ) )
        {
            TryToAdvanceToNext( objectInHand );
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
            // tell the object it was moved
            TryUpdatePosition( objectInHand );

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

        if( ShouldAdvanceToPrevious() )
        {
            AdvanceToPrevious();
        }

        if( ShouldAdvanceToNext() )
        {
            AdvanceToNext();
        }
    }

    private bool ClickDown()
    {
        return touchpadClick.GetStateDown( handType );
    }

    private bool ShouldAdvanceToPrevious()
    {
        return ClickDown() && touchpadXY.GetAxis( handType ).x <= -0.4f;
    }

    private bool ShouldAdvanceToNext()
    {
        return ClickDown() && touchpadXY.GetAxis( handType ).x >= 0.4f;
    }
}