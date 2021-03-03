﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Photon.Pun;

public class RandomizeTerrain : MonoBehaviourPunCallbacks 
{

    public enum ActionType { RandomizeAll, RandomizeCurrent, PerturbBig, PerturbSmall, Copy, DoNothing };
    public ActionType currentAction = ActionType.RandomizeAll;
    private enum RandomizeAmount { Full, PerturbBig, PerturbSmall };


    public SteamVR_Input_Sources currentHand;
    public SteamVR_Action_Boolean gripPress, copyAction;

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
    public bool isHeightPrefabNetworked;
    public bool isBumpPrefabNetworked;
    public bool isTexturePrefabNetworked;
    public SoundChordExample chordPrefab;
    public SoundTempoExample tempoPrefab;
    public Sound0To1Example densityPrefab, timbrePrefab, volumePrefab;
    public bool isChordPrefabNetworked, isTempoPrefabNetworked, 
        isDensityPrefabNetworked, isTimbrePrefabNetworked, isVolumePrefabNetworked;
    
    private bool currentlyComputing = false;

    private Dictionary< ConnectedTerrainController, int > indices;

    public bool randomizeOnStart = true;

    public bool waitForPhoton = false;

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

        if( !waitForPhoton )
        {
            this.OnJoinedRoom();
        }
    }

    public override void OnJoinedRoom()
    {
        // don't do anything if this isn't our scene
        PhotonView maybeView = GetComponent<PhotonView>();
        if( maybeView != null && !maybeView.IsMine ) { return; }

        // otherwise, randomize the world
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
            if( copyAction.GetStateUp( currentHand ) )
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

    IEnumerator RescanTerrain( ConnectedTerrainController t )
    {
        // rescan entire terrain -- it will do the texture at the end
        int computeFrames = 20;
        int gisFrames = 20;
        int textureFrames = 3;
        yield return StartCoroutine( t.RescanProvidedExamplesCoroutine( false, computeFrames, gisFrames, textureFrames ) );
    }

    IEnumerator InitializeAll()
    {
        currentlyComputing = true;
        InitializeTerrainHeights();
        InitializeTerrainBumps();
        yield return StartCoroutine( InitializeTerrainTextures() );
        InitializeMusicalParameters();
        currentlyComputing = false;
    }

    void InitializeTerrainHeights( ConnectedTerrainController terrainController )
    {
        // generate N points in random locations, without scanning the terrain
        for( int j = 0; j < heightExamples; j++ )
        {
            TerrainHeightExample e;
            Vector3 newPosition = terrainController.transform.position + GetRandomLocationWithinRadius( landRadius );
            if( isHeightPrefabNetworked )
            {
                e = PhotonNetwork.Instantiate( heightPrefab.name, newPosition, Quaternion.identity ).GetComponent<TerrainHeightExample>();
            }
            else
            {
                e = Instantiate( heightPrefab, newPosition, Quaternion.identity );
            }

            // ~ JustPlaced
            e.ManuallySpecifyTerrain( terrainController );

            terrainController.ProvideExample( e, false );
        }
    }

    void InitializeTerrainHeights()
    {
        // for each terrain
        for( int i = 0; i < terrainHeightControllers.Length; i++ )
        {
            InitializeTerrainHeights( terrainHeightControllers[i] );
        }
    }

    void InitializeTerrainBumps( ConnectedTerrainController terrainController )
    {
        // generate N points in random locations
        for( int j = 0; j < bumpExamples; j++ )
        {
            Vector3 newPosition = terrainController.transform.position + GetRandomLocationWithinRadius( landRadius );
            TerrainGISExample b;
            if( isBumpPrefabNetworked )
            {
                b = PhotonNetwork.Instantiate( bumpPrefab.name, newPosition, Quaternion.identity )
                    .GetComponent<TerrainGISExample>();
            }
            else
            {
                b = Instantiate( bumpPrefab, newPosition, Quaternion.identity );
            }

            // ~ JustPlaced
            b.ManuallySpecifyTerrain( terrainController );

            // for each point, randomize as well its type and intensity
            b.Randomize();

            // inform terrain of example
            terrainController.ProvideExample( b, false );
        }
    }

    void InitializeTerrainBumps()
    {
        // for each terrain
        for( int i = 0; i < terrainHeightControllers.Length; i++ )
        {
            InitializeTerrainBumps( terrainHeightControllers[i] );
        }
    }

    void InitializeTerrainTextures( ConnectedTerrainController terrainHeight, ConnectedTerrainTextureController terrainTexture )
    {
        // generate N points in random locations
        for( int j = 0; j < bumpExamples; j++ )
        {
            Vector3 newPosition = terrainHeight.transform.position + GetRandomLocationWithinRadius( landRadius );
            TerrainTextureExample t; 
            if( isTexturePrefabNetworked )
            {
                t = PhotonNetwork.Instantiate( texturePrefab.name, newPosition, Quaternion.identity )
                    .GetComponent<TerrainTextureExample>();
            }
            else
            {
                t = Instantiate( texturePrefab, newPosition, Quaternion.identity );
            }
            
            // ~ JustPlaced()
            t.ManuallySpecifyTerrain( terrainTexture );
            
            // for each point, randomize as well its type and intensity
            t.Randomize();

            // inform terrain of example (shouldRetrain = false)
            terrainTexture.ProvideExample( t, false );
        }
    }

    IEnumerator InitializeTerrainTextures()
    {
        // for each terrain
        for( int i = 0; i < terrainTextureControllers.Length; i++ )
        {
            InitializeTerrainTextures( terrainHeightControllers[i], terrainTextureControllers[i] );
            
            // don't rescan just texture
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
            Sound0To1Example v; 
            if( isVolumePrefabNetworked )
            {
                v = PhotonNetwork.Instantiate( volumePrefab.name, GetRandomLocationWithinRadius( musicRadius ), Quaternion.identity )
                    .GetComponent<Sound0To1Example>();
            }
            else
            {
                v = Instantiate( volumePrefab, GetRandomLocationWithinRadius( musicRadius ), Quaternion.identity );
            }
            v.Initialize( false );
            // randomize (also alerts network to new values)
            v.Randomize();
        }
        // rescan to check for new random values
        volumeRegressor.RescanProvidedExamples();

        // density:
        for( int i = 0; i < musicalParamExamples; i++ )
        {
            Sound0To1Example d;
            if( isDensityPrefabNetworked )
            {
                d = PhotonNetwork.Instantiate( densityPrefab.name, GetRandomLocationWithinRadius( musicRadius ), Quaternion.identity )
                    .GetComponent<Sound0To1Example>();
            }
            else
            {
                d = Instantiate( densityPrefab, GetRandomLocationWithinRadius( musicRadius ), Quaternion.identity );
            }
                
            d.Initialize( false );
            // randomize (also alerts network to new values)
            d.Randomize();
        }
        // rescan to check for new random values
        densityRegressor.RescanProvidedExamples();

        // timbre:
        for( int i = 0; i < musicalParamExamples; i++ )
        {
            Sound0To1Example t;
            if( isTimbrePrefabNetworked )
            {
                t = PhotonNetwork.Instantiate( timbrePrefab.name, GetRandomLocationWithinRadius( musicRadius ), Quaternion.identity )
                    .GetComponent<Sound0To1Example>();
            }
            else
            {
                t = Instantiate( timbrePrefab, GetRandomLocationWithinRadius( musicRadius ), Quaternion.identity );
            }
            t.Initialize( false );
            // randomize (also alerts network to new values)
            t.Randomize();
        }
        // rescan to check for new random values
        timbreRegressor.RescanProvidedExamples();

        // tempo:
        for( int i = 0; i < musicalParamExamples; i++ )
        {
            SoundTempoExample t;
            if( isTempoPrefabNetworked )
            {
                t = PhotonNetwork.Instantiate( tempoPrefab.name, GetRandomLocationWithinRadius( musicRadius ), Quaternion.identity )
                    .GetComponent<SoundTempoExample>();
            }
            else
            {
                t = Instantiate( tempoPrefab, GetRandomLocationWithinRadius( musicRadius ), Quaternion.identity );
            }
            t.Initialize( false );
            // randomize (also alerts network to new values)
            t.Randomize();
        }
        // rescan to check for new random values
        tempoRegressor.RescanProvidedExamples();

        // chord:
        for( int i = 0; i < musicalParamExamples; i++ )
        {
            SoundChordExample c; 
            if( isChordPrefabNetworked )
            {
                c = PhotonNetwork.Instantiate( chordPrefab.name, GetRandomLocationWithinRadius( musicRadius ), Quaternion.identity )
                    .GetComponent<SoundChordExample>();
            }
            else
            {
                c = Instantiate( chordPrefab, GetRandomLocationWithinRadius( musicRadius ), Quaternion.identity );
            }
            c.Initialize( false );
            // randomize (also alerts network to new values)
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

        ConnectedTerrainController terrain = terrainHeightControllers[which];
        ConnectedTerrainTextureController texture = terrainTextureControllers[which];

        // height. have we done any work yet?
        if( terrain.myRegressionExamples.Count == 0 )
        {
            // initialize
            InitializeTerrainHeights( terrain );
        }
        else
        {
            // randomize only the points we already have
            ReRandomizeTerrainHeight( which, amount );
        }

        // bump. have we done any work yet?
        if( terrain.myGISRegressionExamples.Count == 0 )
        {
            // initialize
            InitializeTerrainBumps( terrain );
        }
        else
        {
            // randomize only the points we already have
            ReRandomizeTerrainBumpiness( which, amount );
        }

        // texture. have we done any work yet?
        if( texture.myRegressionExamples.Count == 0 )
        {
            // initialize
            InitializeTerrainTextures( terrain, texture );
        }
        else
        {
            // randomize only the points we already have
            ReRandomizeTerrainTexture( which, amount );
        }

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
            // alert examples on the network to changes
            examples[j].AlertNetworkToChanges();
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
            // Randomize() and Perturb() alert the network to changes, so no need to do it here
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
            // alert examples on network to changes
            examples[j].AlertNetworkToChanges();
        }
        // We don't rescan the terrain because we will do it in the above function
    }

    // TODO be able to perturb this?
    void ReRandomizeMusicalParameters()
    {
        // volume:
        for( int i = 0; i < volumeRegressor.myRegressionExamples.Count; i++ )
        {
            volumeRegressor.myRegressionExamples[i].transform.position = GetRandomLocationWithinRadius( musicRadius );
            // randomize (also alerts network to new values)
            volumeRegressor.myRegressionExamples[i].Randomize();
        }
        // rescan to check for new random values
        volumeRegressor.RescanProvidedExamples();

        // density:
        for( int i = 0; i < densityRegressor.myRegressionExamples.Count; i++ )
        {
            densityRegressor.myRegressionExamples[i].transform.position = GetRandomLocationWithinRadius( musicRadius );
            // randomize (also alerts network to new values)
            densityRegressor.myRegressionExamples[i].Randomize();
        }
        // rescan to check for new random values
        densityRegressor.RescanProvidedExamples();

        // timbre:
        for( int i = 0; i < timbreRegressor.myRegressionExamples.Count; i++ )
        {
            timbreRegressor.myRegressionExamples[i].transform.position = GetRandomLocationWithinRadius( musicRadius );
            // randomize (also alerts network to new values)
            timbreRegressor.myRegressionExamples[i].Randomize();
        }
        // rescan to check for new random values
        timbreRegressor.RescanProvidedExamples();

        // tempo:
        for( int i = 0; i < tempoRegressor.myRegressionExamples.Count; i++ )
        {
            tempoRegressor.myRegressionExamples[i].transform.position = GetRandomLocationWithinRadius( musicRadius );
            // randomize (also alerts network to new values)
            tempoRegressor.myRegressionExamples[i].Randomize();
        }
        // rescan to check for new random values
        tempoRegressor.RescanProvidedExamples();

        // chord:
        for( int i = 0; i < chordClassifier.myClassifierExamples.Count; i++ )
        {
            chordClassifier.myClassifierExamples[i].transform.position = GetRandomLocationWithinRadius( musicRadius );
            // randomize (also alerts network to new values)
            chordClassifier.myClassifierExamples[i].Randomize();
        }
        // rescan to check for new random values
        chordClassifier.RescanProvidedExamples();
    }

    IEnumerator CopyTerrainExamples( int from, int to )
    {
        currentlyComputing = true;

        // make sure the destination has same number of examples as source
        terrainHeightControllers[to].MatchNumberOfExamples( terrainHeightControllers[from] );
        
        // copy height
        CopyExampleLocations<TerrainHeightExample>( 
            terrainHeightControllers[from], 
            terrainHeightControllers[to], 
            terrainHeightControllers[from].myRegressionExamples,
            terrainHeightControllers[to].myRegressionExamples
        );
        
        // copy bump
        CopyBumpParameters( terrainHeightControllers[from].myGISRegressionExamples, 
            terrainHeightControllers[to].myGISRegressionExamples );
        CopyExampleLocations<TerrainGISExample>( 
            terrainHeightControllers[from], 
            terrainHeightControllers[to],
            terrainHeightControllers[from].myGISRegressionExamples, 
            terrainHeightControllers[to].myGISRegressionExamples
        );
        
        // copy texture
        CopyTextureParameters( terrainTextureControllers[from].myRegressionExamples, 
            terrainTextureControllers[to].myRegressionExamples );
        CopyExampleLocations<TerrainTextureExample>( 
            terrainHeightControllers[from], 
            terrainHeightControllers[to], 
            terrainTextureControllers[from].myRegressionExamples, 
            terrainTextureControllers[to].myRegressionExamples
        );

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
            // after changing position, alert the network that it has changed
            toOverWrite[i].GetComponent<IPhotonExample>().AlertNetworkToChanges();
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
