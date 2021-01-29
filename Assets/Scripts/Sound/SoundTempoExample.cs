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
        UpdateMyTempo( multiplier * myTempo );
    }

    public void FinalizeMovement()
    {
        // tell the controller to recompute tempo
        Rescan();
    }

    public void Randomize( bool informRegressor = false )
    {
        // pick a new value 
        UpdateMyTempo( Random.Range( minTempo, maxTempo ) );

        // inform regressor
        if( informRegressor )
        {
            Rescan();
        }
    }

    public void Rescan()
    {
        myRegressor.RescanProvidedExamples();
    }

    public void UpdateMyTempo( float newTempo )
    {
        if( !myText ) { myText = GetComponentInChildren<TextMesh>(); }

        // clamp to min / max
        myTempo = Mathf.Clamp( newTempo, minTempo, maxTempo );

        // display
        myText.text = string.Format( "{0:0.00} BPM", myTempo );
    }

    public void InformOfTemporaryMovement( Vector3 currentPosition )
    {
        // don't care
        // TODO or, could update algorithm. this might be overkill perhaps
    }
    public void FinalizeMovement( Vector3 endPosition )
    {
        // tell the controller to recompute tempo
        Rescan();
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

    void IPhotonExample.AlertOthersToChanges()
    {
        throw new System.NotImplementedException();
    }
}

[System.Serializable]
public class SerializableTempoExample
{
    public Vector3 position;
    public float tempo;
}

