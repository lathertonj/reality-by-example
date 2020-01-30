using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class RandomizeTerrain : MonoBehaviour 
{

    public enum ActionType { RandomizeAll, RandomizeCurrent, PerturbCurrent, CopyCurrent };
    public ActionType currentAction = ActionType.RandomizeAll;


    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean gripPress;

    ConnectedTerrainController[] terrainHeightControllers;
    ConnectedTerrainTextureController[] terrainTextureControllers;

    public Vector3 landRadius = new Vector3( 50, 100, 50 );
    public Vector3 musicRadius = new Vector3( 150, 100, 150 );
    public int heightExamples = 5, bumpExamples = 5, textureExamples = 5, musicalParamExamples = 5;

    private List< TerrainHeightExample >[] myHeightExamples;
    private List< TerrainGISExample >[] myBumpExamples;
    private List< TerrainTextureExample >[] myTextureExamples;
    private List< SoundChordExample > myChordExamples;
    private List< SoundTempoExample > myTempoExamples;
    private List< Sound0To1Example > myDensityExamples, myTimbreExamples, myVolumeExamples;

    public TerrainHeightExample heightPrefab;
    public TerrainGISExample bumpPrefab;
    public TerrainTextureExample texturePrefab;
    public SoundChordExample chordPrefab;
    public SoundTempoExample tempoPrefab;
    public Sound0To1Example densityPrefab, timbrePrefab, volumePrefab;
    
    private bool currentlyComputing = true;


    // Start is called before the first frame update
    void Start()
    {
        terrainHeightControllers = FindObjectsOfType<ConnectedTerrainController>();
        terrainTextureControllers = new ConnectedTerrainTextureController[ terrainHeightControllers.Length ];
        myHeightExamples = new List<TerrainHeightExample>[ terrainHeightControllers.Length ];
        myBumpExamples = new List<TerrainGISExample>[ terrainHeightControllers.Length ];
        myTextureExamples = new List<TerrainTextureExample>[ terrainHeightControllers.Length ];
        for( int i = 0; i < terrainHeightControllers.Length; i++ )
        {
            terrainTextureControllers[i] = terrainHeightControllers[i].GetComponent< ConnectedTerrainTextureController >();
            myHeightExamples[i] = new List<TerrainHeightExample>();
            myBumpExamples[i] = new List<TerrainGISExample>();
            myTextureExamples[i] = new List<TerrainTextureExample>();
        }
        myChordExamples = new List<SoundChordExample>();
        myTempoExamples = new List<SoundTempoExample>();
        myDensityExamples = new List<Sound0To1Example>();
        myTimbreExamples = new List<Sound0To1Example>();
        myVolumeExamples = new List<Sound0To1Example>();

        StartCoroutine( InitializeAll() );
    }

    void Update()
    {
        if( gripPress.GetStateDown( handType ) )
        {
            if( ShouldRandomize() )
            {
                TakeAction();
            }
        }
    }

    bool ShouldRandomize()
    {
        // disallow randomization during initialization
        return !currentlyComputing;
    }

    void TakeAction()
    {
        switch( currentAction )
        {
            case ActionType.RandomizeAll:
                // randomize all terrains and musical parameters
                StartCoroutine( ReRandomizeAll() );
                break;
            case ActionType.RandomizeCurrent:
                // find the terrain we're above

                // randomize it

                break;
            case ActionType.PerturbCurrent:
                // find the terrain we're above

                // perturb it
                break;
            case ActionType.CopyCurrent:
                // find the terrain we're above

                // find the terrain we're laser pointing to

                // transfer properties

                break;
        }
    }

    IEnumerator InitializeAll()
    {
        currentlyComputing = true;
        yield return StartCoroutine( InitializeTerrainHeights() );
        yield return StartCoroutine( InitializeTerrainBumps() );
        yield return StartCoroutine( InitializeTerrainTextures() );
        InitializeMusicalParameters();
        currentlyComputing = false;
    }

    IEnumerator InitializeTerrainHeights()
    {
        // for each terrain
        for( int i = 0; i < terrainHeightControllers.Length; i++ )
        {
            // generate N points in random locations, without scanning the terrain
            for( int j = 0; j < heightExamples; j++ )
            {
                TerrainHeightExample e = Instantiate( 
                    heightPrefab,
                    terrainHeightControllers[i].transform.position + GetRandomLocationWithinRadius( landRadius ),
                    Quaternion.identity
                );

                // ~ JustPlaced
                e.ManuallySpecifyTerrain( terrainHeightControllers[i] );

                terrainHeightControllers[i].ProvideExampleEfficient( e );

                // remember
                myHeightExamples[i].Add( e );
            }
            // then rescan the terrain (lazy = true, compute frames = 15)
            // int computeFrames = 15;
            // terrainHeightControllers[i].RescanProvidedExamples( true, computeFrames );

            // // wait before moving on
            // for( int f = 0; f < computeFrames + 1; f++ ) { yield return null; }
            yield return null;
        }


    }

    IEnumerator InitializeTerrainBumps()
    {
        // for each terrain
        for( int i = 0; i < terrainHeightControllers.Length; i++ )
        {
            // generate N points in random locations
            for( int j = 0; j < bumpExamples; j++ )
            {
                TerrainGISExample b = Instantiate( 
                    bumpPrefab,
                    terrainHeightControllers[i].transform.position + GetRandomLocationWithinRadius( landRadius ),
                    Quaternion.identity 
                );

                // ~ JustPlaced
                b.ManuallySpecifyTerrain( terrainHeightControllers[i] );

                // for each point, randomize as well its type and intensity
                b.Randomize();

                // inform terrain of example
                terrainHeightControllers[i].ProvideExampleEfficient( b );

                // remember
                myBumpExamples[i].Add( b );
            }

            // then rescan the terrain (lazy = false, compute frames = 15)
            int computeFrames = 15;
            terrainHeightControllers[i].RescanProvidedExamples( false, computeFrames );

            // wait before moving on
            for( int f = 0; f < computeFrames + 1; f++ ) { yield return null; }
        }
    }

    IEnumerator InitializeTerrainTextures()
    {
        // for each terrain
        for( int i = 0; i < terrainTextureControllers.Length; i++ )
        {
            // generate N points in random locations
            for( int j = 0; j < bumpExamples; j++ )
            {
                TerrainTextureExample t = Instantiate( 
                    texturePrefab,
                    terrainHeightControllers[i].transform.position + GetRandomLocationWithinRadius( landRadius ),
                    Quaternion.identity 
                );
                
                // ~ JustPlaced()
                t.ManuallySpecifyTerrain( terrainTextureControllers[i] );
                
                // for each point, randomize as well its type and intensity
                t.Randomize();

                // inform terrain of example (shouldRetrain = false)
                terrainTextureControllers[i].ProvideExample( t, false );

                // remember
                myTextureExamples[i].Add( t );
            }
            // rescan terrain
            terrainTextureControllers[i].RescanProvidedExamples();

            // wait a frame before doing the next one
            yield return null;
        }
    }

    void InitializeMusicalParameters()
    {
        // for each parameter, initialize n points and randomize their values
        // volume:
        for( int i = 0; i < musicalParamExamples; i++ )
        {
            Sound0To1Example v = Instantiate( volumePrefab, GetRandomLocationWithinRadius( musicRadius ), Quaternion.identity );
            v.Initialize();
            v.Randomize();
            myVolumeExamples.Add( v );
        }
        // rescan to check for new random values
        SoundEngine0To1Regressor.volumeRegressor.RescanProvidedExamples();

        // density:
        for( int i = 0; i < musicalParamExamples; i++ )
        {
            Sound0To1Example d = Instantiate( densityPrefab, GetRandomLocationWithinRadius( musicRadius ), Quaternion.identity );
            d.Initialize();
            d.Randomize();
            myDensityExamples.Add( d );
        }
        // rescan to check for new random values
        SoundEngine0To1Regressor.densityRegressor.RescanProvidedExamples();

        // timbre:
        for( int i = 0; i < musicalParamExamples; i++ )
        {
            Sound0To1Example t = Instantiate( timbrePrefab, GetRandomLocationWithinRadius( musicRadius ), Quaternion.identity );
            t.Initialize();
            t.Randomize();
            myTimbreExamples.Add( t );
        }
        // rescan to check for new random values
        SoundEngine0To1Regressor.timbreRegressor.RescanProvidedExamples();

        // tempo:
        for( int i = 0; i < musicalParamExamples; i++ )
        {
            SoundTempoExample t = Instantiate( tempoPrefab, GetRandomLocationWithinRadius( musicRadius ), Quaternion.identity );
            t.Initialize();
            t.Randomize();
            myTempoExamples.Add( t );
        }
        // rescan to check for new random values
        myTempoExamples[0].Rescan();

        // chord:
        for( int i = 0; i < musicalParamExamples; i++ )
        {
            SoundChordExample c = Instantiate( chordPrefab, GetRandomLocationWithinRadius( musicRadius ), Quaternion.identity );
            c.Initialize();
            c.Randomize();
            myChordExamples.Add( c );
        }
        // rescan to check for new random values
        myChordExamples[0].Rescan();

        
    }

    IEnumerator ReRandomizeAll()
    {
        for( int i = 0; i < terrainHeightControllers.Length; i++ )
        {
            yield return StartCoroutine( ReRandomizeTerrain( i ) );
        }
        ReRandomizeMusicalParameters();
    }

    IEnumerator ReRandomizeTerrain( int which )
    {
        currentlyComputing = true;

        ReRandomizeTerrainHeight( which );
        ReRandomizeTerrainBumpiness( which );
        ReRandomizeTerrainTexture( which );

        // rescan the terrain (lazy = false, compute frames = 15)
        int computeFrames = 15;
        terrainHeightControllers[which].RescanProvidedExamples( false, computeFrames );
        for( int i = 0; i < computeFrames + 1; i++ ) { yield return null; }

        currentlyComputing = false;
    }

    void ReRandomizeTerrainHeight( int which )
    {
        // generate N points in random locations
        for( int j = 0; j < myHeightExamples[which].Count; j++ )
        {
            myHeightExamples[which][j].transform.position = 
                terrainHeightControllers[which].transform.position + GetRandomLocationWithinRadius( landRadius );
            // if it had something to randomize
            // myHeightExamples[which][j].Randomize();
        }
        // We don't rescan the terrain because we will do it after the bumps are randomized too
    }

    void ReRandomizeTerrainBumpiness( int which )
    {
        // generate N points in random locations
        for( int j = 0; j < myBumpExamples[which].Count; j++ )
        {
            // position
            myBumpExamples[which][j].transform.position = 
                terrainHeightControllers[which].transform.position + GetRandomLocationWithinRadius( landRadius );
            // stats
            myBumpExamples[which][j].Randomize();
        }
        // We don't rescan the terrain because we will do it in the above function
    }

    void ReRandomizeTerrainTexture( int which )
    {
        // generate N points in random locations
        for( int j = 0; j < myTextureExamples[which].Count; j++ )
        {
            // position
            myTextureExamples[which][j].transform.position = 
                terrainHeightControllers[which].transform.position + GetRandomLocationWithinRadius( landRadius );
            // stats
            myTextureExamples[which][j].Randomize();
        }
        // We don't rescan the terrain because we will do it in the above function
    }

    void ReRandomizeMusicalParameters()
    {
        // volume:
        for( int i = 0; i < myVolumeExamples.Count; i++ )
        {
            myVolumeExamples[i].Randomize();
            myVolumeExamples[i].transform.position = GetRandomLocationWithinRadius( musicRadius );
        }
        // rescan to check for new random values
        SoundEngine0To1Regressor.volumeRegressor.RescanProvidedExamples();

        // density:
        for( int i = 0; i < myDensityExamples.Count; i++ )
        {
            myDensityExamples[i].Randomize();
            myDensityExamples[i].transform.position = GetRandomLocationWithinRadius( musicRadius );
        }
        // rescan to check for new random values
        SoundEngine0To1Regressor.densityRegressor.RescanProvidedExamples();

        // timbre:
        for( int i = 0; i < myTimbreExamples.Count; i++ )
        {
            myTimbreExamples[i].Randomize();
            myTimbreExamples[i].transform.position = GetRandomLocationWithinRadius( musicRadius );
        }
        // rescan to check for new random values
        SoundEngine0To1Regressor.timbreRegressor.RescanProvidedExamples();

        // tempo:
        for( int i = 0; i < myTempoExamples.Count; i++ )
        {
            myTempoExamples[i].Randomize();
            myTempoExamples[i].transform.position = GetRandomLocationWithinRadius( musicRadius );
        }
        // rescan to check for new random values
        myTempoExamples[0].Rescan();

        // chord:
        for( int i = 0; i < myChordExamples.Count; i++ )
        {
            myChordExamples[i].Randomize();
            myChordExamples[i].transform.position = GetRandomLocationWithinRadius( musicRadius );
        }
        // rescan to check for new random values
        myChordExamples[0].Rescan();
    }

    Vector3 GetRandomLocationWithinRadius( Vector3 radius )
    {
        return new Vector3(
            Random.Range( -radius.x, radius.x ),
            Random.Range( 0, radius.y ),
            Random.Range( -radius.z, radius.z )
        );
    }


}
