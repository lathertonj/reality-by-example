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

    // TODO: refactor to use Unity Terrain so can do procedural splatmapping
    // TODO: 

    // designers better at curating than creating; better to have a computer to generate 100 and have the designer select between them
    
    // texture synthesis by example? paint on the landscape then map it onto new object?


    private Terrain myTerrain;

    public Transform examplePointsContainer;
    public ConnectedTerrainController leftNeighbor, rightNeighbor, upperNeighbor, lowerNeighbor;

    static int extraBorderPixels = 10;

    private int verticesPerSide;
    private float terrainSize;
    private float spaceBetweenVertices;
    private float terrainHeight;
    private float[,] myPureRegressionHeights, myModifiedRegressionHeights;

    // regression
    private RapidMixRegression myRegression;
    private List<Transform> myRegressionExamples;
    private bool haveTrained = false;

    public void ProvideExample( Transform example )
    {
        // remember
        myRegressionExamples.Add( example );

        // recompute
        RescanProvidedExamples();
    }

    public void RescanProvidedExamples()
    {
        // train and recompute
        TrainRegression();
        ComputeLandHeight();
    }


    // Use this for initialization
    void Awake()
    {
        // grab component reference
        myRegression = GetComponent<RapidMixRegression>();
        myTerrain = GetComponentInChildren<Terrain>();

        // initialize list
        myRegressionExamples = new List<Transform>();

        // compute sizes
        verticesPerSide = myTerrain.terrainData.heightmapWidth;
        terrainSize = myTerrain.terrainData.size.x; // it is invariant to scale. scaling up doesn't affect the computations here.
        terrainHeight = myTerrain.terrainData.size.y;
        spaceBetweenVertices = terrainSize / ( verticesPerSide - 1 );
        myPureRegressionHeights = new float[ verticesPerSide + 2 * extraBorderPixels, verticesPerSide + 2 * extraBorderPixels ];
        myModifiedRegressionHeights = new float[ verticesPerSide, verticesPerSide ];
    }

    void Start() 
    {
        if( examplePointsContainer )
        {
            foreach( Transform example in examplePointsContainer )
            {
                // remember
                myRegressionExamples.Add( example );
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


    private void ComputeLandHeight()
    {
        if( !haveTrained ) { return; }

        // recompute height
        for( int y = 0; y < myPureRegressionHeights.GetLength(0); y++ )
        {
            for( int x = 0; x < myPureRegressionHeights.GetLength(1); x++ )
            {
                Vector3 worldCoords = IndicesToCoordinates( x - extraBorderPixels, y - extraBorderPixels );
                float landHeightHere = (float) myRegression.Run( InputVector( worldCoords.x, worldCoords.z ) )[0];
                myPureRegressionHeights[ y, x ] = landHeightHere / terrainHeight;
            }
        }

        // it is [y,x]
        for( int y = 0; y < verticesPerSide; y++ )
        {
            for( int x = 0; x < verticesPerSide; x++ )
            {
                myModifiedRegressionHeights[ y, x ] = myPureRegressionHeights[ y + extraBorderPixels, x + extraBorderPixels ];
            }
        }

        SmoothEdgeRegion();
        SetTerrainData();
        SetNeighbors( true );
        StitchEdges();
    }

    private void SetTerrainData()
    {
        // set vertices from 0,0 corner
        myTerrain.terrainData.SetHeightsDelayLOD( 0, 0, myModifiedRegressionHeights );
        
        // NOTE: can wait to do this ONLY AFTER operation is done, so don't need to keep calling it
        // if we do a long gesture or show change over time
        myTerrain.terrainData.SyncHeightmap();
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
        // TODO: Lerp functions sometimes have weird artifacts at the corners
        // TODO: shadows are wrong, why?
        if( leftNeighbor )
        {
            LerpColsLeftOntoRight( leftNeighbor.myPureRegressionHeights, myPureRegressionHeights, myModifiedRegressionHeights, extraBorderPixels );
            LerpColsRightOntoLeft( myPureRegressionHeights, leftNeighbor.myPureRegressionHeights, leftNeighbor.myModifiedRegressionHeights, extraBorderPixels );
            leftNeighbor.SetTerrainData();
        }
        if( rightNeighbor )
        {
            LerpColsLeftOntoRight( myPureRegressionHeights, rightNeighbor.myPureRegressionHeights, rightNeighbor.myModifiedRegressionHeights, extraBorderPixels );
            LerpColsRightOntoLeft( rightNeighbor.myPureRegressionHeights, myPureRegressionHeights, myModifiedRegressionHeights, extraBorderPixels );
            rightNeighbor.SetTerrainData();
        }
        if( lowerNeighbor )
        {
            LerpRowsBottomOntoTop( lowerNeighbor.myPureRegressionHeights, myPureRegressionHeights, myModifiedRegressionHeights, extraBorderPixels );
            LerpRowsTopOntoBottom( myPureRegressionHeights, lowerNeighbor.myPureRegressionHeights, lowerNeighbor.myModifiedRegressionHeights, extraBorderPixels );
            lowerNeighbor.SetTerrainData();
        }
        if( upperNeighbor )
        {
            LerpRowsBottomOntoTop( myPureRegressionHeights, upperNeighbor.myPureRegressionHeights, upperNeighbor.myModifiedRegressionHeights, extraBorderPixels );
            LerpRowsTopOntoBottom( upperNeighbor.myPureRegressionHeights, myPureRegressionHeights, myModifiedRegressionHeights, extraBorderPixels );
            upperNeighbor.SetTerrainData();
        }
    }

    private void LerpColsLeftOntoRight( float[,] leftCols, float[,] rightCols, float[,] output, int samplesToLerp )
    {
        for( int y = 0; y < verticesPerSide; y++ )
        {
            for( int x = 0; x < samplesToLerp; x++ )
            {
                output[ y, x ] = Mathf.Lerp( 
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
                output[ y, verticesPerSide - x - 1 ] = Mathf.Lerp( 
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
                output[ y, x ] = Mathf.Lerp( 
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
                output[ verticesPerSide - 1 - y, 1 ] = Mathf.Lerp( 
                    topRows[ samplesToLerp - 1 - y, samplesToLerp + x ],
                    bottomRows[ samplesToLerp + verticesPerSide - 1 - y, samplesToLerp + x ],
                    0.5f + 0.5f * y / samplesToLerp
                );
            }
        }
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
                1.0f * extraBorderPixels / verticesPerSide, 
                0.5f, 
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
                1.0f * extraBorderPixels / verticesPerSide, 
                0.5f, 
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
                1.0f * extraBorderPixels / verticesPerSide, 
                0.5f, 
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
                1.0f * extraBorderPixels / verticesPerSide, 
                0.5f, 
                false
            );
        }
    }

    private void TrainRegression()
    {
        // only do this when we have examples
        if( myRegressionExamples.Count > 0 )
        {
            // reset the regression
            myRegression.ResetRegression();

            // rerecord all points
            foreach( Transform example in myRegressionExamples )
            {
                // world to local point
                Vector3 point = transform.InverseTransformPoint( example.position );

                // remember
                myRegression.RecordDataPoint( InputVector( point.x, point.z ), new double[] { point.y } );
            }

            // train
            myRegression.Train();

            // remember
            haveTrained = true;
        }
    }



    private double[] InputVector( float x, float z )
    {
        // kernel method
        return new double[] { x, z, x * x, z * z, x * z, x * x * x, z * z * z, x * x * z, x * z * z };
        // return new double[] { x, z, x*x, z*z, x*z };
    }

}
