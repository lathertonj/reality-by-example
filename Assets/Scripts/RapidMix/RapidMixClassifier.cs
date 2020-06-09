using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;


public class RapidMixClassifier : MonoBehaviour
{
    private System.UInt32 myTrainingID, myClassifierID, myInputLength;
    private bool haveTrained = false;
    private StringCallback myRunCallback;

    void Awake()
    {
        myTrainingID = createEmptyTrainingData();
        myClassifierID = createNewStaticClassifier();
        myRunCallback = new StringCallback( GetResult );
    }


    #if UNITY_WEBGL
    public void RecordDataPoint( double[] input, int label )
    {
        // remember expected output length
        if( myInputLength == 0 )
        {
            myInputLength = (System.UInt32) input.Length;
        }

        // show error if we get something that isn't the expected length
        if( myInputLength != input.Length )
        {
            Debug.LogError( string.Format( "Received input of dimension {0} which was different than the expected / originally recieved input dimension {1}", input.Length, myInputLength ) );
        }

        recordSingleLabeledTrainingElement(
            myTrainingID,
            input, (System.UInt32) input.Length,
            label
        );
    }
    #else
    public void RecordDataPoint( double[] input, string label )
    {
        // remember expected output length
        if( myInputLength == 0 )
        {
            myInputLength = (System.UInt32) input.Length;
        }

        // show error if we get something that isn't the expected length
        if( myInputLength != input.Length )
        {
            Debug.LogError( string.Format( "Received input of dimension {0} which was different than the expected / originally recieved input dimension {1}", input.Length, myInputLength ) );
        }

        recordSingleLabeledTrainingElement(
            myTrainingID,
            input, (System.UInt32) input.Length,
            label
        );
    }
    #endif

    public void Train()
    {
        trainStaticClassifier( myClassifierID, myTrainingID );
        haveTrained = true;
    }

    #if UNITY_WEBGL
    public int Run( double[] input )
    {
        if( !haveTrained )  
        {
            Debug.LogError( "Classifier can't Run() without having Train()ed first!" );
            return 0;
        }
        return runStaticClassifier(
            myClassifierID,
            input, (System.UInt32) input.Length
        );
    }
    #else
    public string Run( double[] input )
    {
        if( !haveTrained )  
        {
            Debug.LogError( "Classifier can't Run() without having Train()ed first!" );
            return "unknown";
        }
        runStaticClassifier(
            myClassifierID,
            input, (System.UInt32) input.Length,
            myRunCallback
        );
        return mostRecentResult;
    }
    #endif

    // TODO can the dataset also be reset? need to check C++ API
    public void ResetClassifier()
    {
        resetStaticClassifier( myClassifierID );
        haveTrained = false;
        
        // reset data too
        myInputLength = 0; 
        cleanupTrainingData( myTrainingID );
        myTrainingID = createEmptyTrainingData();
    }

    private string mostRecentResult = "";

    private void GetResult( System.String result )
    {
        mostRecentResult = result;
    }

    [UnmanagedFunctionPointer (CallingConvention.Cdecl)]
	public delegate void StringCallback( System.String str );

#if UNITY_WEBGL
    const string PLUGIN_NAME = "__Internal";
#else    
    const string PLUGIN_NAME = "RapidMixAPI";
#endif

    [DllImport( PLUGIN_NAME )]
    private static extern System.UInt32 createEmptyTrainingData();

    [DllImport( PLUGIN_NAME )]
    private static extern System.UInt32 createNewStaticClassifier();

#if UNITY_WEBGL
    [DllImport( PLUGIN_NAME )]
    private static extern bool recordSingleLabeledTrainingElement(
        System.UInt32 trainingID,
        double[] input, System.UInt32 n_input,
        System.Int32 label
    );
#else
    [DllImport( PLUGIN_NAME )]
    private static extern bool recordSingleLabeledTrainingElement(
        System.UInt32 trainingID,
        double[] input, System.UInt32 n_input,
        System.String label
    );
#endif

    [DllImport( PLUGIN_NAME )]
    private static extern bool trainStaticClassifier( System.UInt32 classifierID, System.UInt32 trainingID );

#if UNITY_WEBGL
    [DllImport( PLUGIN_NAME )]
    private static extern System.Int32 runStaticClassifier(
        System.UInt32 classifierID,
        double[] input, System.UInt32 n_input
    );
#else
    [DllImport( PLUGIN_NAME )]
    private static extern bool runStaticClassifier(
        System.UInt32 classifierID,
        double[] input, System.UInt32 n_input,
        StringCallback callback
    );
#endif

    [DllImport( PLUGIN_NAME )]
    private static extern bool resetStaticClassifier( System.UInt32 classifierID );

    [DllImport( PLUGIN_NAME )]
    private static extern bool cleanupTrainingData( System.UInt32 trainingID );

}
