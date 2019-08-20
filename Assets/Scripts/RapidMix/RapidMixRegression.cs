using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;


public class RapidMixRegression : MonoBehaviour
{
#if UNITY_WEBGL
    private int myTrainingID, myRegressionID, myInputLength, myOutputLength;
#else
    private System.UInt32 myTrainingID, myRegressionID, myInputLength, myOutputLength;
#endif
    private bool haveTrained = false;

    void Awake()
    {
        #if UNITY_WEBGL
            initializeRapidMix();
        #endif
        myTrainingID = createEmptyTrainingData();
        myRegressionID = createNewStaticRegression();
        myInputLength = 0;
        myOutputLength = 0;
    }

    public void RecordDataPoint( double[] input, double[] output )
    {
        // remember expected input length
        if( myInputLength == 0 )
        {
            #if UNITY_WEBGL
            myInputLength = input.Length;
            #else
            myInputLength = (System.UInt32) input.Length;
            #endif
        }
        // remember expected output length
        if( myOutputLength == 0 )
        {
            #if UNITY_WEBGL
            myOutputLength = output.Length;
            #else
            myOutputLength = (System.UInt32) output.Length;
            #endif
        }

        // show error if we get something that isn't the expected length
        if( myInputLength != input.Length )
        {
            Debug.LogError( string.Format( "Received input of dimension {0} which was different than the expected / originally recieved input dimension {1}", input.Length, myInputLength ) );
        }
        if( myOutputLength != output.Length )
        {
            Debug.LogError( string.Format( "Received output of dimension {0} which was different than the expected / originally recieved output dimension {1}", output.Length, myOutputLength ) );
        }

        recordSingleTrainingElement(
            myTrainingID,
            input, myInputLength,
            output, myOutputLength
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
        if( myInputLength != input.Length )
        {
            Debug.LogError( string.Format( "Received input of dimension {0} which was different than the expected / originally recieved input dimension {1}", input.Length, myInputLength ) );
        }
        double [] output = new double[myOutputLength];
        runStaticRegression(
            myRegressionID,
            input, myInputLength,
            output, myOutputLength
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

#if UNITY_WEBGL
    const string PLUGIN_NAME = "__Internal";
    
    [DllImport( PLUGIN_NAME )]
    private static extern void initializeRapidMix();
    
    [DllImport( PLUGIN_NAME )]
    private static extern int createEmptyTrainingData();

    [DllImport( PLUGIN_NAME )]
    private static extern int createNewStaticRegression();

    [DllImport( PLUGIN_NAME )]
    private static extern bool recordSingleTrainingElement(
        int trainingID,
        double[] input, int n_input,
        double[] output, int n_ouput
    );

    [DllImport( PLUGIN_NAME )]
    private static extern bool trainStaticRegression( int regressionID, int trainingID );

    [DllImport( PLUGIN_NAME )]
    private static extern bool runStaticRegression(
        int regressionID,
        double[] input, int n_input,
        double[] output, int n_output
    );

    [DllImport( PLUGIN_NAME )]
    private static extern bool resetStaticRegression( int regressionID );

    [DllImport( PLUGIN_NAME )]
    private static extern bool cleanupTrainingData( int trainingID );
#else    
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
#endif

    

}
