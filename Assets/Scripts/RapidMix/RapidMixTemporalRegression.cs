using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;


public class RapidMixTemporalRegression : MonoBehaviour
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
        myRegressionID = createNewTemporalRegression();
        myInputLength = 0;
        myOutputLength = 0;
    }

    public void RecordDataPhrase( List< double[] > inputs, List< double[] > outputs )
    {
        Debug.Log( "Recording data phrase!" );
        // error checking
        if( inputs.Count == 0 || outputs.Count == 0 )
        {
            Debug.LogError( "Can't record data phrase with no inputs or outputs." );
            return;
        }
        
        if( inputs.Count != outputs.Count )
        {
            Debug.LogError( "Can't record data phrase without the same number of inputs and outputs." );
            return;
        }

        // remember expected input length
        if( myInputLength == 0 )
        {
            #if UNITY_WEBGL
            myInputLength = inputs[0].Length;
            #else
            myInputLength = (System.UInt32) inputs[0].Length;
            #endif
        }
        // remember expected output length
        if( myOutputLength == 0 )
        {
            #if UNITY_WEBGL
            myOutputLength = outputs[0].Length;
            #else
            myOutputLength = (System.UInt32) outputs[0].Length;
            #endif
        }

        // show error if we get something that isn't the expected length
        if( myInputLength != inputs[0].Length )
        {
            Debug.LogError( string.Format( "Received input of dimension {0} which was different than the expected / originally recieved input dimension {1}", inputs[0].Length, myInputLength ) );
        }
        if( myOutputLength != outputs[0].Length )
        {
            Debug.LogError( string.Format( "Received output of dimension {0} which was different than the expected / originally recieved output dimension {1}", outputs[0].Length, myOutputLength ) );
        }

        // record the phrase
        startPhrase( myTrainingID );

        // ... one element at a time
        for( int i = 0; i < inputs.Count; i++ )
        {
            recordPhraseElement(
                myTrainingID,
                inputs[i], myInputLength,
                outputs[i], myOutputLength
            );
        }

        // finish (this does nothing in current implementation)
        stopPhrase( myTrainingID );
        Debug.Log( "finished recording data phrase" );
        Debug.Log( "number of elements was " + inputs.Count.ToString() );
    }

    public void Train()
    {
        Debug.Log( "training TRALALA!" ); 
        trainTemporalRegression( myRegressionID, myTrainingID );
        haveTrained = true;
        Debug.Log( "finished training WOO!" );
    }

    public double[] Run( double[] input )
    {
        Debug.Log( "Tryna RUUUN" );
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
        runTemporalRegression(
            myRegressionID,
            input, myInputLength,
            output, myOutputLength
        );
        Debug.Log( "finished running~!" );
        return output;
    }

    public void ResetRegression()
    {
        resetTemporalRegression( myRegressionID );
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
    private static extern int createNewTemporalRegression();

    [DllImport( PLUGIN_NAME )]
    private static extern bool startPhrase( System.UInt32 trainingID );

    [DllImport( PLUGIN_NAME )]
    private static extern bool recordPhraseElement(
        System.UInt32 trainingID,
        double[] input, System.UInt32 n_input,
        double[] output, System.UInt32 n_ouput
    );

    [DllImport( PLUGIN_NAME )]
    private static extern bool endPhrase( System.UInt32 trainingID );

    [DllImport( PLUGIN_NAME )]
    private static extern bool trainTemporalRegression( int regressionID, int trainingID );

    [DllImport( PLUGIN_NAME )]
    private static extern bool runTemporalRegression(
        int regressionID,
        double[] input, int n_input,
        double[] output, int n_output
    );

    [DllImport( PLUGIN_NAME )]
    private static extern bool resetTemporalRegression( int regressionID );

    [DllImport( PLUGIN_NAME )]
    private static extern bool cleanupTrainingData( int trainingID );
#else    
    const string PLUGIN_NAME = "RapidMixAPI";
    [DllImport( PLUGIN_NAME )]
    private static extern System.UInt32 createEmptyTrainingData();

    [DllImport( PLUGIN_NAME )]
    private static extern System.UInt32 createNewTemporalRegression();

    [DllImport( PLUGIN_NAME )]
    private static extern bool startPhrase( System.UInt32 trainingID );

    [DllImport( PLUGIN_NAME )]
    private static extern bool recordPhraseElement(
        System.UInt32 trainingID,
        double[] input, System.UInt32 n_input,
        double[] output, System.UInt32 n_ouput
    );

    [DllImport( PLUGIN_NAME )]
    private static extern bool stopPhrase( System.UInt32 trainingID );

    [DllImport( PLUGIN_NAME )]
    private static extern bool trainTemporalRegression( System.UInt32 regressionID, System.UInt32 trainingID );

    [DllImport( PLUGIN_NAME )]
    private static extern bool runTemporalRegression(
        System.UInt32 regressionID,
        double[] input, System.UInt32 n_input,
        double[] output, System.UInt32 n_output
    );

    [DllImport( PLUGIN_NAME )]
    private static extern bool resetTemporalRegression( System.UInt32 regressionID );

    [DllImport( PLUGIN_NAME )]
    private static extern bool cleanupTrainingData( System.UInt32 trainingID );
#endif

    

}
