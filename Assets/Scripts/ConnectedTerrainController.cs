using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private int verticesPerSide;
    private float terrainSize;
    private float spaceBetweenVertices;
    private float terrainHeight;

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

    // Use this for initialization
    void Start()
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


        foreach( Transform example in examplePointsContainer )
        {
            // remember
            myRegressionExamples.Add( example );
        }
        // train and show
        RescanProvidedExamples();

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

        // it is [y,x]
        float[,] newHeights = new float[ verticesPerSide, verticesPerSide ];        
        
        // recompute height
        for( int y = 0; y < verticesPerSide; y++ )
        {
            for( int x = 0; x < verticesPerSide; x++ )
            {
                Vector3 worldCoords = IndicesToCoordinates( x, y );
                float landHeightHere = (float) myRegression.Run( InputVector( worldCoords.x, worldCoords.z ) )[0];
                newHeights[ y, x ] = landHeightHere / terrainHeight;
            }
        }

        // set vertices from 0,0 corner
        myTerrain.terrainData.SetHeightsDelayLOD( 0, 0, newHeights );
        
        // NOTE: can do this ONLY AFTER operation is done, so don't need to keep calling it
        // if we do a long gesture
        myTerrain.terrainData.SyncHeightmap();

    }



}
