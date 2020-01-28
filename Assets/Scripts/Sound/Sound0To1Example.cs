using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sound0To1Example : MonoBehaviour , TouchpadUpDownInteractable , TriggerGrabMoveInteractable , GripPlaceDeleteInteractable
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

    private void UpdateMyValue( float newValue )
    {
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
        Initialize();

        // inform it
        myRegressor.ProvideExample( this );
    }

    public void Initialize()
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
        myText = GetComponentInChildren<TextMesh>();
        UpdateMyValue( myValue );
    }

    public void AboutToBeDeleted()
    {
        // inform it
        myRegressor.ForgetExample( this );
    }
}
