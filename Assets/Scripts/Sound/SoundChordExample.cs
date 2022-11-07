using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundChordExample : MonoBehaviour , TouchpadLeftRightClickInteractable , TriggerGrabMoveInteractable , GripPlaceDeleteInteractable , LaserPointerSelectable
{

    // default to 0.5
    [HideInInspector] public int myChord = 0;

    public static int numChords = 6;

    // static: there should only be one classifier
    private static SoundEngineChordClassifier myClassifier = null;

    private TextMesh myText;


    public void Randomize( bool informClassifier = false )
    {
        // pick a new chord
        UpdateMyChord( Random.Range( 0, numChords ) );

        // update classifier
        if( informClassifier )
        {
            Rescan();
        }
    }

    public void Rescan()
    {
        myClassifier.RescanProvidedExamples();
    }

    private void UpdateMyChord( int newChord )
    {
        if( !myText ) { myText = GetComponentInChildren<TextMesh>(); }
        
        // clamp to min / max
        myChord = newChord % numChords;

        // display
        myText.text = string.Format( "Chord: {0}", myChord );
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

    public void Initialize( bool rescan )
    {
        // there should only be one...
        if( myClassifier == null )
        {
            myClassifier = FindObjectOfType<SoundEngineChordClassifier>();
        }

        // update text too
        UpdateMyChord( myChord );

        // inform it
        myClassifier.ProvideExample( this, rescan );
    }

    public void AboutToBeDeleted()
    {
        // inform it
        myClassifier.ForgetExample( this );
    }

    public void InformOfLeftClick()
    {
        UpdateMyChord( myChord + numChords - 1 );
        Rescan();
    }

    public void InformOfRightClick()
    {
        UpdateMyChord( myChord + 1 );
        Rescan();
    }


    public static void ShowHints( float pauseTimeBeforeFade )
    {
        if( myClassifier == null ) { return; }
        foreach( SoundChordExample e in myClassifier.myClassifierExamples )
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

    public SerializableChordExample Serialize()
    {
        SerializableChordExample serial = new SerializableChordExample();
        serial.position = transform.position;
        serial.chord = myChord;
        return serial;
    }

    public void ResetFromSerial( SerializableChordExample serialized )
    {
        transform.position = serialized.position;
        UpdateMyChord( serialized.chord );
    }

    void LaserPointerSelectable.Selected()
    {
        // activate visualization when selected
        SoundEngineChordClassifier.Activate();
    }

    void LaserPointerSelectable.Unselected()
    {
        // deactivate visualization when unselected
        SoundEngineChordClassifier.Deactivate();
    }
}

[System.Serializable]
public class SerializableChordExample
{
    public Vector3 position;
    public int chord;
}

