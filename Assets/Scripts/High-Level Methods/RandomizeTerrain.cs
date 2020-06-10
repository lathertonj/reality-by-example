using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class RandomizeTerrain : MonoBehaviour 
{

    public enum ActionType { RandomizeAll, RandomizeCurrent, PerturbBig, PerturbSmall, Copy, DoNothing };
    public ActionType currentAction = ActionType.RandomizeAll;
    private enum RandomizeAmount { Full, PerturbBig, PerturbSmall };


    public SteamVR_Input_Sources currentHand;
    public SteamVR_Action_Boolean gripPress;

    public LaserPointerDragAndDrop currentLaser;

    ConnectedTerrainController[] terrainHeightControllers;
    ConnectedTerrainTextureController[] terrainTextureControllers;

    public SoundEngine soundEngine;
    private SoundEngineChordClassifier chordClassifier;
    private SoundEngineTempoRegressor tempoRegressor;
    private SoundEngine0To1Regressor timbreRegressor, densityRegressor, volumeRegressor; 

    public Vector3 landRadius = new Vector3( 50, 100, 50 );
    public Vector3 musicRadius = new Vector3( 150, 100, 150 );
    public Vector3 perturbBigRadius = new Vector3( 5, 5, 5 );
    public Vector3 perturbSmallRadius = new Vector3( 0.2f, 0.2f, 0.2f );
    public float perturbBigBumpRange = 0.2f;
    public float perturbSmallBumpRange = 0.02f;
    public int heightExamples = 5, bumpExamples = 5, textureExamples = 5, musicalParamExamples = 5;

    public TerrainHeightExample heightPrefab;
    public TerrainGISExample bumpPrefab;
    public TerrainTextureExample texturePrefab;
    public SoundChordExample chordPrefab;
    public SoundTempoExample tempoPrefab;
    public Sound0To1Example densityPrefab, timbrePrefab, volumePrefab;
    
    private bool currentlyComputing = false;

    private Dictionary< ConnectedTerrainController, int > indices;

    public bool randomizeOnStart = true;

    private static RandomizeTerrain theRandomizer;

    public static IEnumerator RandomizeWorld()
    {
        return theRandomizer.InitializeAll();
    }

    void Awake()
    {
        theRandomizer = this;
    }


    // Start is called before the first frame update
    void Start()
    {
        terrainHeightControllers = FindObjectsOfType<ConnectedTerrainController>();
        terrainTextureControllers = new ConnectedTerrainTextureController[ terrainHeightControllers.Length ];
        indices = new Dictionary<ConnectedTerrainController, int>();
        for( int i = 0; i < terrainHeightControllers.Length; i++ )
        {
            terrainTextureControllers[i] = terrainHeightControllers[i].GetComponent< ConnectedTerrainTextureController >();
            indices[terrainHeightControllers[i]] = i;
        }

        // get references to the audio ML
        chordClassifier = soundEngine.GetComponent<SoundEngineChordClassifier>();
        tempoRegressor = soundEngine.GetComponent<SoundEngineTempoRegressor>();
        foreach( SoundEngine0To1Regressor r in soundEngine.GetComponents<SoundEngine0To1Regressor>() )
        {
            switch( r.myParameter )
            {
                case SoundEngine0To1Regressor.Parameter.Timbre:
                    timbreRegressor = r;
                    break;
                case SoundEngine0To1Regressor.Parameter.Density:
                    densityRegressor = r;
                    break;
                case SoundEngine0To1Regressor.Parameter.Volume:
                    volumeRegressor = r;
                    break;
            }
        }

        if( randomizeOnStart )
        {
            StartCoroutine( RandomizeWorld() );
        }
    }

    void Update()
    {
        if( ShouldRandomize() )
        {
            if( gripPress.GetStateDown( currentHand ) )
            {
                TakeGripAction();
            }
        }

        if( currentAction == ActionType.Copy )
        {
            if( gripPress.GetStateUp( currentHand ) )
            {
                Copy( currentLaser );
            }
        }
    }

    bool ShouldRandomize()
    {
        // disallow randomization during initialization
        return !currentlyComputing && currentAction != ActionType.DoNothing;
    }

    public void SetGripAction( ActionType newAction, GameObject controller )
    {
        // do nothing
        if( newAction == ActionType.DoNothing )
        {
            currentAction = newAction;
            currentHand = default( SteamVR_Input_Sources );
            currentLaser = null;
            return;
        }

        // other actions: check for input sources
        SteamVR_Behaviour_Pose controllerBehavior = controller.GetComponent<SteamVR_Behaviour_Pose>();
        LaserPointerDragAndDrop laser = controller.GetComponent<LaserPointerDragAndDrop>();
        // short circuit error
        if( controllerBehavior == null || laser == null )
        {
            Debug.LogError( "randomizer can't parse controller: " + controller.name );
            return;
        }
        currentAction = newAction;
        currentHand = controllerBehavior.inputSource;
        currentLaser = laser;
    }

    void TakeGripAction()
    {
        ConnectedTerrainController maybeTerrain;
        switch( currentAction )
        {
            case ActionType.RandomizeAll:
                // randomize all terrains and musical parameters
                Debug.Log( "about to randomize all" );
                StartCoroutine( ReRandomizeAll() );
                break;
            case ActionType.RandomizeCurrent:
                // find the terrain we're above
                maybeTerrain = FindTerrain();

                // randomize it
                if( maybeTerrain != null && indices.ContainsKey( maybeTerrain ) )
                {
                    int which = indices[ maybeTerrain ];
                    StartCoroutine( ReRandomizeTerrain( which ) );
                }
                break;
            case ActionType.PerturbBig:
                // find the terrain we're above
                maybeTerrain = FindTerrain();

                // perturb it
                if( maybeTerrain != null && indices.ContainsKey( maybeTerrain ) )
                {
                    int which = indices[ maybeTerrain ];
                    StartCoroutine( ReRandomizeTerrain( which, RandomizeAmount.PerturbBig ) );
                }
                // randomize it
                break;
            case ActionType.PerturbSmall:
                // find the terrain we're above
                maybeTerrain = FindTerrain();

                // perturb it
                if( maybeTerrain != null && indices.ContainsKey( maybeTerrain ) )
                {
                    int which = indices[ maybeTerrain ];
                    StartCoroutine( ReRandomizeTerrain( which, RandomizeAmount.PerturbSmall ) );
                }
                // randomize it
                break;
            case ActionType.Copy:
                // don't do this here. copy on grip up, not down                
                break;
            case ActionType.DoNothing:
                break;
        }
    }

    void Copy( LaserPointerDragAndDrop dragDrop )
    {
        // find the terrain to copy from
        ConnectedTerrainController fromTerrain = dragDrop.GetStartObject<ConnectedTerrainController>();

        // find the terrain to copy to
        ConnectedTerrainController toTerrain = dragDrop.GetEndObject<ConnectedTerrainController>();

        // transfer properties
        if( fromTerrain != null && toTerrain != null && fromTerrain != toTerrain )
        {
            int from = indices[fromTerrain];
            int to = indices[toTerrain];
            StartCoroutine( CopyTerrainExamples( from, to ) );
        }
    }

    Vector3 FindLocationToCopyFrom()
    {
        // TODO: how?
        return Vector3.zero;
    }

    IEnumerator RescanTerrain( ConnectedTerrainController t )
    {
        // rescan entire terrain -- it will do the texture at the end
        int computeFrames = 20;
        int gisFrames = 20;
        int textureFrames = 3;
        t.RescanProvidedExamples( false, computeFrames, gisFrames, textureFrames );

        // wait before moving on
        for( int f = 0; f < computeFrames + gisFrames + 1; f++ ) { yield return null; }
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

                terrainHeightControllers[i].ProvideExample( e, false );
            }

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
                terrainHeightControllers[i].ProvideExample( b, false );
            }
            // wait before moving on
            yield return null;
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
            }
            // don't rescan just texture
            // terrainTextureControllers[i].RescanProvidedExamples();
            // wait a frame before doing the next one
            // yield return null;

            // rescan entire terrain -- it will do the texture at the end
            yield return StartCoroutine( RescanTerrain( terrainHeightControllers[i] ) );
        }
    }

    void InitializeMusicalParameters()
    {
        // for each parameter, initialize n points and randomize their values
        // volume:
        for( int i = 0; i < musicalParamExamples; i++ )
        {
            Sound0To1Example v = Instantiate( volumePrefab, GetRandomLocationWithinRadius( musicRadius ), Quaternion.identity );
            v.Initialize( false );
            v.Randomize();
        }
        // rescan to check for new random values
        volumeRegressor.RescanProvidedExamples();

        // density:
        for( int i = 0; i < musicalParamExamples; i++ )
        {
            Sound0To1Example d = Instantiate( densityPrefab, GetRandomLocationWithinRadius( musicRadius ), Quaternion.identity );
            d.Initialize( false );
            d.Randomize();
        }
        // rescan to check for new random values
        densityRegressor.RescanProvidedExamples();

        // timbre:
        for( int i = 0; i < musicalParamExamples; i++ )
        {
            Sound0To1Example t = Instantiate( timbrePrefab, GetRandomLocationWithinRadius( musicRadius ), Quaternion.identity );
            t.Initialize( false );
            t.Randomize();
        }
        // rescan to check for new random values
        timbreRegressor.RescanProvidedExamples();

        // tempo:
        for( int i = 0; i < musicalParamExamples; i++ )
        {
            SoundTempoExample t = Instantiate( tempoPrefab, GetRandomLocationWithinRadius( musicRadius ), Quaternion.identity );
            t.Initialize( false );
            t.Randomize();
        }
        // rescan to check for new random values
        tempoRegressor.RescanProvidedExamples();

        // chord:
        for( int i = 0; i < musicalParamExamples; i++ )
        {
            SoundChordExample c = Instantiate( chordPrefab, GetRandomLocationWithinRadius( musicRadius ), Quaternion.identity );
            c.Initialize( false );
            c.Randomize();
        }
        // rescan to check for new random values
        chordClassifier.RescanProvidedExamples();

        
    }

    IEnumerator ReRandomizeAll()
    {
        for( int i = 0; i < terrainHeightControllers.Length; i++ )
        {
            yield return StartCoroutine( ReRandomizeTerrain( i ) );
        }
        ReRandomizeMusicalParameters();
    }

    IEnumerator ReRandomizeTerrain( int which, RandomizeAmount amount = RandomizeAmount.Full )
    {
        currentlyComputing = true;

        ReRandomizeTerrainHeight( which, amount );
        ReRandomizeTerrainBumpiness( which, amount );
        ReRandomizeTerrainTexture( which, amount );

        // rescan
        yield return StartCoroutine( RescanTerrain( terrainHeightControllers[ which ] ) );

        currentlyComputing = false;
    }

    void ReRandomizeTerrainHeight( int which, RandomizeAmount amount )
    {
        // generate N points in random locations
        List< TerrainHeightExample > examples = terrainHeightControllers[which].myRegressionExamples;
        for( int j = 0; j < examples.Count; j++ )
        {
            switch( amount )
            {
                case RandomizeAmount.Full:
                    examples[j].transform.position = 
                        terrainHeightControllers[which].transform.position + GetRandomLocationWithinRadius( landRadius );
                    // if it had something to randomize
                    // myHeightExamples[which][j].Randomize();
                    break;
                case RandomizeAmount.PerturbBig:
                    examples[j].transform.position += GetRandomLocationWithinTallRadius( perturbBigRadius );
                    break;
                case RandomizeAmount.PerturbSmall:
                    examples[j].transform.position += GetRandomLocationWithinTallRadius( perturbSmallRadius );
                    break;
            }
        }
        // We don't rescan the terrain because we will do it after the bumps are randomized too
    }

    void ReRandomizeTerrainBumpiness( int which, RandomizeAmount amount )
    {
        List< TerrainGISExample > examples = terrainHeightControllers[which].myGISRegressionExamples;
        // generate N points in random locations
        for( int j = 0; j < examples.Count; j++ )
        {
            switch( amount )
            {
                case RandomizeAmount.Full:
                    // position
                    examples[j].transform.position = 
                        terrainHeightControllers[which].transform.position + GetRandomLocationWithinRadius( landRadius );
                    // stats
                    examples[j].Randomize();
                    break;
                case RandomizeAmount.PerturbBig:
                    // position
                    examples[j].transform.position += GetRandomLocationWithinTallRadius( perturbBigRadius );
                    // stats
                    examples[j].Perturb( perturbBigBumpRange );
                    break;
                case RandomizeAmount.PerturbSmall:
                    // position
                    examples[j].transform.position += GetRandomLocationWithinTallRadius( perturbSmallRadius );
                    // stats
                    examples[j].Perturb( perturbSmallBumpRange );
                    break;
            }
        }
        // We don't rescan the terrain because we will do it in the above function
    }

    void ReRandomizeTerrainTexture( int which, RandomizeAmount amount )
    {
        List< TerrainTextureExample > examples = terrainTextureControllers[which].myRegressionExamples;
        // generate N points in random locations
        for( int j = 0; j < examples.Count; j++ )
        {
            switch( amount )
            {
                case RandomizeAmount.Full:
                    // position
                    examples[j].transform.position = 
                        terrainHeightControllers[which].transform.position + GetRandomLocationWithinRadius( landRadius );
                    // color
                    examples[j].Randomize();
                    break;
                case RandomizeAmount.PerturbBig:
                    // position
                    examples[j].transform.position += GetRandomLocationWithinTallRadius( perturbBigRadius );
                    // DON'T perturb the color -- it's too drastic of a change for a "perturbation"
                    break;
                case RandomizeAmount.PerturbSmall:
                    // position
                    examples[j].transform.position += GetRandomLocationWithinTallRadius( perturbSmallRadius );
                    // DON'T perturb the color -- it's too drastic of a change for a "perturbation"
                    break;
            }
        }
        // We don't rescan the terrain because we will do it in the above function
    }

    // TODO be able to perturb this?
    void ReRandomizeMusicalParameters()
    {
        // volume:
        for( int i = 0; i < volumeRegressor.myRegressionExamples.Count; i++ )
        {
            volumeRegressor.myRegressionExamples[i].Randomize();
            volumeRegressor.myRegressionExamples[i].transform.position = GetRandomLocationWithinRadius( musicRadius );
        }
        // rescan to check for new random values
        volumeRegressor.RescanProvidedExamples();

        // density:
        for( int i = 0; i < densityRegressor.myRegressionExamples.Count; i++ )
        {
            densityRegressor.myRegressionExamples[i].Randomize();
            densityRegressor.myRegressionExamples[i].transform.position = GetRandomLocationWithinRadius( musicRadius );
        }
        // rescan to check for new random values
        densityRegressor.RescanProvidedExamples();

        // timbre:
        for( int i = 0; i < timbreRegressor.myRegressionExamples.Count; i++ )
        {
            timbreRegressor.myRegressionExamples[i].Randomize();
            timbreRegressor.myRegressionExamples[i].transform.position = GetRandomLocationWithinRadius( musicRadius );
        }
        // rescan to check for new random values
        timbreRegressor.RescanProvidedExamples();

        // tempo:
        for( int i = 0; i < tempoRegressor.myRegressionExamples.Count; i++ )
        {
            tempoRegressor.myRegressionExamples[i].Randomize();
            tempoRegressor.myRegressionExamples[i].transform.position = GetRandomLocationWithinRadius( musicRadius );
        }
        // rescan to check for new random values
        tempoRegressor.RescanProvidedExamples();

        // chord:
        for( int i = 0; i < chordClassifier.myClassifierExamples.Count; i++ )
        {
            chordClassifier.myClassifierExamples[i].Randomize();
            chordClassifier.myClassifierExamples[i].transform.position = GetRandomLocationWithinRadius( musicRadius );
        }
        // rescan to check for new random values
        chordClassifier.RescanProvidedExamples();
    }

    IEnumerator CopyTerrainExamples( int from, int to )
    {
        currentlyComputing = true;

        CopyExampleLocations<TerrainHeightExample>( 
            terrainHeightControllers[from], 
            terrainHeightControllers[to], 
            terrainHeightControllers[from].myRegressionExamples,
            terrainHeightControllers[to].myRegressionExamples
        );
        CopyExampleLocations<TerrainGISExample>( 
            terrainHeightControllers[from], 
            terrainHeightControllers[to],
            terrainHeightControllers[from].myGISRegressionExamples, 
            terrainHeightControllers[to].myGISRegressionExamples
        );
        CopyBumpParameters( terrainHeightControllers[from].myGISRegressionExamples, 
            terrainHeightControllers[to].myGISRegressionExamples );
        CopyExampleLocations<TerrainTextureExample>( 
            terrainHeightControllers[from], 
            terrainHeightControllers[to], 
            terrainTextureControllers[from].myRegressionExamples, 
            terrainTextureControllers[to].myRegressionExamples
        );
        CopyTextureParameters( terrainTextureControllers[from].myRegressionExamples, 
            terrainTextureControllers[to].myRegressionExamples );

        // rescan terrain
        yield return StartCoroutine( RescanTerrain( terrainHeightControllers[ to ] ) );

        currentlyComputing = false;
    }


    void CopyExampleLocations<T>( 
        ConnectedTerrainController from, 
        ConnectedTerrainController to, 
        List<T> sourceExamples, 
        List<T> toOverWrite 
    ) where T : MonoBehaviour 
    {
        if( sourceExamples.Count != toOverWrite.Count )
        {
            Debug.LogError( "different numbers of examples!" );
            return;
        }
        for( int i = 0; i < sourceExamples.Count; i++ )
        {
            toOverWrite[i].transform.position = sourceExamples[i].transform.position
                - from.transform.position + to.transform.position;
        }
    }

    void CopyBumpParameters( List<TerrainGISExample> from, List<TerrainGISExample> to )
    {
        for( int i = 0; i < from.Count; i++ )
        {
            to[i].CopyFrom( from[i] );
        }
    }

    void CopyTextureParameters( List<TerrainTextureExample> from, List<TerrainTextureExample> to )
    {
        for( int i = 0; i < from.Count; i++ )
        {
            to[i].CopyFrom( from[i] );
        }
    }

    ConnectedTerrainController FindTerrain()
    {
        return TerrainUtility.FindTerrain<ConnectedTerrainController>( transform.position );
    }


    Vector3 GetRandomLocationWithinRadius( Vector3 radius )
    {
        return new Vector3(
            Random.Range( -radius.x, radius.x ),
            Random.Range( 0, radius.y ),
            Random.Range( -radius.z, radius.z )
        );
    }

    Vector3 GetRandomLocationWithinTallRadius( Vector3 radius )
    {
        return new Vector3(
            Random.Range( -radius.x, radius.x ),
            Random.Range( -radius.y, radius.y ),
            Random.Range( -radius.z, radius.z )
        );
    }


}
