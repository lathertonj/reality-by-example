using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;


public class RapidMixRegression : MonoBehaviour
{
    private System.UInt32 myTrainingID, myRegressionID, myOutputLength;
    private bool haveTrained = false;

    void Awake()
    {
        myTrainingID = createEmptyTrainingData();
        myRegressionID = createNewStaticRegression();
        myOutputLength = 0;
    }

    public void RecordDataPoint( double[] input, double[] output )
    {
        // remember expected output length
        if( myOutputLength == 0 )
        {
            myOutputLength = (System.UInt32) output.Length;
        }

        // show error if we get something that isn't the expected length
        if( myOutputLength != output.Length )
        {
            Debug.LogError( string.Format( "Received output of dimension {0} which was different than the expected / originally recieved output dimension {1}", output.Length, myOutputLength ) );
        }

        recordSingleTrainingElement(
            myTrainingID,
            input, (System.UInt32) input.Length,
            output, (System.UInt32) output.Length
        );
    }

    public void Train()
    {
        trainStaticRegression( myRegressionID, myTrainingID );
        haveTrained = true;
    }

    public double[] Run( double[] input )
    {
        if( !haveTrained )  
        {
            Debug.LogError( "Regression can't Run() without having Train()ed first!" );
            return new double[]{ };
        }
        double [] output = new double[myOutputLength];
        runStaticRegression(
            myRegressionID,
            input, (System.UInt32) input.Length,
            output, (System.UInt32) output.Length
        );
        return output;
    }

    public void ResetRegression()
    {
        resetStaticRegression( myRegressionID );
        haveTrained = false;
        
        // reset data too
        myOutputLength = 0; 
        cleanupTrainingData( myTrainingID );
        myTrainingID = createEmptyTrainingData();
    }

    const string PLUGIN_NAME = "RapidMixAPI";

    [DllImport( PLUGIN_NAME )]
    private static extern System.UInt32 createEmptyTrainingData();

    [DllImport( PLUGIN_NAME )]
    private static extern System.UInt32 createNewStaticRegression();

    [DllImport( PLUGIN_NAME )]
    private static extern bool recordSingleTrainingElement(
        System.UInt32 trainingID,
        double[] input, System.UInt32 n_input,
        double[] output, System.UInt32 n_ouput
    );

    [DllImport( PLUGIN_NAME )]
    private static extern bool trainStaticRegression( System.UInt32 regressionID, System.UInt32 trainingID );

    [DllImport( PLUGIN_NAME )]
    private static extern bool runStaticRegression(
        System.UInt32 regressionID,
        double[] input, System.UInt32 n_input,
        double[] output, System.UInt32 n_output
    );

    [DllImport( PLUGIN_NAME )]
    private static extern bool resetStaticRegression( System.UInt32 regressionID );

    [DllImport( PLUGIN_NAME )]
    private static extern bool cleanupTrainingData( System.UInt32 trainingID );

}
