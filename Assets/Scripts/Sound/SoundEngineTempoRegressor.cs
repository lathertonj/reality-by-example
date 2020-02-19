using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundEngineTempoRegressor : MonoBehaviour , ColorablePlaneDataSource
{
    public Transform objectToRunRegressionOn;
    private SoundEngine mySoundEngine;
    
    // regression
    private RapidMixRegression myRegression;
    [HideInInspector] public List<SoundTempoExample> myRegressionExamples;
    private bool haveTrained = false;
    private float myDefaultTempo;
    private ColorablePlane myColorablePlane;
    private Vector3 previousPosition;
    private bool currentlyShowingData = false;
    private static SoundEngineTempoRegressor me;


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
        myColorablePlane = GetComponentInChildren<ColorablePlane>( true );

        // initialize list
        myRegressionExamples = new List<SoundTempoExample>();

        // initialize
        myDefaultTempo = 100f;
        previousPosition = transform.position;

        me = this;
    }

    static public void Activate()
    {
        me.myColorablePlane.gameObject.SetActive( true );
        me.myColorablePlane.SetDataSource( me );
        me.currentlyShowingData = true;
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

                if( currentlyShowingData && previousPosition != transform.position )
                {
                    previousPosition = transform.position;
                    myColorablePlane.UpdateColors();
                }
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
            if( currentlyShowingData ) { myColorablePlane.UpdateColors(); }
        }
        else
        {
            // no examples?  no training!
            haveTrained = false;
        }
    }

    public float Intensity0To1( Vector3 worldPos )
    {
        if( !haveTrained ) { return 0; }
        return ((float) myRegression.Run( SoundEngineFeatures.InputVector( worldPos ) )[0])
            .MapClamp( SoundTempoExample.minTempo, SoundTempoExample.maxTempo, 0, 1 ); 
    }
}
