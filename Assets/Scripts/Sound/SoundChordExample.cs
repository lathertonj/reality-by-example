using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundChordExample : MonoBehaviour , TouchpadLeftRightClickInteractable , TriggerGrabMoveInteractable , GripPlaceDeleteInteractable
{

    // default to 0.5
    [HideInInspector] public int myChord = 0;

    public static int numChords = 6;

    private SoundEngineChordClassifier myClassifier;

    private TextMesh myText;




    private void UpdateMyChord( int newChord )
    {
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
        myClassifier.RescanProvidedExamples();
    }

    public void JustPlaced()
    {
        // there should only be one...
        myClassifier = FindObjectOfType<SoundEngineChordClassifier>();

        // inform it
        myClassifier.ProvideExample( this );

        // update text too
        myText = GetComponentInChildren<TextMesh>();
        UpdateMyChord( myChord );
    }

    public void AboutToBeDeleted()
    {
        // inform it
        myClassifier.ForgetExample( this );
    }

    public void InformOfLeftClick()
    {
        UpdateMyChord( myChord + numChords - 1 );
        myClassifier.RescanProvidedExamples();
    }

    public void InformOfRightClick()
    {
        UpdateMyChord( myChord + 1 );
        myClassifier.RescanProvidedExamples();
    }
}
