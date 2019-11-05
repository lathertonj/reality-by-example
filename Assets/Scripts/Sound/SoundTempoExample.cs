﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundTempoExample : MonoBehaviour , TouchpadUpDownInteractable , TriggerGrabMoveInteractable , GripPlaceDeleteInteractable
{

    // default to 100
    [HideInInspector] public float myTempo = 100;

    private SoundEngineTempoRegressor myRegressor;

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
        myRegressor.RescanProvidedExamples();
    }

    private void UpdateMyTempo( float newTempo )
    {
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
        myRegressor.RescanProvidedExamples();
    }

    public void JustPlaced()
    {
        // there should only be one...
        myRegressor = FindObjectOfType<SoundEngineTempoRegressor>();

        // inform it
        myRegressor.ProvideExample( this );

        // update text too
        myText = GetComponentInChildren<TextMesh>();
        UpdateMyTempo( myTempo );
    }

    public void AboutToBeDeleted()
    {
        // inform it
        myRegressor.ForgetExample( this );
    }
}