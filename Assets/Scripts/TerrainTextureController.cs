using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainTextureController : MonoBehaviour
{
    private Terrain myTerrain;
    private TerrainData myTerrainData;
    private RapidMixRegression myRegression;

    // Start is called before the first frame update
    void Start()
    {
        myTerrain = GetComponent<Terrain>();
        myTerrainData = myTerrain.terrainData;
        myRegression = GetComponent<RapidMixRegression>();
        myRegressionExamples = new List<TerrainTextureExample>();
    }

    private List<TerrainTextureExample> myRegressionExamples;
    private bool haveTrained = false;

    public void ProvideExample( TerrainTextureExample example )
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
        ComputeTerrainSplatMaps();
    }

    private void TrainRegression()
    {
        // only do this when we have examples
        if( myRegressionExamples.Count > 0 )
        {
            // reset the regression
            myRegression.ResetRegression();

            // rerecord all points
            foreach( TerrainTextureExample example in myRegressionExamples )
            {
                // remember
                myRegression.RecordDataPoint( InputVector( example.transform.position ), example.myValues );
                Debug.Log( string.Join( ", ", InputVector( example.transform.position ) ) );
            }

            // train
            myRegression.Train();

            // remember
            haveTrained = true;
        }
    }

    private void ComputeTerrainSplatMaps()
    {
        float[, ,] splatmapData = new float[myTerrainData.alphamapWidth, myTerrainData.alphamapHeight, myTerrainData.alphamapLayers];
        if( myTerrainData.alphamapLayers != myRegressionExamples[0].myValues.Length )
        {
            Debug.Log( "Terrain has a different number of layers than the examples know about." );
        }
         
        for( int y = 0; y < myTerrainData.alphamapHeight; y++ )
        {
            for( int x = 0; x < myTerrainData.alphamapWidth; x++ )
            {
                // Normalise x/y coordinates to range 0-1 
                float y_01 = (float)y/(float)myTerrainData.alphamapHeight;
                float x_01 = (float)x/(float)myTerrainData.alphamapWidth;
                 
                
                double[] splatWeights = myRegression.Run( InputVectorFromNormCoordinates( x_01, y_01 ) );
                 
                // Sum of all textures weights must add to 1, so calculate normalization factor from sum of weights
                double sum = 0; for( int i = 0; i < splatWeights.Length; i++ ) { sum += splatWeights[i]; }
                 
                // Loop through each terrain texture
                for( int i = 0; i < splatWeights.Length; i++ )
                {      
                    // Normalize so that sum of all texture weights = 1
                    splatWeights[i] /= sum;
                     
                    // Assign this point to the splatmap array
                    splatmapData[x, y, i] = (float) splatWeights[i];
                }
            }
        }
      
        // Finally assign the new splatmap to the terrainData:
        myTerrainData.SetAlphamaps( 0, 0, splatmapData );
    }

    private double[] InputVector( Vector3 pointNearTerrain )
    {
        Vector3 localPoint = pointNearTerrain - myTerrain.transform.position;
        float normX = Mathf.InverseLerp( 0.0f, myTerrainData.size.x, localPoint.x );
        float normY = Mathf.InverseLerp( 0.0f, myTerrainData.size.z, localPoint.z );

        return InputVectorFromNormCoordinates( normX, normY );
    }

    private double[] InputVectorFromNormCoordinates( float normX, float normY )
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
        return new double[] {
            normHeight * 10,
            normSteepness * 10,
            normal.x,
            normal.z
            // normHeight * normSteepness,
            // normHeight * normal.x,
            // normHeight * normal.z,
            // normSteepness * normal.x,
            // normSteepness * normal.z
        };
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
