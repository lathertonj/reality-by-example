using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Stitchscape;

public class ConnectedTerrainController : MonoBehaviour , SerializableByExample
{
    // CRIT NOTES
    // - could do a comparison between this and linear regression and to non-ML interpolation
    // - in zoo tycoon, some animals need more mountainous and some need more flat --> different places could have different animals
    // --- wheeboxes could jump out of the boxes and roam the terrain!
    // --- the little thingies could run out to the vertices and roam the terrain and pull it up and down
    // - groups of boxes -- be able to move them together after you place them
    // - versioning?

    // designers better at curating than creating; better to have a computer to generate 100 and have the designer select between them

    // texture synthesis by example? paint on the landscape then map it onto new object?


    private Terrain myTerrain;
    private TerrainData myTerrainData;
    private ConnectedTerrainTextureController myTextureController;

    public ConnectedTerrainController leftNeighbor, rightNeighbor, upperNeighbor, lowerNeighbor;

    static int extraBorderPixels = 20;

    private int verticesPerSide;
    private float terrainSize;
    private float spaceBetweenVertices;
    private float terrainHeight;
    private float[,] myPureRegressionHeights, myModifiedRegressionHeights;
    private float stitchWidth;
    private float stitchStrength;

    // regression
    private RapidMixRegression myRegression;
    [HideInInspector] public List<TerrainHeightExample> myRegressionExamples;
    private bool haveTrained = false;

    private UnderTerrainController myBottom;

    public bool addGISTexture = true;
    public bool debugGISFeatures = false;
    public TextMesh debugUI;


    public TerrainHeightExample heightPrefab;
    public TerrainGISExample gisPrefab;

    public string serializationIdentifier;


    private RapidMixRegression myGISRegression;
    [HideInInspector] public List<TerrainGISExample> myGISRegressionExamples;
    private bool haveTrainedGIS = false;
    private float[,,] loadedGISData;
    private int gisDataFileSideLength = 513;
    private string[] gisDataFiles = { "hill_h50", "mountain2_h150", "mountain3_h125", "mountain1_h100", "diagonal_100" };
    // toning these down by multiplying by 0.X just sort of mellows it out
    // TODO: just need to go back to the original .raw files and lower them so they don't add so much height in the first place.
    private float[] gisDataHeights = { 50, 150, 125, 100, 100 };
    // choices for hilly: 1 = 80,235; 2 = 50,60
    private Vector2Int[] gisDataOffsets = { new Vector2Int( 50, 60 ), new Vector2Int( 40, 115 ), new Vector2Int( 160, 225 ), new Vector2Int( 50, 0 ), new Vector2Int( 340, 192 ) }; 

    // lengthOfSide should be size of the "pureData" (i.e. including the extra non shown stuff)
    private void LoadGISData( int lengthOfSide, float scaleDownFactor )
    {
        // i, y, x
        loadedGISData = new float[ gisDataFiles.Length, lengthOfSide, lengthOfSide ];

        for( int i = 0; i < gisDataFiles.Length; i++ )
        {
            // load into loadedGISData[i,y,x]
            float[,] fileData = new float[ gisDataFileSideLength, gisDataFileSideLength ];

            // read from Resources
            TextAsset textAsset = Resources.Load<TextAsset>( gisDataFiles[i] );
            using( System.IO.Stream stream = new System.IO.MemoryStream( textAsset.bytes ) )
            using( System.IO.BinaryReader reader = new System.IO.BinaryReader( stream ) )
            {
                for( int y = 0; y < gisDataFileSideLength; y++ )
                {
                    for( int x = 0; x < gisDataFileSideLength; x++ )
                    {
                        float v = (float)reader.ReadUInt16() / 0xFFFF;
                        fileData[y, x] = v;
                    }
                }
            }

            // crop and re-height
            Vector2Int offsetWithinFile = gisDataOffsets[i];
            for( int y = 0; y < lengthOfSide; y++ )
            {
                for( int x = 0; x < lengthOfSide; x++ )
                {
                    loadedGISData[i, y, x] = fileData[y + offsetWithinFile.y, x + offsetWithinFile.x]
                        * gisDataHeights[i] / scaleDownFactor;
                }
            }
        }
    }


    public void ProvideExample( TerrainHeightExample example, bool shouldRescan = true )
    {
        // remember
        myRegressionExamples.Add( example );

        // recompute
        if( shouldRescan )
        {
            RescanProvidedExamples();
        }
    }

    public void ForgetExample( TerrainHeightExample example, bool shouldRescan = true )
    {
        // forget
        if( myRegressionExamples.Remove( example ) && shouldRescan )
        {
            // recompute
            RescanProvidedExamples();
        }
    }

    public void ProvideExample( TerrainGISExample example, bool shouldRescan = true )
    {
        if( !addGISTexture ) return;
        // remember
        myGISRegressionExamples.Add( example );

        // recompute
        if( shouldRescan )
        {
            RescanProvidedExamples();
        }
    }


    public void ForgetExample( TerrainGISExample example, bool shouldRescan = true )
    {
        if( !addGISTexture ) return;
        // forget
        if( myGISRegressionExamples.Remove( example ) && shouldRescan )
        {
            // recompute
            RescanProvidedExamples();
        }
    }

    // this will only be called before my examples are overwritten
    public void MatchNumberOfExamples( ConnectedTerrainController other )
    {
        // since examples will be overwritten soon, doesn't matter if
        // we delete or add examples / which ones

        // first, perhaps delete extra examples
        while( myRegressionExamples.Count > other.myRegressionExamples.Count )
        {
            TerrainHeightExample exampleToRemove = myRegressionExamples[0];
            ForgetExample( exampleToRemove, false );
            Destroy( exampleToRemove.gameObject );
        }

        while( myGISRegressionExamples.Count > other.myGISRegressionExamples.Count )
        {
            TerrainGISExample exampleToRemove = myGISRegressionExamples[0];
            ForgetExample( exampleToRemove, false );
            Destroy( exampleToRemove.gameObject );
        }

        // next, perhaps add blank examples
        while( myRegressionExamples.Count < other.myRegressionExamples.Count )
        {
            TerrainHeightExample newExample = Instantiate( heightPrefab, transform.position, Quaternion.identity );
            ProvideExample( newExample, false );
        }

        while( myGISRegressionExamples.Count < other.myGISRegressionExamples.Count )
        {
            TerrainGISExample newExample = Instantiate( gisPrefab, transform.position, Quaternion.identity );
            ProvideExample( newExample, false );
        }

        // finally, check texture
        myTextureController.MatchNumberOfExamples( other.myTextureController );
    }

    // TODO: can this be split into two phases: the base data and the GIS data,
    // so that we can only recompute one when it changes? :|
    public void RescanProvidedExamples( bool lazy = false, int framesToSpreadOver = 15, int framesToSpreadGISOver = 15, int framesToSpreadTextureOver = 3 )
    {
        StartCoroutine( RescanProvidedExamplesCoroutine( lazy, framesToSpreadOver, framesToSpreadGISOver, framesToSpreadTextureOver ) );
    }

    private IEnumerator RescanProvidedExamplesCoroutine( bool lazy, int framesToSpreadOver, int framesToSpreadGISOver, int framesToSpreadTextureOver )
    {
        // train and recompute
        TrainRegression();
        yield return StartCoroutine( ComputeLandHeight( lazy, framesToSpreadOver, framesToSpreadGISOver, framesToSpreadTextureOver ) );
    }


    // Use this for initialization
    void Awake()
    {
        // grab component reference
        myRegression = gameObject.AddComponent<RapidMixRegression>();
        myGISRegression = gameObject.AddComponent<RapidMixRegression>();
        myTerrain = GetComponentInChildren<Terrain>();
        myTerrainData = myTerrain.terrainData;
        myTextureController = GetComponent<ConnectedTerrainTextureController>();

        // initialize list
        myRegressionExamples = new List<TerrainHeightExample>();
        myGISRegressionExamples = new List<TerrainGISExample>();

        // compute sizes
        verticesPerSide = myTerrainData.heightmapResolution;
        terrainSize = myTerrainData.size.x; // it is invariant to scale. scaling up doesn't affect the computations here.
        terrainHeight = myTerrainData.size.y;
        spaceBetweenVertices = terrainSize / ( verticesPerSide - 1 );
        myPureRegressionHeights = new float[verticesPerSide + 2 * extraBorderPixels, verticesPerSide + 2 * extraBorderPixels];
        myModifiedRegressionHeights = new float[verticesPerSide, verticesPerSide];
        // stitch width is 15 pixels' worth
        stitchWidth = 0.1f; //15.0f / verticesPerSide;
        stitchStrength = 0.0f;

        myBottom = GetComponentInChildren<UnderTerrainController>();
        if( myBottom )
        {
            myBottom.ConstructMesh( verticesPerSide, spaceBetweenVertices );
        }

        // load GIS data
        if( addGISTexture )
        {
            // scale down factor: because for some reason it's way too high,
            // but I'm not quite sure by how much / why, so let's just fudge it
            float scaleDownFactor = 10f;
            LoadGISData( verticesPerSide + 2 * extraBorderPixels, scaleDownFactor * terrainHeight );
        }
    }

    private double[] InputVector( float x, float z )
    {
        // kernel method
        return new double[] {
            x, z,
            x * x, z * z, x * z,
            x * x * x, z * z * z, x * x * z, x * z * z, 
            /*Mathf.Sin( 0.1f * x ) + Mathf.Sin, Mathf.Sin( 0.3f * x ), Mathf.Sin( 0.5f * x ), Mathf.Sin( 0.7f * x ),
            // Mathf.Sin( 2 * x ), Mathf.Sin( 5 * x ), Mathf.Sin( 8 * x ),

            Mathf.Sin( 0.1f * z ), Mathf.Sin( 0.3f * z ), Mathf.Sin( 0.5f * z ), Mathf.Sin( 0.7f * z ),
            // Mathf.Sin( 2 * z ), Mathf.Sin( 5 * z ), Mathf.Sin( 8 * z ),

            Mathf.Sin( 0.1f * x * z), Mathf.Sin( 0.3f * x * z ), Mathf.Sin( 0.5f * x * z ), Mathf.Sin( 0.7f * x * z ),
            // Mathf.Sin( 2 * x * z ), Mathf.Sin( 5 * x * z ), Mathf.Sin( 8 * x * z ),
            */
            // x + 10* WeirdSineFeature( x, 0.1f, 0.3f, 0.7f ), x - 70 * WeirdSineFeature( x, 0.5f, 0.6f, 2f ), - x + 39 * WeirdSineFeature( x, 0.1f, 0.2f, 0.3f ),
            // z - 10 * WeirdSineFeature( z, 0.1f, 0.3f, 0.7f ), -z + 70 * WeirdSineFeature( z, 0.5f, 0.6f, 2f ), - z - 39 * WeirdSineFeature( z, 0.1f, 0.2f, 0.3f ),
            // x + z - 40 * WeirdSineFeature( x+z, 0.1f, 0.3f, 0.7f ), x - z + 35 * WeirdSineFeature( x+z, 0.5f, 0.6f, 2f ), z - x + 60 * WeirdSineFeature( x+z, 0.1f, 0.2f, 0.3f ),
            // WeirdSineFeature( x-z, 0.1f, 0.3f, 0.7f ), WeirdSineFeature( x-z, 0.5f, 0.6f, 2f ), WeirdSineFeature( x-z, 0.1f, 0.2f, 0.3f ),
            // WeirdSineFeature( x*z, 0.1f, 0.3f, 0.7f ), WeirdSineFeature( x*z, 0.5f, 0.6f, 2f ), WeirdSineFeature( x*z, 0.1f, 0.2f, 0.3f ),
        };
    }

    private double[] GISInputVector( Vector3 pointNearTerrain )
    {
        Vector3 localPoint = pointNearTerrain - myTerrain.transform.position;
        float normX = Mathf.InverseLerp( 0.0f, myTerrainData.size.x, localPoint.x );
        float normY = Mathf.InverseLerp( 0.0f, myTerrainData.size.z, localPoint.z );

        return GISInputVectorFromNormCoordinates( normX, normY );
    }

    // NOTE: this implementation means that to calculate GIS data, we first need to
    // re-fill the terrain with data from the general heightmap calculation,
    // THEN do the GIS addition, because it relies on the terrain already having
    // steepness information and normals.
    private double[] GISInputVectorFromNormCoordinates( float normX, float normY )
    {
        float normHeight = myTerrainData.GetInterpolatedHeight( normX, normY ) / myTerrainData.heightmapResolution;
        float x = normX, y = normHeight, z = normY;
        return new double[] {
            x, y, z,
            x * x, y * y, z * z,
            x * y, x * z, y * z,
            // TODO consider removing
            // x * x * x, x * x * y, x * x * z,
            // y * y * x, y * y * y, y * y * z,
            // z * z * z, z * z * y, z * z * z,
            x * y * z,
        };
    }

    private float WeirdSineFeature( float f, float a, float b, float c )
    {
        return Mathf.Sin( f * a ) + Mathf.Sin( f * b ) + Mathf.Sin( f * c );
    }

    void Start()
    {
        // edges
        SetNeighbors( true );
        // reset data until told to rescan
        SetTerrainData( true );
    }


    // Update is called once per frame
    void Update()
    {
        if( debugGISFeatures && haveTrainedGIS && debugUI != null )
        {
            double[] gisWeights = myGISRegression.Run( GISInputVector( debugUI.transform.position ) );
            debugUI.text = string.Format( @"Smooth:{0:0.000}
Hill: {1:0.000}
Boulder: {2:0.000}
Mountain: {3:0.000}", gisWeights[0], gisWeights[1], gisWeights[3], gisWeights[4] );
        }
    }

    private Vector3 IndicesToCoordinates( int x, int z )
    {
        // 0, 0 is bottom left corner, NOT center
        return new Vector3( ( x - ( verticesPerSide / 2 ) ) * spaceBetweenVertices, 0, ( z - ( verticesPerSide / 2 ) ) * spaceBetweenVertices );
    }


    private IEnumerator ComputeLandHeight( bool lazy, int framesToSpreadOver, int framesToSpreadGISOver, int framesToSpreadTextureOver )
    {
        if( !haveTrained ) { yield break; }


        // UnityEngine.Profiling.Profiler.BeginSample("Running regression");

        // Added for coroutine
        int totalRuns = myPureRegressionHeights.GetLength( 0 ) * myPureRegressionHeights.GetLength( 1 );
        int runsPerFrame = totalRuns / framesToSpreadOver + 1;
        int runsSoFar = 0;
        // End added for coroutine

        // recompute height
        for( int y = 0; y < myPureRegressionHeights.GetLength( 0 ); y++ )
        {
            for( int x = 0; x < myPureRegressionHeights.GetLength( 1 ); x++ )
            {
                Vector3 localCoords = IndicesToCoordinates( x - extraBorderPixels, y - extraBorderPixels );
                float landHeightHere = (float)myRegression.Run( InputVector( localCoords.x, localCoords.z ) )[0];
                myPureRegressionHeights[y, x] = landHeightHere / terrainHeight;

                if( x >= extraBorderPixels && x < verticesPerSide + extraBorderPixels &&
                    y >= extraBorderPixels && y < verticesPerSide + extraBorderPixels )
                {
                    myModifiedRegressionHeights[y - extraBorderPixels, x - extraBorderPixels] = myPureRegressionHeights[y, x];
                }

                // Added for coroutine
                runsSoFar++;
                if( runsSoFar == runsPerFrame )
                {
                    runsSoFar = 0;

                    // lazy terrain set
                    SetTerrainData( false );
                    ReducedStitchEdges();

                    yield return null;
                }
                // End added for coroutine
            }
        }
        // UnityEngine.Profiling.Profiler.EndSample();

        // UnityEngine.Profiling.Profiler.BeginSample("Copying regression");
        // // it is [y,x]
        // for( int y = 0; y < verticesPerSide; y++ )
        // {
        //     for( int x = 0; x < verticesPerSide; x++ )
        //     {
        //         myModifiedRegressionHeights[ y, x ] = myPureRegressionHeights[ y + extraBorderPixels, x + extraBorderPixels ];
        //     }
        // }
        // UnityEngine.Profiling.Profiler.EndSample();


        if( !lazy )
        {
            // before stitching, run GIS
            if( addGISTexture )
            {
                // add the data into the terrain component in a lazy way so we can compute features.. s a d
                SetTerrainData( false );

                // now that we have the data in place to compute features, train GIS
                TrainGISRegression();

                // compute GIS
                yield return StartCoroutine( ComputeGISAddition( framesToSpreadGISOver ) );
            }
            // on a final pass, rescan the textures when the height is re-finalized
            // do this BEFORE smoothing edges -- that way you can "copy" one terrain to another
            // and the texture will look the same instead of wildly different
            
            // to do this,
            // add the data into the terrain component in a lazy way so we can compute features.. s a d
            SetTerrainData( false );
            // do it!
            yield return StartCoroutine( myTextureController.RescanProvidedExamples( framesToSpreadTextureOver ) );

            // smoothing
            SmoothEdgeRegion();
            SetTerrainData( true );
            SetNeighbors( true );
            StitchEdges();
            SetBottomTerrainData( true );
            
            // finally, update all spawned object positions
            SpawnedObject.ResetSpawnedObjectHeights();

            // and tell anyone else listening to reset
            NotifyWhenChanges.Terrain();
        
        }
        else
        {
            // TODO: could maybe still restitch neighbors...
            UnityEngine.Profiling.Profiler.BeginSample( "Lazy terrain set" );
            SmoothEdgeRegion();
            SetTerrainData( finalize: false );
            StitchEdges();
            UnityEngine.Profiling.Profiler.EndSample();
        }
    }

    private IEnumerator ComputeGISAddition( int framesToSpreadOver )
    {
        if( !haveTrainedGIS ) { yield break; }

        // Added for coroutine
        int totalRuns = myPureRegressionHeights.GetLength( 0 ) * myPureRegressionHeights.GetLength( 1 );
        int runsPerFrame = totalRuns / framesToSpreadOver + 1;
        int runsSoFar = 0;
        // End added for coroutine

        for( int y = 0; y < myPureRegressionHeights.GetLength( 0 ); y++ )
        {
            for( int x = 0; x < myPureRegressionHeights.GetLength( 1 ); x++ )
            {
                // calculate addition
                float addition = 0;

                // Normalise x/y coordinates to range 0-1 
                float x_01 = (float)x / (float)myTerrainData.alphamapWidth;
                float y_01 = (float)y / (float)myTerrainData.alphamapHeight;

                double[] gisWeights = myGISRegression.Run( GISInputVectorFromNormCoordinates( x_01, y_01 ) );
                // calculate potential scale factor
                float sum = 0f;
                for( int i = 1; i < gisWeights.Length; i++ ) { sum += Mathf.Clamp01( (float) gisWeights[i] ); }

                // calculate added height based on the GIS data we loaded
                for( int i = 0; i < loadedGISData.GetLength( 0 ); i++ )
                {
                    addition += Mathf.Clamp01( (float) gisWeights[ i + 1 ] ) * loadedGISData[i, y, x];
                }

                // downweight only if it gets too strong
                // if( sum > 1 )
                // {
                //     addition /= sum;
                // }

                // 0th is smooth -- use this to mute other features
                // DISABLE SMOOTHING FOR NOW
                // addition *= 1 - Mathf.Clamp01( (float) gisWeights[0] );

                // note the GIS data is already normalized to terrainHeight so we don't need to normalize it here
                myPureRegressionHeights[y, x] += addition;

                // re-store into myModifiedRegressionHeights too
                if( x >= extraBorderPixels && x < verticesPerSide + extraBorderPixels &&
                    y >= extraBorderPixels && y < verticesPerSide + extraBorderPixels )
                {
                    myModifiedRegressionHeights[y - extraBorderPixels, x - extraBorderPixels] = myPureRegressionHeights[y, x];
                }


                // Added for coroutine
                runsSoFar++;
                if( runsSoFar == runsPerFrame )
                {
                    runsSoFar = 0;

                    // lazy terrain set
                    SetTerrainData( false );
                    ReducedStitchEdges();

                    yield return null;
                }
                // End added for coroutine

            }
        }
    }

    private void SetTerrainData( bool finalize )
    {
        // set vertices from 0,0 corner
        myTerrainData.SetHeightsDelayLOD( 0, 0, myModifiedRegressionHeights );

        // can wait to do this ONLY AFTER operation is done, so don't need to keep calling it
        // if we do a long gesture or show change over time
        if( finalize ) { myTerrainData.SyncHeightmap(); }
    }

    private void SetBottomTerrainData( bool doNeighbors = false )
    {
        // set bottom too 
        if( myBottom )
        {
            myBottom.SetHeight( myTerrainData.GetHeights( 0, 0, verticesPerSide, verticesPerSide ), terrainHeight );
        }

        if( doNeighbors )
        {
            if( upperNeighbor )
            {
                upperNeighbor.SetBottomTerrainData();
                if( upperNeighbor.leftNeighbor ) { upperNeighbor.leftNeighbor.SetBottomTerrainData(); }
                if( upperNeighbor.rightNeighbor ) { upperNeighbor.rightNeighbor.SetBottomTerrainData(); }
            }
            if( leftNeighbor ) { leftNeighbor.SetBottomTerrainData(); }
            if( rightNeighbor ) { rightNeighbor.SetBottomTerrainData(); }
            if( lowerNeighbor )
            {
                lowerNeighbor.SetBottomTerrainData();
                if( lowerNeighbor.leftNeighbor ) { lowerNeighbor.leftNeighbor.SetBottomTerrainData(); }
                if( lowerNeighbor.rightNeighbor ) { lowerNeighbor.rightNeighbor.SetBottomTerrainData(); }
            }
        }
    }


    private void SetNeighbors( bool recursive = false )
    {
        myTerrain.SetNeighbors(
            leftNeighbor ? leftNeighbor.GetComponentInChildren<Terrain>() : null,
            upperNeighbor ? upperNeighbor.GetComponentInChildren<Terrain>() : null,
            rightNeighbor ? rightNeighbor.GetComponentInChildren<Terrain>() : null,
            lowerNeighbor ? lowerNeighbor.GetComponentInChildren<Terrain>() : null
        );
        myTerrain.Flush();
        if( !recursive ) { return; }

        if( leftNeighbor )
        {
            leftNeighbor.SetNeighbors( false );
        }
        if( rightNeighbor )
        {
            rightNeighbor.SetNeighbors( false );
        }
        if( upperNeighbor )
        {
            upperNeighbor.SetNeighbors( false );
        }
        if( lowerNeighbor )
        {
            lowerNeighbor.SetNeighbors( false );
        }
    }


    private void SmoothEdgeRegion()
    {
        // TODO: shadows are sometimes wrong, read this article https://docs.unity3d.com/Manual/BestPracticeLightingPipelines.html
        if( leftNeighbor )
        {
            LerpColsLeftOntoRight( leftNeighbor.myPureRegressionHeights, myPureRegressionHeights, myModifiedRegressionHeights, extraBorderPixels );
            LerpColsRightOntoLeft( myPureRegressionHeights, leftNeighbor.myPureRegressionHeights, leftNeighbor.myModifiedRegressionHeights, extraBorderPixels );
            leftNeighbor.SetTerrainData( true );
        }
        if( rightNeighbor )
        {
            LerpColsLeftOntoRight( myPureRegressionHeights, rightNeighbor.myPureRegressionHeights, rightNeighbor.myModifiedRegressionHeights, extraBorderPixels );
            LerpColsRightOntoLeft( rightNeighbor.myPureRegressionHeights, myPureRegressionHeights, myModifiedRegressionHeights, extraBorderPixels );
            rightNeighbor.SetTerrainData( true );
        }
        if( lowerNeighbor )
        {
            LerpRowsBottomOntoTop( lowerNeighbor.myPureRegressionHeights, myPureRegressionHeights, myModifiedRegressionHeights, extraBorderPixels );
            LerpRowsTopOntoBottom( myPureRegressionHeights, lowerNeighbor.myPureRegressionHeights, lowerNeighbor.myModifiedRegressionHeights, extraBorderPixels );
            lowerNeighbor.SetTerrainData( true );
        }
        if( upperNeighbor )
        {
            LerpRowsBottomOntoTop( myPureRegressionHeights, upperNeighbor.myPureRegressionHeights, upperNeighbor.myModifiedRegressionHeights, extraBorderPixels );
            LerpRowsTopOntoBottom( upperNeighbor.myPureRegressionHeights, myPureRegressionHeights, myModifiedRegressionHeights, extraBorderPixels );
            upperNeighbor.SetTerrainData( true );
        }
    }

    private void LerpColsLeftOntoRight( float[,] leftCols, float[,] rightCols, float[,] output, int samplesToLerp )
    {
        for( int y = 0; y < verticesPerSide; y++ )
        {
            for( int x = 0; x < samplesToLerp; x++ )
            {
                output[y, x] = Mathf.SmoothStep(
                    leftCols[samplesToLerp + y, samplesToLerp + verticesPerSide + x],
                    rightCols[samplesToLerp + y, samplesToLerp + x],
                    0.5f + 0.5f * x / samplesToLerp
                );
            }
        }
    }

    private void LerpColsRightOntoLeft( float[,] rightCols, float[,] leftCols, float[,] output, int samplesToLerp )
    {
        for( int y = 0; y < verticesPerSide; y++ )
        {
            for( int x = 0; x < samplesToLerp; x++ )
            {
                output[y, verticesPerSide - 1 - x] = Mathf.SmoothStep(
                    rightCols[samplesToLerp + y, samplesToLerp - 1 - x],
                    leftCols[samplesToLerp + y, samplesToLerp + verticesPerSide - 1 - x],
                    0.5f + 0.5f * x / samplesToLerp
                );
            }
        }
    }

    private void LerpRowsBottomOntoTop( float[,] bottomRows, float[,] topRows, float[,] output, int samplesToLerp )
    {
        for( int x = 0; x < verticesPerSide; x++ )
        {
            for( int y = 0; y < samplesToLerp; y++ )
            {
                output[y, x] = Mathf.SmoothStep(
                    bottomRows[samplesToLerp + verticesPerSide + y, samplesToLerp + x],
                    topRows[samplesToLerp + y, samplesToLerp + x],
                    0.5f + 0.5f * y / samplesToLerp
                );
            }
        }
    }

    private void LerpRowsTopOntoBottom( float[,] topRows, float[,] bottomRows, float[,] output, int samplesToLerp )
    {
        for( int x = 0; x < verticesPerSide; x++ )
        {
            for( int y = 0; y < samplesToLerp; y++ )
            {
                output[verticesPerSide - 1 - y, x] = Mathf.SmoothStep(
                    topRows[samplesToLerp - 1 - y, samplesToLerp + x],
                    bottomRows[samplesToLerp + verticesPerSide - 1 - y, samplesToLerp + x],
                    0.5f + 0.5f * y / samplesToLerp
                );
            }
        }
    }


    private void ReducedStitchEdges()
    {
        // for temporary times
        StitchEdgeLeft();
        StitchEdgeRight();
        StitchEdgeUp();
        StitchEdgeDown();
    }



    private void StitchEdges()
    {
        // Actually, use the companion class
        StitchAllTerrains.Restitch();
        return;

        // re-stitch the grid of 9 surrounding this square.

        // horizontal first
        // top row
        if( upperNeighbor )
        {
            upperNeighbor.StitchEdgeLeft();
            upperNeighbor.StitchEdgeRight();
        }

        // middle row
        StitchEdgeLeft();
        StitchEdgeRight();

        // bottom row
        if( lowerNeighbor )
        {
            lowerNeighbor.StitchEdgeLeft();
            lowerNeighbor.StitchEdgeRight();
        }

        // vertical second
        // top row
        if( upperNeighbor )
        {
            if( upperNeighbor.leftNeighbor )
            {
                upperNeighbor.leftNeighbor.StitchEdgeDown();
            }
            upperNeighbor.StitchEdgeDown();
            if( upperNeighbor.rightNeighbor )
            {
                upperNeighbor.rightNeighbor.StitchEdgeDown();
            }
        }
        // middle row
        if( leftNeighbor )
        {
            leftNeighbor.StitchEdgeDown();
        }
        StitchEdgeDown();
        if( rightNeighbor )
        {
            rightNeighbor.StitchEdgeDown();
        }
    }

    private void StitchEdgesSad()
    {
        if( upperNeighbor && upperNeighbor.leftNeighbor )
        {
            // 1, 2
            upperNeighbor.leftNeighbor.StitchEdgeRight();
            upperNeighbor.leftNeighbor.StitchEdgeDown();
        }
        else if( leftNeighbor && leftNeighbor.upperNeighbor )
        {
            // 1, 2
            leftNeighbor.upperNeighbor.StitchEdgeRight();
            leftNeighbor.upperNeighbor.StitchEdgeDown();
        }

        if( upperNeighbor )
        {
            // 3, 4
            upperNeighbor.StitchEdgeRight();
            upperNeighbor.StitchEdgeDown();
        }

        if( upperNeighbor && upperNeighbor.rightNeighbor )
        {
            // 5 
            upperNeighbor.rightNeighbor.StitchEdgeDown();
        }
        else if( rightNeighbor && rightNeighbor.upperNeighbor )
        {
            // 5
            rightNeighbor.upperNeighbor.StitchEdgeDown();
        }

        if( leftNeighbor )
        {
            // 6, 7
            leftNeighbor.StitchEdgeRight();
            leftNeighbor.StitchEdgeDown();
        }

        // 8, 9
        StitchEdgeRight();
        StitchEdgeDown();

        if( rightNeighbor )
        {
            // 10
            rightNeighbor.StitchEdgeDown();
        }

        if( leftNeighbor && leftNeighbor.lowerNeighbor )
        {
            // 11
            leftNeighbor.lowerNeighbor.StitchEdgeRight();
        }
        else if( lowerNeighbor && lowerNeighbor.leftNeighbor )
        {
            // 11
            lowerNeighbor.leftNeighbor.StitchEdgeRight();
        }

        if( lowerNeighbor )
        {
            // 12 
            lowerNeighbor.StitchEdgeRight();
        }


    }

    private void StitchEdgeLeft()
    {
        if( leftNeighbor )
        {
            Stitch.TerrainStitch(
                leftNeighbor.myTerrainData,
                myTerrainData,
                StitchDirection.Across,
                stitchWidth,
                stitchStrength,
                false
            );
        }
    }

    private void StitchEdgeRight()
    {
        if( rightNeighbor )
        {
            Stitch.TerrainStitch(
                myTerrainData,
                rightNeighbor.myTerrainData,
                StitchDirection.Across,
                stitchWidth,
                stitchStrength,
                false
            );
        }
    }

    private void StitchEdgeUp()
    {
        if( upperNeighbor )
        {
            Stitch.TerrainStitch(
                upperNeighbor.myTerrainData,
                myTerrainData,
                StitchDirection.Down,
                stitchWidth,
                stitchStrength,
                false
            );
        }
    }

    private void StitchEdgeDown()
    {
        if( lowerNeighbor )
        {
            Stitch.TerrainStitch(
                myTerrainData,
                lowerNeighbor.myTerrainData,
                StitchDirection.Down,
                stitchWidth,
                stitchStrength,
                false
            );
        }
    }

    private void TrainRegression()
    {
        UnityEngine.Profiling.Profiler.BeginSample( "Training regression" );
        // only do this when we have examples
        if( myRegressionExamples.Count > 0 )
        {
            // reset the regression
            myRegression.ResetRegression();

            // rerecord all points
            foreach( TerrainHeightExample example in myRegressionExamples )
            {
                // world to local point
                Vector3 point = transform.InverseTransformPoint( example.transform.position );

                // remember
                myRegression.RecordDataPoint( InputVector( point.x, point.z ), new double[] { point.y } );
            }

            // train
            myRegression.Train();

            // remember
            haveTrained = true;
        }
        UnityEngine.Profiling.Profiler.EndSample();
    }


    private void TrainGISRegression()
    {
        // only do this when we have examples
        if( myGISRegressionExamples.Count > 0 )
        {
            // reset the regression
            myGISRegression.ResetRegression();

            // rerecord all points
            foreach( TerrainGISExample example in myGISRegressionExamples )
            {
                // remember
                myGISRegression.RecordDataPoint( GISInputVector( example.transform.position ), example.myValues );
            }

            // train
            myGISRegression.Train();

            haveTrainedGIS = true;
        }
        else
        {
            haveTrainedGIS = false;
        }
    }

    string SerializableByExample.SerializeExamples()
    {
        SerializableTerrainHeightTrainingExamples mySerializableExamples;
        mySerializableExamples = new SerializableTerrainHeightTrainingExamples();
        mySerializableExamples.heightExamples = new List<SerializableTerrainHeightExample>();
        mySerializableExamples.gisExamples = new List<SerializableTerrainGISExample>();

        // height
        foreach( TerrainHeightExample example in myRegressionExamples )
        {
            mySerializableExamples.heightExamples.Add( example.Serialize( this ) );
        }

        // gis
        foreach( TerrainGISExample example in myGISRegressionExamples )
        {
            mySerializableExamples.gisExamples.Add( example.Serialize( this ) );
        }

        // texture
        mySerializableExamples.textureExamples = myTextureController.SerializeExamples();

        // convert to json
        return SerializationManager.ConvertToJSON<SerializableTerrainHeightTrainingExamples>( mySerializableExamples );
    }

    IEnumerator SerializableByExample.LoadExamples( string serializedExamples )
    {
        SerializableTerrainHeightTrainingExamples examples = 
            SerializationManager.ConvertFromJSON<SerializableTerrainHeightTrainingExamples>( serializedExamples );
        
        // height
        for( int i = 0; i < examples.heightExamples.Count; i++ )
        {
            TerrainHeightExample newExample = Instantiate( heightPrefab );
            newExample.ResetFromSerial( examples.heightExamples[i], this );
            // initialize it
            newExample.ManuallySpecifyTerrain( this );
            // don't retrain until end
            ProvideExample( newExample, false );
        }

        // gis
        for( int i = 0; i < examples.gisExamples.Count; i++ )
        {
            TerrainGISExample newExample = Instantiate( gisPrefab );
            newExample.ResetFromSerial( examples.gisExamples[i], this );
            // initialize it
            newExample.ManuallySpecifyTerrain( this );
            // don't retrain until end
            ProvideExample( newExample, false );
        }

        // texture
        myTextureController.LoadExamples( examples.textureExamples );

        // retrain!
        // not lazy, 15 frames for height, 15 frames for GIS, 3 frames for texture
        yield return StartCoroutine( RescanProvidedExamplesCoroutine( false, 15, 15, 3 ) );
    }

    string SerializableByExample.FilenameIdentifier()
    {
        return "terrain_" + serializationIdentifier;
    }
}


[System.Serializable]
public class SerializableTerrainHeightTrainingExamples
{
    public List< SerializableTerrainHeightExample > heightExamples;
    public List< SerializableTerrainGISExample > gisExamples;
    public List< SerializableTerrainTextureExample > textureExamples;
}