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

    public void RescanProvidedExamples( bool lazy = false )
    {
        // train and recompute
        TrainRegression();
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
        myTerrain = GetComponentInChildren<Terrain>();
        myTextureController = GetComponent<ConnectedTerrainTextureController>();

        // initialize list
        myRegressionExamples = new List<TerrainHeightExample>();

        // compute sizes
        verticesPerSide = myTerrain.terrainData.heightmapWidth;
        terrainSize = myTerrain.terrainData.size.x; // it is invariant to scale. scaling up doesn't affect the computations here.
        terrainHeight = myTerrain.terrainData.size.y;
        spaceBetweenVertices = terrainSize / ( verticesPerSide - 1 );
        myPureRegressionHeights = new float[ verticesPerSide + 2 * extraBorderPixels, verticesPerSide + 2 * extraBorderPixels ];
        myModifiedRegressionHeights = new float[ verticesPerSide, verticesPerSide ];
        // stitch width is 15 pixels' worth
        stitchWidth = 15.0f / verticesPerSide;
        stitchStrength = 0.2f;
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
        // return new double[] { x, z, x*x, z*z, x*z };
    }

    private float WeirdSineFeature( float f, float a, float b, float c )
    {
        return Mathf.Sin( f*a ) + Mathf.Sin( f*b ) + Mathf.Sin( f*c );
    }

    void Start() 
    {
        if( examplePointsContainer )
        {
            foreach( Transform example in examplePointsContainer )
            {
                // remember
                TerrainHeightExample e = example.GetComponent<TerrainHeightExample>();
                if( e ) { myRegressionExamples.Add( e ); }
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
        int totalRuns = myPureRegressionHeights.GetLength(0) * myPureRegressionHeights.GetLength(1);
        int runsPerFrame = totalRuns / framesToSpreadOver + 1;
        int runsSoFar = 0;
        // End added for coroutine

        // recompute height
        for( int y = 0; y < myPureRegressionHeights.GetLength(0); y++ )
        {
            for( int x = 0; x < myPureRegressionHeights.GetLength(1); x++ )
            {
                Vector3 worldCoords = IndicesToCoordinates( x - extraBorderPixels, y - extraBorderPixels );
                float landHeightHere = (float) myRegression.Run( InputVector( worldCoords.x, worldCoords.z ) )[0];
                myPureRegressionHeights[ y, x ] = landHeightHere / terrainHeight;

                if( x >= extraBorderPixels && x < verticesPerSide + extraBorderPixels &&
                    y >= extraBorderPixels && y < verticesPerSide + extraBorderPixels )
                {
                    myModifiedRegressionHeights[ y - extraBorderPixels, x - extraBorderPixels ] = myPureRegressionHeights[ y, x ];
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
            SmoothEdgeRegion();
            SetTerrainData( true );
            SetNeighbors( true );
            StitchEdges();
        }
        else
        {
            // TODO: could maybe still restitch neighbors...
            UnityEngine.Profiling.Profiler.BeginSample("Lazy terrain set");
            SmoothEdgeRegion();
            SetTerrainData( false );
            StitchEdges();
            UnityEngine.Profiling.Profiler.EndSample();
        }
    }

    private void SetTerrainData( bool finalize )
    {
        // set vertices from 0,0 corner
        myTerrain.terrainData.SetHeightsDelayLOD( 0, 0, myModifiedRegressionHeights );
        
        // can wait to do this ONLY AFTER operation is done, so don't need to keep calling it
        // if we do a long gesture or show change over time
        if( finalize ) { myTerrain.terrainData.SyncHeightmap(); }
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
                output[ y, x ] = Mathf.SmoothStep( 
                    leftCols[ samplesToLerp + y, samplesToLerp + verticesPerSide + x ],
                    rightCols[ samplesToLerp + y, samplesToLerp + x ],
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
                output[ y, verticesPerSide - 1 - x ] = Mathf.SmoothStep( 
                    rightCols[ samplesToLerp + y, samplesToLerp - 1 - x ],
                    leftCols[ samplesToLerp + y, samplesToLerp + verticesPerSide - 1 - x ],
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
                output[ y, x ] = Mathf.SmoothStep( 
                    bottomRows[ samplesToLerp + verticesPerSide + y, samplesToLerp + x ],
                    topRows[ samplesToLerp + y, samplesToLerp + x ],
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
                output[ verticesPerSide - 1 - y, x ] = Mathf.SmoothStep( 
                    topRows[ samplesToLerp - 1 - y, samplesToLerp + x ],
                    bottomRows[ samplesToLerp + verticesPerSide - 1 - y, samplesToLerp + x ],
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
                leftNeighbor.myTerrain.terrainData, 
                myTerrain.terrainData, 
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
                myTerrain.terrainData, 
                rightNeighbor.myTerrain.terrainData,
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
                upperNeighbor.myTerrain.terrainData, 
                myTerrain.terrainData, 
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
                myTerrain.terrainData, 
                lowerNeighbor.myTerrain.terrainData,
                StitchDirection.Down, 
                stitchWidth, 
                stitchStrength,
                false
            );
        }
    }

    private void TrainRegression()
    {
        UnityEngine.Profiling.Profiler.BeginSample("Training regression");
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



    

}
