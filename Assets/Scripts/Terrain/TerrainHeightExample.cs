using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class TerrainHeightExample : MonoBehaviour , TriggerGrabMoveInteractable , GripPlaceDeleteInteractable , IPunInstantiateMagicCallback , IPhotonExample
{
    [HideInInspector] public ConnectedTerrainController myTerrain;

    private static List< TerrainHeightExample > allExamples = new List< TerrainHeightExample >();


    void TriggerGrabMoveInteractable.InformOfTemporaryMovement( Vector3 currentPosition )
    {
        // don't respond to movements midway
    }

    void TriggerGrabMoveInteractable.FinalizeMovement( Vector3 endPosition )
    {
        FinalizeMovement();
    }

    public void FinalizeMovement()
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
        this.AlertNetworkToChanges();
    }

    public void JustPlaced()
    {
        ConnectedTerrainController maybeTerrain = FindTerrain();
        if( maybeTerrain == null )
        {
            PhotonView maybeNetworked = GetComponent<PhotonView>();
            if( maybeNetworked != null )
            {
                PhotonNetwork.Destroy( maybeNetworked );
            }
            else
            {
                Destroy( gameObject );
            }
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
        return TerrainUtility.FindTerrain<ConnectedTerrainController>( transform.position );
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

    public SerializableTerrainHeightExample Serialize( ConnectedTerrainController myTerrain )
    {
        // myTerrain passed just in case!

        SerializableTerrainHeightExample serial = new SerializableTerrainHeightExample();
        
        serial.localPosition = myTerrain.transform.InverseTransformPoint( transform.position );
        return serial;
    }

    public void ResetFromSerial( SerializableTerrainHeightExample serialized, ConnectedTerrainController myTerrain )
    {
        transform.position = myTerrain.transform.TransformPoint( serialized.localPosition );
    }

    void IPhotonExample.AlertNetworkToChanges()
    {
        AlertNetworkToChanges();
    }

    public void AlertNetworkToChanges()
    {
        // if we have a PhotonView component...
        PhotonView maybeNetworked = GetComponent<PhotonView>();
        // and the corresponding object belongs to us and we're on the network
        if( maybeNetworked != null && maybeNetworked.IsMine && PhotonNetwork.IsConnected )
        {
            // then tell others they need to rescan
            maybeNetworked.RPC( "TerrainHeightLazyRescan", RpcTarget.Others );
        }
    }

    [PunRPC]
    void TerrainHeightLazyRescan()
    {
        // see if we've been moved to a new terrain
        ConnectedTerrainController newTerrain = FindTerrain();
        if( newTerrain != null && newTerrain != myTerrain )
        {
            myTerrain.ForgetExample( this, shouldRescan: false );
            newTerrain.ProvideExample( this, shouldRescan: false );
            PhotonRescanManager.LazyRescan( myTerrain );
            PhotonRescanManager.LazyRescan( newTerrain );
            myTerrain = newTerrain;
        }
        else
        {
            // stick with myTerrain
            PhotonRescanManager.LazyRescan( myTerrain );
        }
    }

    // photon: init
    void IPunInstantiateMagicCallback.OnPhotonInstantiate( PhotonMessageInfo info )
    {
        // check who this came from
        PhotonView photonView = GetComponent<PhotonView>();
        if( !photonView.IsMine && PhotonNetwork.IsConnected )
        {
            // this example came from someone else
            ConnectedTerrainController maybeTerrain = FindTerrain();
            if( maybeTerrain != null )
            {
                // inform this terrain that I exist
                ManuallySpecifyTerrain( maybeTerrain );
                myTerrain.ProvideExample( this, false );
                PhotonRescanManager.LazyRescan( myTerrain );
            }
        }
    }


    // handle photon.destroy
    void OnDestroy()
    {
        // if we have a PhotonView component...
        PhotonView maybeNetworked = GetComponent<PhotonView>();
        // and the corresponding object doesn't belong to us and we're on the network
        if( maybeNetworked != null && !maybeNetworked.IsMine && PhotonNetwork.IsConnected )
        {
            // then my terrain needs to forget me
            myTerrain.ForgetExample( this, shouldRescan: false );
            PhotonRescanManager.LazyRescan( myTerrain );
        }
    }


}


[System.Serializable]
public class SerializableTerrainHeightExample
{
    public Vector3 localPosition;
}

