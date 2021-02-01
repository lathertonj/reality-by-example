using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Sound0To1Example : MonoBehaviour , TouchpadUpDownInteractable , TriggerGrabMoveInteractable , GripPlaceDeleteInteractable , LaserPointerSelectable , IPunInstantiateMagicCallback , IPhotonExample
{

    // default to 0.5
    [HideInInspector] public float myValue = 0.5f;
    public SoundEngine0To1Regressor.Parameter myType;

    private SoundEngine0To1Regressor myRegressor;

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
        UpdateMyValue( multiplier * myValue );
    }

    void TouchpadUpDownInteractable.FinalizeMovement()
    {
        // tell the controller to recompute parameter
        myRegressor.RescanProvidedExamples();
        // tell other network examples to rescan
        this.AlertNetworkToChanges();
    }

    public void Randomize( bool informRegressor = false )
    {
        // pick a random value
        UpdateMyValue( Random.Range( 0f, 1f ) );

        // update my regressor
        if( informRegressor )
        {
            myRegressor.RescanProvidedExamples();
        }
        // alert others to rescan lazily
        this.AlertNetworkToChanges();
    }

    public void UpdateMyValue( float newValue )
    {
        if( !myText ) { myText = GetComponentInChildren<TextMesh>(); }
        
        // clamp to min / max
        myValue = Mathf.Clamp01( newValue );

        // display according to mode
        switch( myType )
        {
            case SoundEngine0To1Regressor.Parameter.Density:
                myText.text = string.Format( "Density: {0:0.00}", myValue );
                break;
            case SoundEngine0To1Regressor.Parameter.Timbre:
                myText.text = string.Format( "Timbre: {0:0.00}", myValue );
                break;
            case SoundEngine0To1Regressor.Parameter.Volume:
                myText.text = string.Format( "Volume: {0:0.00}", myValue );
                break;
        }
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
        // alert others to rescan lazily
        this.AlertNetworkToChanges();
    }

    public void JustPlaced()
    {
        Initialize( true );
    }

    public void Initialize( bool rescan )
    {
        // there should only be one...
        switch( myType )
        {
            case SoundEngine0To1Regressor.Parameter.Density:
                myRegressor = SoundEngine0To1Regressor.densityRegressor;
                break;
            case SoundEngine0To1Regressor.Parameter.Timbre:
                myRegressor = SoundEngine0To1Regressor.timbreRegressor;
                break;
            case SoundEngine0To1Regressor.Parameter.Volume:
                myRegressor = SoundEngine0To1Regressor.volumeRegressor;
                break;
        }

        // update text too
        UpdateMyValue( myValue );

        // inform it
        myRegressor.ProvideExample( this, rescan );
    }

    public void AboutToBeDeleted()
    {
        // inform it
        myRegressor.ForgetExample( this );
    }


    public static void ShowHints( SoundEngine0To1Regressor regressor, float pauseTimeBeforeFade )
    {
        foreach( Sound0To1Example e in regressor.myRegressionExamples )
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

    public Serializable0To1Example Serialize()
    {
        Serializable0To1Example serial = new Serializable0To1Example();
        serial.position = transform.position;
        serial.type = myType;
        serial.value = myValue;
        return serial;
    }

    public void ResetFromSerial( Serializable0To1Example serialized )
    {
        transform.position = serialized.position;
        myType = serialized.type;
        UpdateMyValue( serialized.value );
    }

    void LaserPointerSelectable.Selected()
    {
        // activate visualization when selected
        SoundEngine0To1Regressor.Activate( myRegressor );
    }

    void LaserPointerSelectable.Unselected()
    {
        // deactivate visualization when unselected
        SoundEngine0To1Regressor.Deactivate( myRegressor );
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
            maybeNetworked.RPC( "Sound0To1LazyRescan", RpcTarget.Others );
        }
    }

    [PunRPC]
    void Sound0To1LazyRescan()
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
public class Serializable0To1Example
{
    public Vector3 position;
    public SoundEngine0To1Regressor.Parameter type;
    public float value;
}

