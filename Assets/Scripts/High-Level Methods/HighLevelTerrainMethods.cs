using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighLevelTerrainMethods : MonoBehaviour , LaserPointerSelectable , TouchpadUpDownInteractable
{
    public enum Method { AverageHeight, HeightVariance, BumpLevel, BumpVariation, TextureVariance };
    public Method method;

    private TextMesh myText;

    private bool lookForTerrain = false;
    private ConnectedTerrainController currentTerrain = null;


    private float my0To1Value = 0f;
    private float myFloatDisplayValue = 0f;
    private int myIntDisplayValue = 0;
    public int myMaxIntValue = 1;
    public float myMinFloatValue = 0f;
    public float myMaxFloatValue = 10f;


    // specific to average height
    private float _averageHeight = 0f;
    

    public float sensitivity = 1f;

    // Start is called before the first frame update
    void Start()
    {
        // set text
        myText = GetComponentInChildren<TextMesh>();
        SetTextWithoutValue();
         
    }

    // Update is called once per frame
    void Update()
    {
        if( lookForTerrain )
        {
            ConnectedTerrainController newTerrain = TerrainUtility.FindTerrain<ConnectedTerrainController>( transform.position );
            // did we find a new terrain?
            if( newTerrain != currentTerrain )
            {
                // stop what we were doing
                if( currentTerrain != null )
                {
                    StopAction();
                }

                // remember it
                currentTerrain = newTerrain;

                // start a new thing                
                StartAction();
            }
        }
    }

    void SetTextWithoutValue()
    {
        switch( method )
        {
            case Method.AverageHeight:
                myText.text = "Average Height";
                break;
            case Method.HeightVariance:
                myText.text = "Height Range";
                break;
            case Method.BumpLevel:
                myText.text = "Bumpiness Amount";
                break;
            case Method.BumpVariation:
                myText.text = "Bumpiness Variation";
                break;
            case Method.TextureVariance:
                myText.text = "Texture Variation";
                break;
        }
    }

    void SetMyValue( float newValue )
    {
        my0To1Value = Mathf.Clamp01( newValue );
        myFloatDisplayValue = my0To1Value.Map( 0, 1, myMinFloatValue, myMaxFloatValue );
        myIntDisplayValue = (int) ( my0To1Value * ( myMaxIntValue ) );
        string newText = "";
        
        switch( method )
        {
            case Method.AverageHeight:
                newText = string.Format( "Average Height:\n{0}", myFloatDisplayValue );
                break;
            case Method.HeightVariance:
                newText = string.Format( "Height Range:\n{0}", myFloatDisplayValue );
                break;
            case Method.BumpLevel:
                newText = string.Format( "Bumpiness Amount:\n{0}", myFloatDisplayValue );
                break;
            case Method.BumpVariation:
                newText = string.Format( "Bumpiness Variation:\n{0}", myIntDisplayValue );
                break;
            case Method.TextureVariance:
                newText = string.Format( "Texture Variation:\n{0}", myIntDisplayValue );
                break;
        }

        myText.text = newText;
    }

    void StartAction()
    {
        switch( method )
        {
            case Method.AverageHeight:
                // find current terrain height
                _ScanTerrainHeight();
                // reset text
                SetMyValue( my0To1Value );
                break;
            case Method.HeightVariance:
                break;
            case Method.BumpLevel:
                break;
            case Method.BumpVariation:
                break;
            case Method.TextureVariance:
                break;
        }
    }

    void StopAction()
    {
        // switch back to text without value
        SetTextWithoutValue();
    }

    void LaserPointerSelectable.Selected()
    {
        // find a terrain under me, continuously
        lookForTerrain = true;
    }

    void LaserPointerSelectable.Unselected()
    {
        // stop action in progress
        if( currentTerrain != null ) 
        { 
            StopAction();
        }

        // stop looking for terrain
        lookForTerrain = false;
        currentTerrain = null;
    }

    void TouchpadUpDownInteractable.InformOfUpOrDownMovement( float verticalDisplacementSinceBeginning, float verticalDisplacementThisFrame )
    {
        // update the internal number and the visual display
        float delta = verticalDisplacementThisFrame.MapClamp( -0.1f, 0.1f, -0.05f, 0.05f ) * sensitivity;
        SetMyValue( my0To1Value + delta );
    }

    void TouchpadUpDownInteractable.FinalizeMovement()
    {
        // take some action to change the value permanently
        switch( method )
        {
            case Method.AverageHeight:
                _UpdateTerrainHeight( myFloatDisplayValue );
                break;
            case Method.HeightVariance:
                break;
            case Method.BumpLevel:
                break;
            case Method.BumpVariation:
                break;
            case Method.TextureVariance:
                break;
        }
    }


    void _ScanTerrainHeight()
    {   
        // calculate sum
        float sum = 0f;
        foreach( TerrainHeightExample e in currentTerrain.myRegressionExamples )
        {
            sum += e.transform.position.y;
        }
        
        _averageHeight = sum / currentTerrain.myRegressionExamples.Count;

        // update internals
        myFloatDisplayValue = _averageHeight;
        my0To1Value = myFloatDisplayValue.MapClamp( myMinFloatValue, myMaxFloatValue, 0, 1 );

    }

    void _UpdateTerrainHeight( float newAverage )
    {
        Vector3 delta = ( newAverage - _averageHeight ) * Vector3.up;

        foreach( TerrainHeightExample e in currentTerrain.myRegressionExamples )
        {
            e.transform.position += delta;
        }

        currentTerrain.RescanProvidedExamples();

        _averageHeight = newAverage;
    }
}
