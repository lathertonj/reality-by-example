using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SoundEngineChordClassifier : MonoBehaviour , ColorablePlaneDataSource , SerializableByExample
{
    public Transform objectToRunRegressionOn;
    private SoundEngine mySoundEngine;
    
    // regression
    private RapidMixClassifier myClassifier;
    [HideInInspector] public List<SoundChordExample> myClassifierExamples;
    private bool haveTrained = false;
    private int myDefaultChord = 0;
    private bool currentlyShowingData = false;

    private static SoundEngineChordClassifier me;

    public bool displayPlaneVisualization = true;

    public SoundChordExample examplePrefab;
    public bool isExampleNetworked;



    public void ProvideExample( SoundChordExample example, bool rescan = true )
    {
        // remember
        myClassifierExamples.Add( example );

        // recompute
        if( rescan )
        {
            RescanProvidedExamples();
        }
    }

    public void ForgetExample( SoundChordExample example )
    {
        // forget
        if( myClassifierExamples.Remove( example ) )
        {
            // recompute
            RescanProvidedExamples();
        }
    }

    public void RescanProvidedExamples()
    {
        // train and recompute
        TrainClassifier();
    }


    // Use this for initialization
    void Awake()
    {
        // grab component reference
        myClassifier = gameObject.AddComponent<RapidMixClassifier>();
        mySoundEngine = GetComponent<SoundEngine>();
        me = this;

        // initialize list
        myClassifierExamples = new List<SoundChordExample>();

        // initialize
        myDefaultChord = 0;
    }

    static public void Activate()
    {
        me.ActivatePlane();
    }

    private void ActivatePlane()
    {
        if( displayPlaneVisualization )
        {
            // don't use reference data
            ColorablePlane.SetDataSource( this, 0 );
            currentlyShowingData = true;
        }
    }

    public static void Deactivate()
    {
        ColorablePlane.ClearDataSource( me );

        me.currentlyShowingData = false;
    }

    private double[] InputVector( Vector3 position )
    {
        return SoundEngineFeatures.InputVector( position );
    }

    void Start()
    {
        StartCoroutine( UpdateChords() );
    }


    // Update is called once per frame
    IEnumerator UpdateChords()
    {
        while( true )
        {
            int chord = myDefaultChord;
            if( haveTrained )
            {
                chord = RunClassifier( objectToRunRegressionOn.position );
            }
            // update the sound engine
            mySoundEngine.SetChord( chord );

            yield return new WaitForSecondsRealtime( 0.1f );
        }
    }

    private void TrainClassifier()
    {
        // only do this when we have examples
        if( myClassifierExamples.Count > 0 )
        {
            // reset the regression
            myClassifier.ResetClassifier();

            // rerecord all points
            foreach( SoundChordExample example in myClassifierExamples )
            {
                // world point, NOT local
                Vector3 point = example.transform.position;

                // remember
                #if UNITY_WEBGL
                myClassifier.RecordDataPoint( InputVector( point ), example.myChord );
                #else
                myClassifier.RecordDataPoint( InputVector( point ), example.myChord.ToString() );
                #endif
            }

            // train
            myClassifier.Train();

            // remember
            haveTrained = true;

            // display
            if( currentlyShowingData ) { ColorablePlane.UpdateColors(); }
        }
        else
        {
            // no examples?  no training!
            haveTrained = false;
        }
    }

    private int RunClassifier( Vector3 pos )
    {
        #if UNITY_WEBGL
        return myClassifier.Run( SoundEngineFeatures.InputVector( pos ) );
        #else
        return int.Parse( myClassifier.Run( SoundEngineFeatures.InputVector( pos ) ) );
        #endif
    }

    float ColorablePlaneDataSource.Intensity0To1( Vector3 worldPos, float referenceData )
    {
        if( !haveTrained ) { return 0; }
        // ignore referenceData
        return RunClassifier( worldPos ) * 1.0f / SoundChordExample.numChords;
    }

    string SerializableByExample.SerializeExamples()
    {
        SerializableChordExamples examples = new SerializableChordExamples();
        examples.examples = new List<SerializableChordExample>();

        foreach( SoundChordExample example in myClassifierExamples )
        {
            examples.examples.Add( example.Serialize() );
        }

        // convert to json
        return SerializationManager.ConvertToJSON<SerializableChordExamples>( examples );
    }

    IEnumerator SerializableByExample.LoadExamples( string serializedExamples )
    {
        SerializableChordExamples examples = 
            SerializationManager.ConvertFromJSON<SerializableChordExamples>( serializedExamples );
        
        // height
        for( int i = 0; i < examples.examples.Count; i++ )
        {
            SoundChordExample newExample; 
            if( isExampleNetworked )
            {
                newExample = PhotonNetwork.Instantiate( examplePrefab.name, Vector3.zero, Quaternion.identity )
                    .GetComponent<SoundChordExample>();
            }
            else
            {
                newExample = Instantiate( examplePrefab );
            }
            newExample.ResetFromSerial( examples.examples[i] );
            newExample.Initialize( false );
        }

        RescanProvidedExamples();
        yield break;
    }

    string SerializableByExample.FilenameIdentifier()
    {
        return "chord";
    }
}

[System.Serializable]
public class SerializableChordExamples
{
    public List< SerializableChordExample > examples;
}
