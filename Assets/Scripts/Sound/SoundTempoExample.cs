using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SoundTempoExample : MonoBehaviour , TouchpadUpDownInteractable , TriggerGrabMoveInteractable , GripPlaceDeleteInteractable , LaserPointerSelectable , IPunInstantiateMagicCallback , IPhotonExample
{

    // default to 100
    [HideInInspector] public float myTempo = 100;

    // there should only be one
    private static SoundEngineTempoRegressor myRegressor = null;

    public static float minTempo = 30, maxTempo = 250;
    private TextMesh myText;

    void TouchpadUpDownInteractable.InformOfUpOrDownMovement( float verticalDisplacementSinceBeginning, float verticalDisplacementThisFrame )
    {
        float multiplier = 1f;
        if( verticalDisplacementThisFrame < 0 )
        {
            multiplier = verticalDisplacementThisFrame.MapClamp( -0.1f, 0f, 0.8f, 1f );
        }
        else
        {
            multiplier = verticalDisplacementThisFrame.MapClamp( 0f, 0.1f, 1f, 1.25f );
        }
        UpdateMyTempo( multiplier * myTempo );
    }

    void TouchpadUpDownInteractable.FinalizeMovement()
    {
        // tell the controller to recompute tempo
        myRegressor.RescanProvidedExamples();
        // alert others to rescan
        this.AlertNetworkToChanges();
    }

    public void Randomize( bool informRegressor = false )
    {
        // pick a new value 
        UpdateMyTempo( Random.Range( minTempo, maxTempo ) );

        // inform regressor
        if( informRegressor )
        {
            myRegressor.RescanProvidedExamples();
        }
        // alert others to rescan
        this.AlertNetworkToChanges();
    }

    public void UpdateMyTempo( float newTempo )
    {
        if( !myText ) { myText = GetComponentInChildren<TextMesh>(); }

        // clamp to min / max
        myTempo = Mathf.Clamp( newTempo, minTempo, maxTempo );

        // display
        myText.text = string.Format( "{0:0.00} BPM", myTempo );
    }

    void TriggerGrabMoveInteractable.InformOfTemporaryMovement( Vector3 currentPosition )
    {
        // don't care
        // TODO or, could update algorithm. this might be overkill perhaps
    }
    void TriggerGrabMoveInteractable.FinalizeMovement( Vector3 endPosition )
    {
        // tell the controller to recompute tempo
        myRegressor.RescanProvidedExamples();
        // alert others to rescan
        this.AlertNetworkToChanges();
    }

    public void JustPlaced()
    {
        Initialize( true );
    }

    public void Initialize( bool rescan )
    {
        // there should only be one...
        if( myRegressor == null )
        {
            myRegressor = FindObjectOfType<SoundEngineTempoRegressor>();
        }

        // update text too
        UpdateMyTempo( myTempo );
        
        // inform it
        myRegressor.ProvideExample( this, rescan );
    }

    public void AboutToBeDeleted()
    {
        // inform it
        myRegressor.ForgetExample( this );
    }


    public static void ShowHints( float pauseTimeBeforeFade )
    {
        if( myRegressor == null ) { return; }
        foreach( SoundTempoExample e in myRegressor.myRegressionExamples )
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

    public SerializableTempoExample Serialize()
    {
        SerializableTempoExample serial = new SerializableTempoExample();
        serial.position = transform.position;
        serial.tempo = myTempo;
        return serial;
    }

    public void ResetFromSerial( SerializableTempoExample serialized )
    {
        transform.position = serialized.position;
        UpdateMyTempo( serialized.tempo );
    }


    void LaserPointerSelectable.Selected()
    {
        // activate visualization when selected
        SoundEngineTempoRegressor.Activate();
    }

    void LaserPointerSelectable.Unselected()
    {
        // deactivate visualization when unselected
        SoundEngineTempoRegressor.Deactivate();
    }

    void IPhotonExample.AlertNetworkToChanges()
    {
        AlertNetworkToChanges();
    }

    void AlertNetworkToChanges()
    {
        // if we have a PhotonView component...
        PhotonView maybeNetworked = GetComponent<PhotonView>();
        // and the corresponding object belongs to us and we're on the network
        if( maybeNetworked != null && maybeNetworked.IsMine && PhotonNetwork.IsConnected )
        {
            // then tell others they need to rescan
            maybeNetworked.RPC( "SoundTempoLazyRescan", RpcTarget.Others );
        }
    }

    [PunRPC]
    void SoundTempoLazyRescan()
    {
        // do a lazy rescan
        PhotonRescanManager.LazyRescan( myRegressor );
    }

    // photon: init
    void IPunInstantiateMagicCallback.OnPhotonInstantiate( PhotonMessageInfo info )
    {
        // check who this came from
        PhotonView photonView = GetComponent<PhotonView>();
        if( !photonView.IsMine && PhotonNetwork.IsConnected )
        {
            Initialize( rescan: false );
            PhotonRescanManager.LazyRescan( myRegressor );
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
            // then my regressor needs to forget me
            myRegressor.ForgetExample( this, rescan: false );
            // lazily rescan soon
            PhotonRescanManager.LazyRescan( myRegressor );
        }
    }
}

[System.Serializable]
public class SerializableTempoExample
{
    public Vector3 position;
    public float tempo;
}

