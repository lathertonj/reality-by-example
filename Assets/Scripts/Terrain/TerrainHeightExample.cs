using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainHeightExample : MonoBehaviour , TriggerGrabMoveInteractable , GripPlaceDeleteInteractable
{
    [HideInInspector] public ConnectedTerrainController myTerrain;

    private static List< TerrainHeightExample > allExamples = new List< TerrainHeightExample >();



    void TriggerGrabMoveInteractable.InformOfTemporaryMovement( Vector3 currentPosition )
    {
        // don't respond to movements midway
    }

    void TriggerGrabMoveInteractable.FinalizeMovement( Vector3 endPosition )
    {
        ConnectedTerrainController newTerrain = FindTerrain();
        if( newTerrain != null && newTerrain != myTerrain )
        {
            // switch to a new terrain
            myTerrain.ForgetExample( this );
            newTerrain.ProvideExample( this );
            myTerrain = newTerrain;
        }
        else
        {
            // tell my terrain to update
            myTerrain.RescanProvidedExamples();
        }
    }

    public void JustPlaced()
    {
        ConnectedTerrainController maybeTerrain = FindTerrain();
        if( maybeTerrain == null )
        {
            Destroy( gameObject );
        }
        else
        {
            ManuallySpecifyTerrain( maybeTerrain );
            myTerrain.ProvideExample( this );
        }
    }

    public void ManuallySpecifyTerrain( ConnectedTerrainController c )
    {
        myTerrain = c;
        allExamples.Add( this );
    }
    
    void GripPlaceDeleteInteractable.JustPlaced()
    {
        JustPlaced();
    }

    void GripPlaceDeleteInteractable.AboutToBeDeleted()
    {
        myTerrain.ForgetExample( this );
        allExamples.Remove( this );
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


    public static void ShowHints( float pauseTimeBeforeFade )
    {
        foreach( TerrainHeightExample e in allExamples )
        {
            e.ShowHint( pauseTimeBeforeFade );
        }
    }

    public MeshRenderer myHint;
    private Coroutine hintCoroutine;
    private void ShowHint( float pauseTimeBeforeFade )
    {
        StopHintAnimation();
        hintCoroutine = StartCoroutine( AnimateHint.AnimateHintFade( myHint, pauseTimeBeforeFade ) );
    }

    private void StopHintAnimation()
    {
        if( hintCoroutine != null )
        {
            StopCoroutine( hintCoroutine );
        }
    }

}
