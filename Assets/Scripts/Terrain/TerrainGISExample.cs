using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGISExample : MonoBehaviour, TouchpadUpDownInteractable, TouchpadLeftRightClickInteractable, TriggerGrabMoveInteractable, GripPlaceDeleteInteractable
{
    public enum GISType { Smooth = 0, Hilly = 1, River = 2, Boulder = 3, Mountain = 4, Spines = 5 };
    

    [HideInInspector] public double[] myValues = new double[6];

    [HideInInspector] private ConnectedTerrainController myTerrain;
    // default to 1.0
    [HideInInspector] public float myValue = 1.0f;
    [HideInInspector] public GISType myType = GISType.Smooth;

    private TextMesh myText;

    void Awake()
    {
        myText = GetComponentInChildren<TextMesh>();
        for( int i = 0; i < myValues.Length; i++ ) { myValues[i] = 0; }
        UpdateMyValue( myType, myValue );
    }



    private void UpdateMyValue( GISType newType, float newValue )
    {
        // set previous one to zero
        myValues[ (int) myType ] = 0;

        // store
        myType = newType;

        // clamp to min / max
        myValue = Mathf.Clamp01( newValue );

        // store
        myValues[ (int) myType ] = myValue;

        // display according to mode
        switch( myType )
        {
            case GISType.Smooth:
                myText.text = string.Format( "Smooth: {0:0.00}", myValue );
                break;
            case GISType.Hilly:
                myText.text = string.Format( "Hilly: {0:0.00}", myValue );
                break;
            case GISType.River:
                myText.text = string.Format( "River: {0:0.00}", myValue );
                break;
            case GISType.Boulder:
                myText.text = string.Format( "Boulder: {0:0.00}", myValue );
                break;
            case GISType.Mountain:
                myText.text = string.Format( "Mountain: {0:0.00}", myValue );
                break;
            case GISType.Spines:
                myText.text = string.Format( "Spines: {0:0.00}", myValue );
                break;
        }
    }

    private void SwitchToNextGISType()
    {
        UpdateMyValue( myType.Next(), myValue );
    }

    private void SwitchToPreviousGISType()
    {
        UpdateMyValue( myType.Previous(), myValue );
    }

    private ConnectedTerrainController FindTerrain()
    {
        // Bit shift the index of the layer (8: Connected terrains) to get a bit mask
        int layerMask = 1 << 8;

        RaycastHit hit;
        // Check from a point really high above us, in the downward direction (in case we are below terrain)
        if( Physics.Raycast( transform.position + 400 * Vector3.up, Vector3.down, out hit, Mathf.Infinity, layerMask ) )
        {
            return hit.transform.GetComponentInParent<ConnectedTerrainController>();
        }
        return null;
    }

    void GripPlaceDeleteInteractable.JustPlaced()
    {
        myTerrain = FindTerrain();

        if( myTerrain == null )
        {
            Destroy( gameObject );
        }
        else
        {
            myTerrain.ProvideExample( this );
        }
    }

    public void ManuallySpecifyTerrain( ConnectedTerrainController c )
    {
        myTerrain = c;
    }

    void GripPlaceDeleteInteractable.AboutToBeDeleted()
    {
        myTerrain.ForgetExample( this );
    }

    void TouchpadLeftRightClickInteractable.InformOfLeftClick()
    {
        SwitchToPreviousGISType();
        myTerrain.RescanProvidedExamples();
    }

    void TouchpadLeftRightClickInteractable.InformOfRightClick()
    {
        SwitchToNextGISType();
        myTerrain.RescanProvidedExamples();
    }

    void TriggerGrabMoveInteractable.InformOfTemporaryMovement( Vector3 currentPosition )
    {
        // do nothing (don't update terrain while moving temporarily)
    }

    void TriggerGrabMoveInteractable.FinalizeMovement( Vector3 endPosition )
    {
        // see if we're on a new terrain
        ConnectedTerrainController newTerrain = FindTerrain();
        if( newTerrain != null && newTerrain != myTerrain )
        {
            myTerrain.ForgetExample( this );
            newTerrain.ProvideExample( this );
            myTerrain = newTerrain;
        }
        else
        {
            // stick with myTerrain
            myTerrain.RescanProvidedExamples();
        }
    }



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
        UpdateMyValue( myType, multiplier * myValue );
    }

    public void FinalizeMovement()
    {
        // tell the controller to recompute tempo
        myTerrain.RescanProvidedExamples();
    }


    public void Randomize( bool informMyTerrain = false )
    {
        // get random new value
        UpdateMyValue( myType, Random.Range( 0f, 1f ) );

        // get random new type (future proofed and thus somewhat skewed)
        int numSwitches = Random.Range( 0, 8 );
        for( int i = 0; i < numSwitches; i++ ) { SwitchToNextGISType(); }

        // inform my terrain
        if( informMyTerrain )
        {
            myTerrain.RescanProvidedExamples();
        }
    }


    public void Perturb( float amount, bool informMyTerrain = false )
    {
        // get random new value
        UpdateMyValue( myType, myValue + Random.Range( -amount, amount ) );

        // don't get new type -- too drastic

        // inform my terrain
        if( informMyTerrain )
        {
            myTerrain.RescanProvidedExamples();
        }
    }

    public void CopyFrom( TerrainGISExample other )
    {
        UpdateMyValue( other.myType, other.myValue );
    }
    
}

public static class GISExtensions
{
    // next and previous methods: this allows us to potentially eliminate some of the ones if we want to
    public static TerrainGISExample.GISType Next( this TerrainGISExample.GISType myEnum )
    {
        switch( myEnum )
        {
            case TerrainGISExample.GISType.Smooth:
                return TerrainGISExample.GISType.Hilly;
            case TerrainGISExample.GISType.Hilly:
                // disable river
                // return TerrainGISExample.GISType.River;
                return TerrainGISExample.GISType.Boulder;
            case TerrainGISExample.GISType.River:
                return TerrainGISExample.GISType.Boulder;
            case TerrainGISExample.GISType.Boulder:
                return TerrainGISExample.GISType.Mountain;
            case TerrainGISExample.GISType.Mountain:
                // disable spines
                // return TerrainGISExample.GISType.Spines;
                return TerrainGISExample.GISType.Smooth;
            case TerrainGISExample.GISType.Spines:
                return TerrainGISExample.GISType.Smooth;
            default:
                return TerrainGISExample.GISType.Smooth;
        }
    }

    // next and previous methods: this allows us to potentially eliminate some of the ones if we want to
    public static TerrainGISExample.GISType Previous( this TerrainGISExample.GISType myEnum )
    {
        switch( myEnum )
        {
            case TerrainGISExample.GISType.Smooth:
                // disable spines
                // return TerrainGISExample.GISType.Spines;
                return TerrainGISExample.GISType.Mountain;
            case TerrainGISExample.GISType.Hilly:
                return TerrainGISExample.GISType.Smooth;
            case TerrainGISExample.GISType.River:
                return TerrainGISExample.GISType.Hilly;
            case TerrainGISExample.GISType.Boulder:
                // disable river
                // return TerrainGISExample.GISType.River;
                return TerrainGISExample.GISType.Hilly;
            case TerrainGISExample.GISType.Mountain:
                return TerrainGISExample.GISType.Boulder;
            case TerrainGISExample.GISType.Spines:
                return TerrainGISExample.GISType.Mountain;
            default:
                return TerrainGISExample.GISType.Smooth;
        }
    }
}