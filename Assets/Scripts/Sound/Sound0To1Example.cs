using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Sound0To1Example : MonoBehaviour , TouchpadUpDownInteractable , TriggerGrabMoveInteractable , GripPlaceDeleteInteractable , LaserPointerSelectable , IPunInstantiateMagicCallback
{

    // default to 0.5
    [HideInInspector] public float myValue = 0.5f;
    public SoundEngine0To1Regressor.Parameter myType;

    private SoundEngine0To1Regressor myRegressor;

    private TextMesh myText;

    public void InformOfUpOrDownMovement( float verticalDisplacementSinceBeginning, float verticalDisplacementThisFrame )
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

    public void FinalizeMovement()
    {
        // tell the controller to recompute parameter
        myRegressor.RescanProvidedExamples();
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
    }

    public void JustPlaced()
    {
        Initialize( true );
    }

    void IPunInstantiateMagicCallback.OnPhotonInstantiate( PhotonMessageInfo info )
    {
        // check who this came from
        PhotonView photonView = GetComponent<PhotonView>();
        if( !photonView.IsMine && PhotonNetwork.IsConnected )
        {
            Initialize( rescan: false );
        }
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
}

[System.Serializable]
public class Serializable0To1Example
{
    public Vector3 position;
    public SoundEngine0To1Regressor.Parameter type;
    public float value;
}

