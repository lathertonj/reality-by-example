using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Photon.Pun;

public class ConnectedTerrainTextureController : MonoBehaviour , IPhotonExampleRescanner
{
    private Terrain myTerrain;
    private TerrainData myTerrainData;
    private RapidMixRegression myRegression;

    public TerrainTextureExample terrainExamplePrefab;
    public bool isPrefabNetworked;


    private ConnectedTerrainTextureController leftNeighbor, rightNeighbor, upperNeighbor, lowerNeighbor;
    private ConnectedTerrainController myHeightController;

    private int yMax, xMax, iMax;
    private float[,,] pureSplatmapData, blendedSplatmapData;
    private static float[,,] whiteSplatmapData = null;
    private int overlapPixels = 5, cornerOverlapPixels = 3, edgeOverlapPixels = 8;

    // TODO: how to smooth texture?
    // I can't compute extra beyond the regions of the terrain
    // like I do for terrain height, because there is no way
    // to compute the features for an area of the terrain that
    // technically doesn't exist...

    // --> try blending it with just the values at the edge of the final row of the other space?
    // but then it would be blended into something that doesn't necessarily exist there anymore...
    // I guess this is why people mirror things. I will try that. Will have to be careful to get the values exactly
    // identical at the edges, so that it is half-and-half for the border for both of them.

    // Start is called before the first frame update
    void Awake()
    {
        myTerrain = GetComponentInChildren<Terrain>();
        myTerrainData = myTerrain.terrainData;
        myRegression = gameObject.AddComponent<RapidMixRegression>();
        myRegressionExamples = new List<TerrainTextureExample>();
        myHeightController = GetComponent<ConnectedTerrainController>();

        InitSplats();
    }

    void InitSplats()
    {
        yMax = myTerrainData.alphamapHeight;
        xMax = myTerrainData.alphamapWidth;
        iMax = myTerrainData.alphamapLayers;

        pureSplatmapData = new float[yMax, xMax, iMax];
        blendedSplatmapData = new float[yMax, xMax, iMax];
        if( whiteSplatmapData == null )
        {
            whiteSplatmapData = new float[yMax, xMax, iMax ];
        }

        for( int y = 0; y < yMax; y++ )
        {
            for( int x = 0; x < xMax; x++ )
            {
                // white is currently the 2nd (0 indexed 3rd) material
                whiteSplatmapData[y, x, 2] = 1;
                pureSplatmapData[y, x, 0] = 1;
                for( int i = 1; i < iMax; i++ )
                {
                    pureSplatmapData[y, x, i] = 0;
                }
            }
        }
        CopyPureIntoBlended();
        SetTerrainData();
    }

    void Start()
    {
        leftNeighbor = myHeightController.leftNeighbor ? myHeightController.leftNeighbor.GetComponent<ConnectedTerrainTextureController>() : null;
        rightNeighbor = myHeightController.rightNeighbor ? myHeightController.rightNeighbor.GetComponent<ConnectedTerrainTextureController>() : null;
        upperNeighbor = myHeightController.upperNeighbor ? myHeightController.upperNeighbor.GetComponent<ConnectedTerrainTextureController>() : null;
        lowerNeighbor = myHeightController.lowerNeighbor ? myHeightController.lowerNeighbor.GetComponent<ConnectedTerrainTextureController>() : null;
    }

    [HideInInspector] public List<TerrainTextureExample> myRegressionExamples;
    private bool haveTrained = false;

    public void ProvideExample( TerrainTextureExample example, bool shouldRetrain = true )
    {
        // remember
        myRegressionExamples.Add( example );

        // recompute
        if( shouldRetrain )
        {
            RescanProvidedExamples();
        }
    }

    public void ForgetExample( TerrainTextureExample example, bool shouldRetrain = true )
    {
        // forget
        if( myRegressionExamples.Remove( example ) && shouldRetrain )
        {
            // recompute
            RescanProvidedExamples();
        }
    }

    // called before my examples are overwritten with other's examples
    public void MatchNumberOfExamples( ConnectedTerrainTextureController other )
    {
        // first, perhaps delete extra examples
        while( myRegressionExamples.Count > other.myRegressionExamples.Count )
        {
            TerrainTextureExample exampleToRemove = myRegressionExamples[0];
            ForgetExample( exampleToRemove, false );
            // destroy (networked or normal)
            if( isPrefabNetworked )
            {
                PhotonNetwork.Destroy( exampleToRemove.gameObject );
            }
            else
            {
                Destroy( exampleToRemove.gameObject );
            }
        }

        // next, perhaps add blank examples
        while( myRegressionExamples.Count < other.myRegressionExamples.Count )
        {
            TerrainTextureExample newExample;
            if( isPrefabNetworked )
            {
                newExample = PhotonNetwork.Instantiate( terrainExamplePrefab.name, transform.position, Quaternion.identity )
                    .GetComponent<TerrainTextureExample>();
            }
            else
            {
                newExample = Instantiate( terrainExamplePrefab, transform.position, Quaternion.identity );
            }
            ProvideExample( newExample, false );
        }
    }

    public void RescanProvidedExamples()
    {
        StartCoroutine( RescanProvidedExamples( 3 ) );
    }

    public IEnumerator RescanProvidedExamples( int framesToSpreadOver )
    {
        // train and recompute
        TrainRegression();
        yield return StartCoroutine( ComputeTerrainSplatMaps( framesToSpreadOver ) );
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
                //Debug.Log( string.Join( ", ", InputVector( example.transform.position ) ) );
            }

            // train
            myRegression.Train();

            // remember
            haveTrained = true;
        }
    }

    private IEnumerator ComputeTerrainSplatMaps( int framesToSpreadOver )
    {
        if( !haveTrained ) { yield break; }
        if( myTerrainData.alphamapLayers != myRegressionExamples[0].myValues.Length )
        {
            Debug.Log( "Terrain has a different number of layers than the examples know about." );
        }

        // Added for coroutine
        int totalRuns = myTerrainData.alphamapWidth * myTerrainData.alphamapHeight;
        int runsPerFrame = totalRuns / framesToSpreadOver + 1;
        int runsSoFar = 0;
        // End added for coroutine

        for( int x = 0; x < myTerrainData.alphamapWidth; x++ )
        {
            for( int y = 0; y < myTerrainData.alphamapHeight; y++ )
            {
                // Normalise x/y coordinates to range 0-1 
                float x_01 = (float)x / (float)myTerrainData.alphamapWidth;
                float y_01 = (float)y / (float)myTerrainData.alphamapHeight;


                double[] splatWeights = myRegression.Run( InputVectorFromNormCoordinates( x_01, y_01 ) );

                // Loop through each terrain texture
                for( int i = 0; i < splatWeights.Length; i++ )
                {
                    // Assign this point to the splatmap array
                    // NOTE: The unusual indexing of the array!
                    // it is Y, then X!
                    // it took me forever to debug this!
                    pureSplatmapData[y, x, i] = (float)splatWeights[i];
                }

                NormSplatWeights( pureSplatmapData, y, x );


                // Added for coroutine
                runsSoFar++;
                if( runsSoFar == runsPerFrame )
                {
                    runsSoFar = 0;

                    // do not lazy terrain set
                    // CopyPureIntoBlended();
                    // SetTerrainData();

                    yield return null;
                }
                // End added for coroutine
            }
        }

        // copy data
        CopyPureIntoBlended();

        // now blend
        BlendWithNeighbors();

        // Finally assign the new splatmap to the terrainData:
        SetTerrainData();
    }

    private void CopyPureIntoBlended()
    {
        System.Array.Copy( pureSplatmapData, blendedSplatmapData,
            pureSplatmapData.GetLength( 0 ) * pureSplatmapData.GetLength( 1 ) * pureSplatmapData.GetLength( 2 ) );
    }

    private void SetTerrainData()
    {
        myTerrainData.SetAlphamaps( 0, 0, blendedSplatmapData );
    }

    private void NormSplatWeights( float[,,] weights, int y, int x )
    {
        float sum = 0;
        for( int i = 0; i < iMax; i++ )
        {
            sum += weights[y, x, i];
        }
        for( int i = 0; i < iMax; i++ )
        {
            weights[y, x, i] /= sum;
        }
    }

    private void NormSplatVector( float[] v )
    {
        float sum = 0;
        for( int i = 0; i < v.Length; i++ )
        {
            sum += v[i];
        }
        for( int i = 0; i < v.Length; i++ )
        {
            v[i] /= sum;
        }
    }

    private void BlendWithNeighbors()
    {
        // edges
        // left
        if( leftNeighbor )
        {
            LerpColsLeftOntoRight( leftNeighbor.pureSplatmapData, pureSplatmapData, blendedSplatmapData, overlapPixels );
            LerpColsRightOntoLeft( pureSplatmapData, leftNeighbor.pureSplatmapData, leftNeighbor.blendedSplatmapData, overlapPixels );
        }
        else
        {
            LerpColsLeftOntoRight( whiteSplatmapData, pureSplatmapData, blendedSplatmapData, edgeOverlapPixels, true );
        }

        // right
        if( rightNeighbor )
        {
            LerpColsLeftOntoRight( pureSplatmapData, rightNeighbor.pureSplatmapData, rightNeighbor.blendedSplatmapData, overlapPixels );
            LerpColsRightOntoLeft( rightNeighbor.pureSplatmapData, pureSplatmapData, blendedSplatmapData, overlapPixels );
        }
        else
        {
            LerpColsRightOntoLeft( whiteSplatmapData, pureSplatmapData, blendedSplatmapData, edgeOverlapPixels, true );
        }

        // lower
        if( lowerNeighbor )
        {
            LerpRowsBottomOntoTop( lowerNeighbor.pureSplatmapData, pureSplatmapData, blendedSplatmapData, overlapPixels );
            LerpRowsTopOntoBottom( pureSplatmapData, lowerNeighbor.pureSplatmapData, lowerNeighbor.blendedSplatmapData, overlapPixels );
        }
        else
        {
            LerpRowsBottomOntoTop( whiteSplatmapData, pureSplatmapData, blendedSplatmapData, edgeOverlapPixels, true );
        }

        // upper
        if( upperNeighbor )
        {
            LerpRowsBottomOntoTop( pureSplatmapData, upperNeighbor.pureSplatmapData, upperNeighbor.blendedSplatmapData, overlapPixels );
            LerpRowsTopOntoBottom( upperNeighbor.pureSplatmapData, pureSplatmapData, blendedSplatmapData, overlapPixels );
        }
        else
        {
            LerpRowsTopOntoBottom( whiteSplatmapData, pureSplatmapData, blendedSplatmapData, edgeOverlapPixels, true );
        }

        // corners
        if( leftNeighbor && leftNeighbor.upperNeighbor && upperNeighbor )
        {
            LerpCorner( 
                leftNeighbor.upperNeighbor.blendedSplatmapData, 
                upperNeighbor.blendedSplatmapData, 
                leftNeighbor.blendedSplatmapData,
                blendedSplatmapData,
                cornerOverlapPixels
            );
        }

        if( upperNeighbor && upperNeighbor.rightNeighbor && rightNeighbor )
        {
            LerpCorner( 
                upperNeighbor.blendedSplatmapData, 
                upperNeighbor.rightNeighbor.blendedSplatmapData, 
                blendedSplatmapData,
                rightNeighbor.blendedSplatmapData,
                cornerOverlapPixels
            );
        }

        if( leftNeighbor && leftNeighbor.lowerNeighbor && lowerNeighbor )
        {
            LerpCorner( 
                leftNeighbor.blendedSplatmapData, 
                blendedSplatmapData,
                leftNeighbor.lowerNeighbor.blendedSplatmapData, 
                lowerNeighbor.blendedSplatmapData,
                cornerOverlapPixels
            );
        }

        if( rightNeighbor && rightNeighbor.lowerNeighbor && lowerNeighbor )
        {
            LerpCorner( 
                blendedSplatmapData,
                rightNeighbor.blendedSplatmapData, 
                lowerNeighbor.blendedSplatmapData, 
                rightNeighbor.lowerNeighbor.blendedSplatmapData,
                cornerOverlapPixels
            );
        }


        // set
        if( leftNeighbor )
        {
            leftNeighbor.SetTerrainData();
        }
        if( rightNeighbor )
        {
            rightNeighbor.SetTerrainData();
        }
        if( lowerNeighbor )
        {
            lowerNeighbor.SetTerrainData();
            if( lowerNeighbor.leftNeighbor ) { lowerNeighbor.leftNeighbor.SetTerrainData(); }
            if( lowerNeighbor.rightNeighbor ) { lowerNeighbor.rightNeighbor.SetTerrainData(); }
        }
        if( upperNeighbor )
        {
            upperNeighbor.SetTerrainData();
            if( upperNeighbor.leftNeighbor ) { upperNeighbor.leftNeighbor.SetTerrainData(); }
            if( upperNeighbor.rightNeighbor ) { upperNeighbor.rightNeighbor.SetTerrainData(); }
        }
    }

    private void LerpColsLeftOntoRight( float[,,] leftCols, float[,,] rightCols, float[,,] output, int samplesToLerp, bool quicker = false )
    {
        for( int x = 0; x < samplesToLerp; x++ )
        {
            // ignore diagonals at corners
            for( int y = x; y < yMax - x; y++ )
            {
                for( int i = 0; i < iMax; i++ )
                {
                    output[y, x, i] = Mathf.SmoothStep(
                        leftCols[y, xMax - 1 - x, i],
                        rightCols[y, x, i],
                        quicker ? x * 1.0f / samplesToLerp : 0.5f + 0.5f * x / samplesToLerp
                    );
                }
                NormSplatWeights( output, y, x );
            }
        }
    }

    private void LerpColsRightOntoLeft( float[,,] rightCols, float[,,] leftCols, float[,,] output, int samplesToLerp, bool quicker = false )
    {
        for( int x = 0; x < samplesToLerp; x++ )
        {
            // ignore diagonals at corners
            for( int y = x; y < yMax - x; y++ )
            {
                for( int i = 0; i < iMax; i++ )
                {
                    output[y, xMax - 1 - x, i] = Mathf.SmoothStep(
                        rightCols[y, x, i],
                        leftCols[y, xMax - 1 - x, i],
                        quicker ? x * 1.0f / samplesToLerp : 0.5f + 0.5f * x / samplesToLerp
                    );
                }
                NormSplatWeights( output, y, x );
            }
        }
    }

    private void LerpRowsBottomOntoTop( float[,,] bottomRows, float[,,] topRows, float[,,] output, int samplesToLerp, bool quicker = false )
    {
        for( int y = 0; y < samplesToLerp; y++ )
        {
            // ignore diagonals at corners
            for( int x = y; x < xMax - y; x++ )
            {
                for( int i = 0; i < iMax; i++ )
                {
                    output[y, x, i] = Mathf.SmoothStep(
                        bottomRows[yMax - 1 - y, x, i],
                        topRows[y, x, i],
                        quicker ? y * 1.0f / samplesToLerp : 0.5f + 0.5f * y / samplesToLerp
                    );
                }
                NormSplatWeights( output, y, x );
            }
        }
    }

    private void LerpRowsTopOntoBottom( float[,,] topRows, float[,,] bottomRows, float[,,] output, int samplesToLerp, bool quicker = false )
    {
        for( int y = 0; y < samplesToLerp; y++ )
        {
            // ignore diagonals at corners
            for( int x = y; x < xMax - y; x++ )
            {
                for( int i = 0; i < iMax; i++ )
                {
                    output[yMax - 1 - y, x, i] = Mathf.SmoothStep(
                        topRows[y, x, i],
                        bottomRows[yMax - 1 - y, x, i],
                        quicker ? y * 1.0f / samplesToLerp : 0.5f + 0.5f * y / samplesToLerp
                    );
                }
                NormSplatWeights( output, y, x );
            }
        }
    }

    private void LerpCorner( float[,,] topLeft, float[,,] topRight, float[,,] bottomLeft, float[,,] bottomRight, int samplesToLerp )
    {
        float[] averages = new float[topLeft.GetLength( 2 )];
        for( int i = 0; i < averages.Length; i++ )
        {
            for( int x = 0; x < samplesToLerp; x++ )
            {
                for( int y = 0; y < samplesToLerp; y++ )
                {
                    averages[i] += topLeft[y, xMax - 1 - x, i];
                    averages[i] += topRight[y, x, i];
                    averages[i] += bottomLeft[yMax - 1 - y, xMax - 1 - x, i];
                    averages[i] += bottomRight[yMax - 1 - y, x, i];
                }
            }
        }
        NormSplatVector( averages );


        for( int x = 0; x < samplesToLerp; x++ )
        {
            for( int y = 0; y < samplesToLerp; y++ )
            {
                for( int i = 0; i < averages.Length; i++ )
                {
                    // float lerpAmount = ( x + y ) * 1.0f / ( 2.0f * samplesToLerp - 2 );
                    float lerpAmount = Mathf.Min( ( x + y ) * 1.0f / ( samplesToLerp - 1 ), 1 );

                    topLeft[y, xMax - 1 - x, i] = Mathf.SmoothStep(
                        averages[i],
                        topLeft[y, xMax - 1 - x, i],
                        lerpAmount
                    );

                    topRight[y, x, i] = Mathf.SmoothStep(
                        averages[i],
                        topRight[y, x, i],
                        lerpAmount
                    );

                    bottomLeft[yMax - 1 - y, xMax - 1 - x, i] = Mathf.SmoothStep(
                        averages[i],
                        bottomLeft[yMax - 1 - y, xMax - 1 - x, i],
                        lerpAmount
                    );

                    bottomRight[yMax - 1 - y, x, i] = Mathf.SmoothStep(
                        averages[i],
                        bottomRight[yMax - 1 - y, x, i],
                        lerpAmount
                    );
                }
                NormSplatWeights( topLeft, y, x );
                NormSplatWeights( topRight, y, x );
                NormSplatWeights( bottomLeft, y, x );
                NormSplatWeights( bottomRight, y, x );
            }
        }
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
        float normHeight = myTerrainData.GetInterpolatedHeight( normX, normY ) / myTerrainData.heightmapResolution;

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


    public List<SerializableTerrainTextureExample> SerializeExamples()
    {
        List<SerializableTerrainTextureExample> examples = new List<SerializableTerrainTextureExample>();

        foreach( TerrainTextureExample example in myRegressionExamples )
        {
            examples.Add( example.Serialize( transform ) );
        }

        return examples;
    }

    public void LoadExamples( List<SerializableTerrainTextureExample> examples )
    {
        for( int i = 0; i < examples.Count; i++ )
        {
            TerrainTextureExample newExample;
            if( isPrefabNetworked )
            {
                newExample = PhotonNetwork.Instantiate( terrainExamplePrefab.name, Vector3.zero, Quaternion.identity )
                    .GetComponent<TerrainTextureExample>();
            }
            else
            {
                newExample = Instantiate( terrainExamplePrefab );
            }
            newExample.ResetFromSerial( examples[i], transform );
            // initialize
            newExample.ManuallySpecifyTerrain( this );
            // don't retrain until end
            ProvideExample( newExample, false );
        }
    }
}


[System.Serializable]
public class SerializableTerrainTextureTrainingExamples
{
    public List< SerializableTerrainTextureExample > examples;
}
