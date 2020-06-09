using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundEngineChordClassifier : MonoBehaviour , ColorablePlaneDataSource , SerializableByExample
{
    public Transform objectToRunRegressionOn;
    private SoundEngine mySoundEngine;
    
    // regression
    private RapidMixClassifier myClassifier;
    [HideInInspector] public List<SoundChordExample> myClassifierExamples;
    private bool haveTrained = false;
    private int myDefaultChord = 0;
    private ColorablePlane myColorablePlane;
    private Vector3 previousPosition;
    private bool currentlyShowingData = false;

    private static SoundEngineChordClassifier me;

    public bool displayPlaneVisualization = true;

    public SoundChordExample examplePrefab;



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
        myColorablePlane = GetComponentInChildren<ColorablePlane>( true );
        me = this;

        // initialize list
        myClassifierExamples = new List<SoundChordExample>();

        // initialize
        myDefaultChord = 0;
        previousPosition = transform.position;
    }

    static public void Activate()
    {
        me.ActivatePlane();
    }

    private void ActivatePlane()
    {
        if( displayPlaneVisualization )
        {
            myColorablePlane.gameObject.SetActive( true );
            myColorablePlane.SetDataSource( this );
            currentlyShowingData = true;
        }
    }

    static public void Deactivate()
    {
        // TODO hide the plane -- want to do this, but only when NEITHER of our hands is using the plane...
        me.myColorablePlane.gameObject.SetActive( false );

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

                if( currentlyShowingData && previousPosition != transform.position )
                {
                    previousPosition = transform.position;
                    myColorablePlane.UpdateColors();
                }
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
            if( currentlyShowingData ) { myColorablePlane.UpdateColors(); }
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

    public float Intensity0To1( Vector3 worldPos )
    {
        if( !haveTrained ) { return 0; }
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
            SoundChordExample newExample = Instantiate( examplePrefab );
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
