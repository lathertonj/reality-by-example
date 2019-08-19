using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainTextureClassifierController : MonoBehaviour
{
    private Terrain myTerrain;
    private TerrainData myTerrainData;
    private RapidMixClassifier myClassifier;

    // Classifier looks worse 
    // BUT I think it is helping me figure out that the visuals are
    // being applied to the map rotated?

    // Start is called before the first frame update
    void Start()
    {
        myTerrain = GetComponent<Terrain>();
        myTerrainData = myTerrain.terrainData;
        myClassifier = GetComponent<RapidMixClassifier>();
        myClassifierExamples = new List<TerrainTextureExample>();
    }

    private List<TerrainTextureExample> myClassifierExamples;
    private bool haveTrained = false;

    public void ProvideExample( TerrainTextureExample example )
    {
        // remember
        myClassifierExamples.Add( example );

        // recompute
        RescanProvidedExamples();
    }

    public void RescanProvidedExamples()
    {
        // train and recompute
        TrainClassifier();
        ComputeTerrainSplatMaps();
    }

    private void TrainClassifier()
    {
        // only do this when we have examples
        if( myClassifierExamples.Count > 0 )
        {
            // reset the classifier
            myClassifier.ResetClassifier();

            // rerecord all points
            foreach( TerrainTextureExample example in myClassifierExamples )
            {
                // remember
                myClassifier.RecordDataPoint( InputVector( example.transform.position ), example.myLabel );
                //Debug.Log( string.Join( ", ", InputVector( example.transform.position ) ) );
            }

            // train
            myClassifier.Train();

            // remember
            haveTrained = true;
        }
    }

    private void ComputeTerrainSplatMaps()
    {
        float[, ,] splatmapData = new float[myTerrainData.alphamapHeight, myTerrainData.alphamapWidth, myTerrainData.alphamapLayers];
        if( myTerrainData.alphamapLayers != myClassifierExamples[0].myValues.Length )
        {
            Debug.Log( "Terrain has a different number of layers than the examples know about." );
        }
         
        for( int x = 0; x < myTerrainData.alphamapWidth; x++ )
        {
            for( int y = 0; y < myTerrainData.alphamapHeight; y++ )
            {
                // Normalise x/y coordinates to range 0-1 
                float x_01 = (float)x/(float)myTerrainData.alphamapWidth;
                float y_01 = (float)y/(float)myTerrainData.alphamapHeight;
                 
                
                string label = myClassifier.Run( InputVectorFromNormCoordinates( x_01, y_01 ) );
                double[] splatWeights = new double[] {0, 0, 0, 0};
                if( label == "1" )
                {
                    splatWeights[1] = 1;
                }
                else if( label == "2" ) 
                {
                    splatWeights[2] = 1;
                }
                else if( label == "3" )
                {
                    splatWeights[3] = 1;
                }
                else
                {
                    splatWeights[0] = 1;
                }
                 
                // Sum of all textures weights must add to 1, so calculate normalization factor from sum of weights
                //double sum = 0; for( int i = 0; i < splatWeights.Length; i++ ) { sum += splatWeights[i]; }
                 
                // Loop through each terrain texture
                for( int i = 0; i < splatWeights.Length; i++ )
                {      
                    // Normalize so that sum of all texture weights = 1
                    //splatWeights[i] /= sum;
                     
                    // Assign this point to the splatmap array
                    // NOTE: The unusual indexing of the array!
                    // it is Y, then X!
                    // it took me forever to debug this!
                    splatmapData[y, x, i] = (float) splatWeights[i];
                }
            }
        }
      
        // Finally assign the new splatmap to the terrainData:
        myTerrainData.SetAlphamaps( 0, 0, splatmapData );
    }

    private double[] InputVector( Vector3 pointNearTerrain )
    {
        // TODO: I am not sure I am convinced that this math is correct.
        // Something seems like it is going wrong because when my vector is ONLY height,
        // I can't seem to get reasonable height-based mappings.
        // verified that the height feature extractor is working, though
        // let's try a classifier instead of 4-output regression...
        Vector3 localPoint = pointNearTerrain - myTerrain.transform.position;
        float normX = Mathf.InverseLerp( 0.0f, myTerrainData.size.x, localPoint.x );
        float normY = Mathf.InverseLerp( 0.0f, myTerrainData.size.z, localPoint.z );

        return InputVectorFromNormCoordinates( normX, normY );
    }

    private double[] InputVectorFromNormCoordinates( float normX, float normY )
    {
        // FIRST POINT: normalized height at this location in terrain
        // TODO check if this is normalized already or if we need to divide by myTerrainData.heightmapHeight
        float normHeight = myTerrainData.GetInterpolatedHeight( normX, normY );// / myTerrainData.heightmapHeight;
        
        // SECOND POINT: normalized steepness at this location in terrain
        // according to Unity: "Steepness is given as an angle, 0..90 degrees"
        float normSteepness = myTerrainData.GetSteepness( normX, normY );// / 90.0f;

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
            normHeight
            // normSteepness,
            // normX * myTerrainData.size.x,
            // normY * myTerrainData.size.z
            // normHeight * normX,
            // normHeight * normY,
            // normSteepness * normX,
            // normSteepness * normY
            // normal.x * 20,
            // normal.z * 20
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
