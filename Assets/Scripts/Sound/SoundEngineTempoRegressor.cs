using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SoundEngineTempoRegressor : MonoBehaviour , ColorablePlaneDataSource , SerializableByExample
{
    public Transform objectToRunRegressionOn;
    private SoundEngine mySoundEngine;
    
    // regression
    private RapidMixRegression myRegression;
    [HideInInspector] public List<SoundTempoExample> myRegressionExamples;
    private bool haveTrained = false;
    private float myDefaultTempo;
    private bool currentlyShowingData = false;
    private static SoundEngineTempoRegressor me;

    public bool displayPlaneVisualization = true;

    public SoundTempoExample examplePrefab;
    public bool isExampleNetworked;


    public void ProvideExample( SoundTempoExample example, bool rescan = true )
    {
        // remember
        myRegressionExamples.Add( example );

        // recompute
        if( rescan )
        {
            RescanProvidedExamples();
        }
    }

    public void ForgetExample( SoundTempoExample example )
    {
        // forget
        if( myRegressionExamples.Remove( example ) )
        {
            // recompute
            RescanProvidedExamples();
        }
    }

    public void RescanProvidedExamples()
    {
        // train and recompute
        TrainRegression();
    }


    // Use this for initialization
    void Awake()
    {
        // grab component reference
        myRegression = gameObject.AddComponent<RapidMixRegression>();
        mySoundEngine = GetComponent<SoundEngine>();

        // initialize list
        myRegressionExamples = new List<SoundTempoExample>();

        // initialize
        myDefaultTempo = 100f;
        me = this;
    }

    static public void Activate()
    {
        me.ActivatePlane();
    }

    private void ActivatePlane()
    {
        if( displayPlaneVisualization )
        {
            // don't use reference data source
            ColorablePlane.SetDataSource( this, 0 );
            currentlyShowingData = true;
        }
    }

    static public void Deactivate()
    {
        // TODO hide the plane -- want to do this, but only when NEITHER of our hands is using the plane...
        ColorablePlane.ClearDataSource( me );

        me.currentlyShowingData = false;
    }

    private double[] InputVector( Vector3 position )
    {
        return SoundEngineFeatures.InputVector( position );
    }

    void Start()
    {
        StartCoroutine( UpdateTempo() );
    }


    // Update is called once per frame
    IEnumerator UpdateTempo()
    {
        while( true )
        {
            float tempo = myDefaultTempo;
            if( haveTrained )
            {
                tempo = (float) myRegression.Run( SoundEngineFeatures.InputVector( objectToRunRegressionOn.position ) )[0];
            }
            // update sound engine
            mySoundEngine.SetQuarterNoteTime( TempoBPMToQuarterNoteSeconds( tempo ) );

            yield return new WaitForSecondsRealtime( 0.1f );
        }
    }

    private float TempoBPMToQuarterNoteSeconds( float bpm )
    {
        bpm = Mathf.Clamp( bpm, SoundTempoExample.minTempo, SoundTempoExample.maxTempo );
        // (60 seconds / 1 minute) * (1 minute / X beats) == units of seconds / beats
        return 60.0f / bpm;
    }


    private void TrainRegression()
    {
        // only do this when we have examples
        if( myRegressionExamples.Count > 0 )
        {
            // reset the regression
            myRegression.ResetRegression();

            // rerecord all points
            foreach( SoundTempoExample example in myRegressionExamples )
            {
                // world point, NOT local
                Vector3 point = example.transform.position;

                // remember
                myRegression.RecordDataPoint( InputVector( point ), new double[] { example.myTempo } );
            }

            // train
            myRegression.Train();

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

    float ColorablePlaneDataSource.Intensity0To1( Vector3 worldPos, float referenceData )
    {
        if( !haveTrained ) { return 0; }
        // don't use reference data
        return ((float) myRegression.Run( SoundEngineFeatures.InputVector( worldPos ) )[0])
            .MapClamp( SoundTempoExample.minTempo, SoundTempoExample.maxTempo, 0, 1 ); 
    }

    string SerializableByExample.SerializeExamples()
    {
        SerializableTempoExamples examples = new SerializableTempoExamples();
        examples.examples = new List<SerializableTempoExample>();

        foreach( SoundTempoExample example in myRegressionExamples )
        {
            examples.examples.Add( example.Serialize() );
        }

        // convert to json
        return SerializationManager.ConvertToJSON<SerializableTempoExamples>( examples );
    }

    IEnumerator SerializableByExample.LoadExamples( string serializedExamples )
    {
        SerializableTempoExamples examples = 
            SerializationManager.ConvertFromJSON<SerializableTempoExamples>( serializedExamples );
        
        // height
        for( int i = 0; i < examples.examples.Count; i++ )
        {
            SoundTempoExample newExample; 
            if( isExampleNetworked )
            {
                newExample = PhotonNetwork.Instantiate( examplePrefab.name, Vector3.zero, Quaternion.identity )
                    .GetComponent<SoundTempoExample>();
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
        return "tempo";
    }
}

[System.Serializable]
public class SerializableTempoExamples
{
    public List< SerializableTempoExample > examples;
}