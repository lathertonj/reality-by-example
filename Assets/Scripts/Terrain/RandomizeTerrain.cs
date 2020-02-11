﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class RandomizeTerrain : MonoBehaviour 
{

    public enum ActionType { RandomizeAll, RandomizeCurrent, PerturbBig, PerturbSmall, Copy };
    public ActionType currentAction = ActionType.RandomizeAll;
    private enum RandomizeAmount { Full, PerturbBig, PerturbSmall };


    public SteamVR_Input_Sources leftHand, rightHand;
    public SteamVR_Action_Boolean gripPress;

    public LaserPointerDragAndDrop leftHandLaser, rightHandLaser;

    ConnectedTerrainController[] terrainHeightControllers;
    ConnectedTerrainTextureController[] terrainTextureControllers;

    public Vector3 landRadius = new Vector3( 50, 100, 50 );
    public Vector3 musicRadius = new Vector3( 150, 100, 150 );
    public Vector3 perturbBigRadius = new Vector3( 5, 5, 5 );
    public Vector3 perturbSmallRadius = new Vector3( 0.2f, 0.2f, 0.2f );
    public float perturbBigBumpRange = 0.2f;
    public float perturbSmallBumpRange = 0.02f;
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

    private Dictionary< ConnectedTerrainController, int > indices;


    // Start is called before the first frame update
    void Start()
    {
        terrainHeightControllers = FindObjectsOfType<ConnectedTerrainController>();
        terrainTextureControllers = new ConnectedTerrainTextureController[ terrainHeightControllers.Length ];
        myHeightExamples = new List<TerrainHeightExample>[ terrainHeightControllers.Length ];
        myBumpExamples = new List<TerrainGISExample>[ terrainHeightControllers.Length ];
        myTextureExamples = new List<TerrainTextureExample>[ terrainHeightControllers.Length ];
        indices = new Dictionary<ConnectedTerrainController, int>();
        for( int i = 0; i < terrainHeightControllers.Length; i++ )
        {
            terrainTextureControllers[i] = terrainHeightControllers[i].GetComponent< ConnectedTerrainTextureController >();
            myHeightExamples[i] = new List<TerrainHeightExample>();
            myBumpExamples[i] = new List<TerrainGISExample>();
            myTextureExamples[i] = new List<TerrainTextureExample>();
            indices[terrainHeightControllers[i]] = i;
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
        if( ShouldRandomize() )
        {
            if( gripPress.GetStateDown( leftHand ) || gripPress.GetStateDown( rightHand ) )
            {
                TakeGripAction();
            }
        }

        if( currentAction == ActionType.Copy )
        {
            if( gripPress.GetStateUp( leftHand ) )
            {
                Copy( leftHandLaser );
            }
            else if( gripPress.GetStateUp( rightHand ) )
            {
                Copy( rightHandLaser );
            }
        }
    }

    bool ShouldRandomize()
    {
        // disallow randomization during initialization
        return !currentlyComputing;
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

                terrainHeightControllers[i].ProvideExampleEfficient( e );

                // remember
                myHeightExamples[i].Add( e );
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
                terrainHeightControllers[i].ProvideExampleEfficient( b );

                // remember
                myBumpExamples[i].Add( b );
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

                // remember
                myTextureExamples[i].Add( t );
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
            myVolumeExamples.Add( v );
        }
        // rescan to check for new random values
        SoundEngine0To1Regressor.volumeRegressor.RescanProvidedExamples();

        // density:
        for( int i = 0; i < musicalParamExamples; i++ )
        {
            Sound0To1Example d = Instantiate( densityPrefab, GetRandomLocationWithinRadius( musicRadius ), Quaternion.identity );
            d.Initialize( false );
            d.Randomize();
            myDensityExamples.Add( d );
        }
        // rescan to check for new random values
        SoundEngine0To1Regressor.densityRegressor.RescanProvidedExamples();

        // timbre:
        for( int i = 0; i < musicalParamExamples; i++ )
        {
            Sound0To1Example t = Instantiate( timbrePrefab, GetRandomLocationWithinRadius( musicRadius ), Quaternion.identity );
            t.Initialize( false );
            t.Randomize();
            myTimbreExamples.Add( t );
        }
        // rescan to check for new random values
        SoundEngine0To1Regressor.timbreRegressor.RescanProvidedExamples();

        // tempo:
        for( int i = 0; i < musicalParamExamples; i++ )
        {
            SoundTempoExample t = Instantiate( tempoPrefab, GetRandomLocationWithinRadius( musicRadius ), Quaternion.identity );
            t.Initialize( false );
            t.Randomize();
            myTempoExamples.Add( t );
        }
        // rescan to check for new random values
        myTempoExamples[0].Rescan();

        // chord:
        for( int i = 0; i < musicalParamExamples; i++ )
        {
            SoundChordExample c = Instantiate( chordPrefab, GetRandomLocationWithinRadius( musicRadius ), Quaternion.identity );
            c.Initialize( false );
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
        for( int j = 0; j < myHeightExamples[which].Count; j++ )
        {
            switch( amount )
            {
                case RandomizeAmount.Full:
                    myHeightExamples[which][j].transform.position = 
                        terrainHeightControllers[which].transform.position + GetRandomLocationWithinRadius( landRadius );
                    // if it had something to randomize
                    // myHeightExamples[which][j].Randomize();
                    break;
                case RandomizeAmount.PerturbBig:
                    myHeightExamples[which][j].transform.position += GetRandomLocationWithinTallRadius( perturbBigRadius );
                    break;
                case RandomizeAmount.PerturbSmall:
                    myHeightExamples[which][j].transform.position += GetRandomLocationWithinTallRadius( perturbSmallRadius );
                    break;
            }
        }
        // We don't rescan the terrain because we will do it after the bumps are randomized too
    }

    void ReRandomizeTerrainBumpiness( int which, RandomizeAmount amount )
    {
        // generate N points in random locations
        for( int j = 0; j < myBumpExamples[which].Count; j++ )
        {
            switch( amount )
            {
                case RandomizeAmount.Full:
                    // position
                    myBumpExamples[which][j].transform.position = 
                        terrainHeightControllers[which].transform.position + GetRandomLocationWithinRadius( landRadius );
                    // stats
                    myBumpExamples[which][j].Randomize();
                    break;
                case RandomizeAmount.PerturbBig:
                    // position
                    myBumpExamples[which][j].transform.position += GetRandomLocationWithinTallRadius( perturbBigRadius );
                    // stats
                    myBumpExamples[which][j].Perturb( perturbBigBumpRange );
                    break;
                case RandomizeAmount.PerturbSmall:
                    // position
                    myBumpExamples[which][j].transform.position += GetRandomLocationWithinTallRadius( perturbSmallRadius );
                    // stats
                    myBumpExamples[which][j].Perturb( perturbSmallBumpRange );
                    break;
            }
        }
        // We don't rescan the terrain because we will do it in the above function
    }

    void ReRandomizeTerrainTexture( int which, RandomizeAmount amount )
    {
        // generate N points in random locations
        for( int j = 0; j < myTextureExamples[which].Count; j++ )
        {
            switch( amount )
            {
                case RandomizeAmount.Full:
                    // position
                    myTextureExamples[which][j].transform.position = 
                        terrainHeightControllers[which].transform.position + GetRandomLocationWithinRadius( landRadius );
                    // color
                    myTextureExamples[which][j].Randomize();
                    break;
                case RandomizeAmount.PerturbBig:
                    // position
                    myTextureExamples[which][j].transform.position += GetRandomLocationWithinTallRadius( perturbBigRadius );
                    // DON'T perturb the color -- it's too drastic of a change for a "perturbation"
                    break;
                case RandomizeAmount.PerturbSmall:
                    // position
                    myTextureExamples[which][j].transform.position += GetRandomLocationWithinTallRadius( perturbSmallRadius );
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

    IEnumerator CopyTerrainExamples( int from, int to )
    {
        currentlyComputing = true;

        CopyExampleLocations<TerrainHeightExample>( terrainHeightControllers[from], terrainHeightControllers[to], myHeightExamples[from], myHeightExamples[to] );
        CopyExampleLocations<TerrainGISExample>( terrainHeightControllers[from], terrainHeightControllers[to], myBumpExamples[from], myBumpExamples[to] );
        CopyBumpParameters( myBumpExamples[from], myBumpExamples[to] );
        CopyExampleLocations<TerrainTextureExample>( terrainHeightControllers[from], terrainHeightControllers[to], myTextureExamples[from], myTextureExamples[to] );
        CopyTextureParameters( myTextureExamples[from], myTextureExamples[to] );

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
        return FindTerrain( transform.position );
    }

    ConnectedTerrainController FindTerrain( Vector3 nearLocation )
    {
        // Bit shift the index of the layer (8: Connected terrains) to get a bit mask
        int layerMask = 1 << 8;

        RaycastHit hit;
        // Check from a point really high above us, in the downward direction (in case we are below terrain)
        if( Physics.Raycast( nearLocation + 400 * Vector3.up, Vector3.down, out hit, Mathf.Infinity, layerMask ) )
        {
            ConnectedTerrainController foundTerrain = hit.transform.GetComponentInParent<ConnectedTerrainController>();
            if( foundTerrain != null )
            {
                return foundTerrain;
            }
        }
        return null;
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
