using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;



public class HighLevelTerrainMethods : HighLevelMethods , TouchpadUpDownInteractable
{
    public enum Method { AverageHeight, HeightVariance, BumpLevel, BumpVariation, TextureVariance };
    public Method method;

    private TextMesh myText;

    


    private float my0To1Value = 0f;
    private float myFloatDisplayValue = 0f;
    private int myIntDisplayValue = 0;
    public int myMinIntValue = 0;
    public int myMaxIntValue = 1;
    public float myMinFloatValue = 0f;
    public float myMaxFloatValue = 10f;


    // specific to methods
    private float _averageHeight = 0f;
    private float _heightDifference = 0f;
    private TerrainGISExample.GISType _baseGISType = TerrainGISExample.GISType.Smooth;
    private float _gisAmount = 0f;
    private List<TerrainGISExample.GISType> _currentlyUsedGISTypes;
    private List<int> _currentlyUsedTextures;
    

    public float sensitivity = 1f;

    // Start is called before the first frame update
    void Start()
    {
        // set text
        myText = GetComponentInChildren<TextMesh>();
        SetTextWithoutValue();
         
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
        myIntDisplayValue = (int) ( my0To1Value.MapClamp( 0, 1, myMinIntValue, myMaxIntValue + 0.99f ) );
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

    void DirectAssignFloatValue( float newValue )
    {
        myFloatDisplayValue = newValue;
        my0To1Value = myFloatDisplayValue.MapClamp( myMinFloatValue, myMaxFloatValue, 0, 1 );
    }

    void DirectAssignIntValue( int newValue )
    {
        myIntDisplayValue = newValue;
        my0To1Value = ((float) newValue + 0.5f).MapClamp( myMinIntValue, myMaxIntValue + 0.99f, 0, 1 );
    }

    protected override void StartAction()
    {
        switch( method )
        {
            case Method.AverageHeight:
                // find current terrain height
                _ScanTerrainHeight();
                break;
            case Method.HeightVariance:
                // find current variance
                _ScanTerrainHeightDifference();
                break;
            case Method.BumpLevel:
                // find current amount
                _ScanGISFeatures();
                break;
            case Method.BumpVariation:
                // find current amount
                _ScanGISFeatures();
                break;
            case Method.TextureVariance:
                // find current used textures
                _ScanTextureVariation();
                break;
        }
        // reset text
        SetMyValue( my0To1Value );
    }

    protected override void StopAction()
    {
        // switch back to text without value
        SetTextWithoutValue();
    }

    void OnDisable()
    {
        StopAction();
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
                _UpdateTerrainHeightDifference( myFloatDisplayValue );
                break;
            case Method.BumpLevel:
                _UpdateGISAmount( myFloatDisplayValue );
                // bump level may have changed due to randomness in method
                _ScanGISFeatures();
                // reset text
                SetMyValue( my0To1Value );
                break;
            case Method.BumpVariation:
                _UpdateGISVariation( myIntDisplayValue );
                break;
            case Method.TextureVariance:
                _UpdateTextureVariation( myIntDisplayValue );
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
        DirectAssignFloatValue( _averageHeight );
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


    void _ScanTerrainHeightDifference()
    {
        // first, find average height
        _ScanTerrainHeight();

        // now, find the average spread
        float sum = 0f;
        foreach( TerrainHeightExample e in currentTerrain.myRegressionExamples )
        {
            sum += Mathf.Abs( e.transform.position.y - _averageHeight );
        }

        // store
        _heightDifference = sum / currentTerrain.myRegressionExamples.Count;
        DirectAssignFloatValue( _heightDifference );
    }

    void _UpdateTerrainHeightDifference( float newDifference )
    {
        float multiplier = newDifference / _heightDifference;

        foreach( TerrainHeightExample e in currentTerrain.myRegressionExamples )
        {
            // scale the delta compared to the average height
            Vector3 pos = e.transform.position;
            float delta = pos.y - _averageHeight;
            pos.y = _averageHeight + multiplier * delta;
            e.transform.position = pos;
        }

        _heightDifference = newDifference;
        currentTerrain.RescanProvidedExamples();
    }

    void _ScanGISFeatures()
    {
        float sum = 0;
        Dictionary<TerrainGISExample.GISType, int> usedTypes = new Dictionary<TerrainGISExample.GISType, int>();
        foreach( TerrainGISExample e in currentTerrain.myGISRegressionExamples )
        {
            // "smooth" is an inverse-type -- the more the value, the less textured it gets
            sum += ( e.myType != TerrainGISExample.GISType.Smooth ) ? e.myValue : ( 1 - e.myValue );
            
            // track
            if( !usedTypes.ContainsKey( e.myType ) )
            {
                usedTypes[e.myType] = 0;
            }
            usedTypes[e.myType]++;
        }

        // compute
        int maxUsedCount = 0;
        foreach( TerrainGISExample.GISType type in usedTypes.Keys )
        {
            if( usedTypes[type] > maxUsedCount )
            {
                maxUsedCount = usedTypes[type];
                _baseGISType = type;
            }
        }
        
        // compute
        _gisAmount = sum / currentTerrain.myGISRegressionExamples.Count;
        _currentlyUsedGISTypes = new List<TerrainGISExample.GISType>( usedTypes.Keys );
        _currentlyUsedGISTypes.OrderByDescending( t => usedTypes[t] );

        // store
        if( method == Method.BumpLevel )
        {
            DirectAssignFloatValue( _gisAmount );
        }
        else if( method == Method.BumpVariation )
        {
            DirectAssignIntValue( _currentlyUsedGISTypes.Count );
        }

    }

    void _UpdateGISAmount( float newAmount )
    {
        float random = 0.08f;

        foreach( TerrainGISExample e in currentTerrain.myGISRegressionExamples )
        {
            // no need to clamp as e.Update clamps
            float v = newAmount + Random.Range( -random, random );
            // special case for smooth
            if( e.myType == TerrainGISExample.GISType.Smooth ) { v = 1 - v; }
            // set
            e.UpdateMyValue( e.myType, v );
        }

        // update base type then update GIS variation to use the new base type
        if( newAmount < 0.25f )
        {
            _baseGISType = TerrainGISExample.GISType.Smooth;
        }   
        else if( newAmount < 0.5f )
        {
            _baseGISType = TerrainGISExample.GISType.Hilly;
        }
        else if( newAmount < 0.75f )
        {
            _baseGISType = TerrainGISExample.GISType.Boulder;
        }
        else
        {
            _baseGISType = TerrainGISExample.GISType.Mountain;
        }

        _UpdateGISVariation( _currentlyUsedGISTypes.Count );
    }

    void _UpdateGISVariation( int newVariation )
    {
        // hacky code because I am not very clever 
        _currentlyUsedGISTypes.Clear();
        _currentlyUsedGISTypes.Add( _baseGISType );
        List<TerrainGISExample.GISType> otherTypes = new List<TerrainGISExample.GISType>();
        otherTypes.Add( _baseGISType.Next() );
        otherTypes.Add( _baseGISType.Next().Next() );
        otherTypes.Add( _baseGISType.Next().Next().Next() );
        otherTypes.Shuffle();
        // pick N to add
        for( int i = 0; i < newVariation - 1; i++ )
        {
            _currentlyUsedGISTypes.Add( otherTypes[i] );
        }

        // reset textures
        List<TerrainGISExample.GISType> bumpsToUse = new List<TerrainGISExample.GISType>();
        // get which textures we will use
        for( int i = 0; i < currentTerrain.myGISRegressionExamples.Count; i++ )
        {
            bumpsToUse.Add( _currentlyUsedGISTypes[ i % _currentlyUsedGISTypes.Count ] );
        }
        // shuffle 
        bumpsToUse.Shuffle();

        // assign
        for( int i = 0; i < currentTerrain.myGISRegressionExamples.Count; i++ )
        {
            currentTerrain.myGISRegressionExamples[i].UpdateMyValue(
                bumpsToUse[i],
                currentTerrain.myGISRegressionExamples[i].myValue
            );
        }

        // rescan
        currentTerrain.RescanProvidedExamples();
    }

    void _ScanTextureVariation()
    {
        Dictionary<int, int> usedTextures = new Dictionary<int, int>();
        ConnectedTerrainTextureController terrain = currentTerrain.GetComponent<ConnectedTerrainTextureController>();
        foreach( TerrainTextureExample e in terrain.myRegressionExamples )
        {            
            // track
            if( !usedTextures.ContainsKey( e.myCurrentValue ) )
            {
                usedTextures[e.myCurrentValue] = 0;
            }
            usedTextures[e.myCurrentValue]++;
        }

        _currentlyUsedTextures = new List<int>( usedTextures.Keys );
        _currentlyUsedTextures.OrderByDescending( t => usedTextures[t] );

        DirectAssignIntValue( _currentlyUsedTextures.Count );
    }

    void _UpdateTextureVariation( int newVariation )
    {
        if( newVariation == _currentlyUsedTextures.Count ) { return; }
        else if( newVariation < _currentlyUsedTextures.Count )
        {
            // keep the first N
            _currentlyUsedTextures.RemoveRange( newVariation, _currentlyUsedTextures.Count - newVariation );
        }
        else if( newVariation > _currentlyUsedTextures.Count )
        {
            // find new ones
            while( _currentlyUsedTextures.Count < newVariation )
            {
                int newElem = Random.Range( myMinIntValue - 1, myMaxIntValue );
                if( !_currentlyUsedTextures.Contains( newElem ) )
                {
                    _currentlyUsedTextures.Add( newElem );
                }
            }
        }


        // reset textures
        ConnectedTerrainTextureController terrain = currentTerrain.GetComponent<ConnectedTerrainTextureController>();
        List<int> texturesToUse = new List<int>();
        // get which textures we will use
        for( int i = 0; i < terrain.myRegressionExamples.Count; i++ )
        {
            texturesToUse.Add( _currentlyUsedTextures[ i % _currentlyUsedTextures.Count ] );
        }
        // shuffle 
        texturesToUse.Shuffle();

        // assign
        for( int i = 0; i < terrain.myRegressionExamples.Count; i++ )
        {
            terrain.myRegressionExamples[i].SwitchTo( texturesToUse[i] );
        }

        terrain.RescanProvidedExamples();
    }
}
