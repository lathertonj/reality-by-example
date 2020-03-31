using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class HighLevelTerrainClickMethods : HighLevelMethods , TouchpadLeftRightClickInteractable
{
    public enum Method { TextureSwap, BumpSwap };
    public Method method;
    public int numCategories = 4;

    private bool shouldRespondToClicks = false;

    // method specific
    List< List< int > > undoMaps;
    int currentUndoIndex = 0;
    

    // Start is called before the first frame update
    void Start()
    {
        undoMaps = new List<List<int>>();
    }

    protected override void StartAction()
    {
        switch( method )
        {
            case Method.TextureSwap:
                break;
            case Method.BumpSwap:
                break;
        }

        shouldRespondToClicks = true;
        undoMaps.Clear();
    }

    protected override void StopAction()
    {
        shouldRespondToClicks = false;
    }


    bool CheckAddUndoMap()
    {
        if( currentUndoIndex < 0 || currentUndoIndex >= undoMaps.Count )
        {
            List<int> shuffledRange = new List<int>( Enumerable.Range( 0, numCategories ) );
            shuffledRange.Shuffle();
            undoMaps.Add( shuffledRange );
            currentUndoIndex = undoMaps.Count - 1;
            return true;
        }
        return false;
    }

    void ProcessUndoMapForward()
    {
        ProcessUndoMap( undoMaps[ currentUndoIndex ] );
    }

    void ProcessUndoMapBackward()
    {
        List<int> forwardVersion = undoMaps[ currentUndoIndex ];
        List<int> backwardVersion = new List<int>( forwardVersion );
        for( int from = 0; from < forwardVersion.Count; from++ )
        {
            int to = forwardVersion[from];
            backwardVersion[to] = from;
        }
        ProcessUndoMap( backwardVersion );
    }

    void ProcessUndoMap( List<int> map )
    {
        switch( method )
        {
            case Method.TextureSwap:
                _SwapTexture( map );
                break;
            case Method.BumpSwap:
                _SwapBump( map );
                break;
        }
    }

    void TouchpadLeftRightClickInteractable.InformOfLeftClick()
    {
        // left click: decrement one in the undo map; if out of range, make a new undo map
        if( CheckAddUndoMap() )
        {
            // we added one -- process it forward
            ProcessUndoMapForward();
        }
        else
        {
            // current undo index is valid --> process it backward to undo it
            ProcessUndoMapBackward();
            // then, we're on the previous one
            currentUndoIndex--;
        }
    }

    void TouchpadLeftRightClickInteractable.InformOfRightClick()
    {
        // right click: increment one in the undo map; if out of range, then make a new undo map
        currentUndoIndex++;
        CheckAddUndoMap();
        
        // always process forward whether we are incrementing to existing or just generated new one
        ProcessUndoMapForward();
    }


    void _SwapTexture( List<int> map )
    {
        ConnectedTerrainTextureController terrain = currentTerrain.GetComponent<ConnectedTerrainTextureController>();
        foreach( TerrainTextureExample e in terrain.myRegressionExamples )
        {
            e.SwitchTo( map[e.myCurrentValue] );
        }
        terrain.RescanProvidedExamples();
    }

    void _SwapBump( List<int> map )
    {
        foreach( TerrainGISExample e in currentTerrain.myGISRegressionExamples )
        {
            int start = _GISToInt( e.myType );
            int next = map[start];
            e.UpdateMyValue( _IntToGIS( next ), e.myValue );
        }
    }

    int _GISToInt( TerrainGISExample.GISType t )
    {
        switch( t )
        {
            case TerrainGISExample.GISType.Smooth:
                return 0;
            case TerrainGISExample.GISType.Hilly:
                return 1;
            case TerrainGISExample.GISType.Boulder:
                return 2;
            case TerrainGISExample.GISType.Mountain:
                return 3;
            default:
                return -1;
        }
    }

    TerrainGISExample.GISType _IntToGIS( int i )
    {
        switch( i )
        {
            case 1:
                return TerrainGISExample.GISType.Hilly;
            case 2:
                return TerrainGISExample.GISType.Boulder;
            case 3:
                return TerrainGISExample.GISType.Mountain;
            case 0:
            default:
                return TerrainGISExample.GISType.Smooth;
        }
    }
}
