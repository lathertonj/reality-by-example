using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundTimbreExample : MonoBehaviour , TouchpadUpDownInteractable , TriggerGrabMoveInteractable , GripPlaceDeleteInteractable
{

    // default to 0.5
    [HideInInspector] public float myTimbre = 0.5f;

    private SoundEngineTimbreRegressor myRegressor;

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
        UpdateMyTempo( multiplier * myTimbre );
    }

    public void FinalizeMovement()
    {
        // tell the controller to recompute tempo
        myRegressor.RescanProvidedExamples();
    }

    private void UpdateMyTempo( float newTempo )
    {
        // clamp to min / max
        myTimbre = Mathf.Clamp01( newTempo );

        // display
        myText.text = string.Format( "Timbre: {0:0.00}", myTimbre );
    }

    public void InformOfTemporaryMovement( Vector3 currentPosition )
    {
        // don't care
        // TODO or, could update algorithm. this might be overkill perhaps
    }
    public void FinalizeMovement( Vector3 endPosition )
    {
        // tell the controller to recompute tempo
        myRegressor.RescanProvidedExamples();
    }

    public void JustPlaced()
    {
        // there should only be one...
        myRegressor = FindObjectOfType<SoundEngineTimbreRegressor>();

        // inform it
        myRegressor.ProvideExample( this );

        // update text too
        myText = GetComponentInChildren<TextMesh>();
        UpdateMyTempo( myTimbre );
    }

    public void AboutToBeDeleted()
    {
        // inform it
        myRegressor.ForgetExample( this );
    }
}
