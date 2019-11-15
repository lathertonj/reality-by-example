using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Stitchscape;

public class ConnectedTerrainController : MonoBehaviour
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

    public Transform examplePointsContainer;
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
    private List<TerrainHeightExample> myRegressionExamples;
    private bool haveTrained = false;

    private UnderTerrainController myBottom;

    public bool addGISTexture = true;
    private RapidMixRegression myGISRegression;
    private List<TerrainGISExample> myGISRegressionExamples;
    private bool haveTrainedGIS = false;
    private float[,,] loadedGISData;
    private int gisDataFileSideLength = 513;
    private string[] gisDataFiles = { "hillyrivervalley_heightis50", "mountainous2_heightis150", "mountainous3_heightis200", "mountainous1_heightis135", "diagonalcanyon_heightis100" };
    // toning these down by multiplying by 0.X just sort of mellows it out
    // TODO: just need to go back to the original .raw files and lower them so they don't add so much height in the first place.
    private float[] gisDataHeights = { 50, 150, 200, 135, 100 };
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


    public void ProvideExample( TerrainHeightExample example )
    {
        // remember
        myRegressionExamples.Add( example );

        // recompute
        RescanProvidedExamples();
    }

    public void ForgetExample( TerrainHeightExample example )
    {
        // forget
        if( myRegressionExamples.Remove( example ) )
        {
            // recompute
            RescanProvidedExamples();
        }
    }

    public void ProvideExample( TerrainGISExample example )
    {
        if( !addGISTexture ) return;
        // remember
        myGISRegressionExamples.Add( example );

        // recompute
        RescanProvidedExamples();
    }

    public void ForgetExample( TerrainGISExample example )
    {
        if( !addGISTexture ) return;
        // forget
        if( myGISRegressionExamples.Remove( example ) )
        {
            // recompute
            RescanProvidedExamples();
        }
    }

    // TODO: can this be split into two phases: the base data and the GIS data,
    // so that we can only recompute one when it changes? :|
    public void RescanProvidedExamples( bool lazy = false )
    {
        // train and recompute
        TrainRegression();
        if( addGISTexture ) { TrainGISRegression(); }
        int framesToSpreadOver = 15;
        StartCoroutine( ComputeLandHeight( lazy, framesToSpreadOver ) );

        // on a final pass, rescan the textures when the height is re-finalized
        if( !lazy )
        {
            myTextureController.RescanProvidedExamples();
        }
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
        verticesPerSide = myTerrainData.heightmapWidth;
        terrainSize = myTerrainData.size.x; // it is invariant to scale. scaling up doesn't affect the computations here.
        terrainHeight = myTerrainData.size.y;
        spaceBetweenVertices = terrainSize / ( verticesPerSide - 1 );
        myPureRegressionHeights = new float[verticesPerSide + 2 * extraBorderPixels, verticesPerSide + 2 * extraBorderPixels];
        myModifiedRegressionHeights = new float[verticesPerSide, verticesPerSide];
        // stitch width is 15 pixels' worth
        stitchWidth = 15.0f / verticesPerSide;
        stitchStrength = 0.2f;

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
        // FIRST POINT: normalized height at this location in terrain
        // TODO check if this is normalized already or if we need to divide by myTerrainData.heightmapHeight
        float normHeight = myTerrainData.GetInterpolatedHeight( normX, normY ) / myTerrainData.heightmapHeight;

        // SECOND POINT: normalized steepness at this location in terrain
        // according to Unity: "Steepness is given as an angle, 0..90 degrees"
        float normSteepness = myTerrainData.GetSteepness( normX, normY ) / 90.0f;

        // THIRD POINT: the x and z directions of the surface normal (ignore y because that has to do with steepness)
        Vector3 normal = myTerrainData.GetInterpolatedNormal( normX, normY );

        // final vector:
        // height
        // steepness
        // normal x direction
        // normal z direction
        // height * steepness
        // height * normal x direction
        // height * normal z direction
        // steepness * normal x direction
        // steepness * normal z direction
        // could consider adding height * steepness * normal directions...
        // could consider adding norm positions...
        return new double[] {
            normHeight,
            normSteepness,
            normX,
            normY,
            normal.x,
            normal.z
            // normHeight * normX,
            // normHeight * normY,
            // normSteepness * normX,
            // normSteepness * normY
            // normHeight * normSteepness,
            // normHeight * normal.x,
            // normHeight * normal.z,
            // normSteepness * normal.x,
            // normSteepness * normal.z
        };
    }

    private float WeirdSineFeature( float f, float a, float b, float c )
    {
        return Mathf.Sin( f * a ) + Mathf.Sin( f * b ) + Mathf.Sin( f * c );
    }

    void Start()
    {
        if( examplePointsContainer )
        {
            foreach( Transform example in examplePointsContainer )
            {
                // remember
                TerrainHeightExample e = example.GetComponent<TerrainHeightExample>();
                if( e )
                {
                    e.JustPlaced();
                }
            }
        }

        // edges
        SetNeighbors( true );

        if( myRegressionExamples.Count > 0 )
        {
            // train and show
            RescanProvidedExamples();
        }
    }


    // Update is called once per frame
    void Update()
    {

    }

    private Vector3 IndicesToCoordinates( int x, int z )
    {
        // 0, 0 is bottom left corner, NOT center
        return new Vector3( ( x - ( verticesPerSide / 2 ) ) * spaceBetweenVertices, 0, ( z - ( verticesPerSide / 2 ) ) * spaceBetweenVertices );
    }


    private IEnumerator ComputeLandHeight( bool lazy, int framesToSpreadOver )
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
                Vector3 worldCoords = IndicesToCoordinates( x - extraBorderPixels, y - extraBorderPixels );
                float landHeightHere = (float)myRegression.Run( InputVector( worldCoords.x, worldCoords.z ) )[0];
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

                // compute GIS
                ComputeGISAddition();
            }
            SmoothEdgeRegion();
            SetTerrainData( true );
            SetNeighbors( true );
            StitchEdges();
            SetBottomTerrainData( true );
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

    private void ComputeGISAddition()
    {
        if( !haveTrainedGIS ) { return; }

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
                // calculate added height based on the GIS data we loaded
                for( int i = 0; i < loadedGISData.GetLength( 0 ); i++ )
                {
                    addition += Mathf.Clamp01( (float) gisWeights[ i + 1 ] ) * loadedGISData[i, y, x];
                }
                // 0th is smooth -- use this to mute other features
                addition *= 1 - Mathf.Clamp01( (float) gisWeights[0] );

                // note the GIS data is already normalized to terrainHeight so we don't need to normalize it here
                myPureRegressionHeights[y, x] += addition;

                // re-store into myModifiedRegressionHeights too
                if( x >= extraBorderPixels && x < verticesPerSide + extraBorderPixels &&
                    y >= extraBorderPixels && y < verticesPerSide + extraBorderPixels )
                {
                    myModifiedRegressionHeights[y - extraBorderPixels, x - extraBorderPixels] = myPureRegressionHeights[y, x];
                }

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





}
