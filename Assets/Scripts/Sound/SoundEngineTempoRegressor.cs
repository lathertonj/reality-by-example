using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundEngineTempoRegressor : MonoBehaviour , ColorablePlaneDataSource
{
    public Transform objectToRunRegressionOn;
    private SoundEngine mySoundEngine;
    
    // regression
    private RapidMixRegression myRegression;
    private List<SoundTempoExample> myRegressionExamples;
    private bool haveTrained = false;
    private float myDefaultTempo;
    private ColorablePlane myColorablePlane;
    private Vector3 previousPosition;

    // TODO: it seems like the 0th example is ignored once the 1st example is placed


    public void ProvideExample( SoundTempoExample example )
    {
        // remember
        myRegressionExamples.Add( example );

        // recompute
        RescanProvidedExamples();
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
        myColorablePlane = GetComponentInChildren<ColorablePlane>();

        // TODO: activate only when we are in this mode
        Activate();

        // initialize list
        myRegressionExamples = new List<SoundTempoExample>();

        // initialize
        myDefaultTempo = 100f;
        previousPosition = transform.position;
    }

    public void Activate()
    {
        myColorablePlane.gameObject.SetActive( true );
        myColorablePlane.SetDataSource( this );
    }

    public void Deactivate()
    {
        // TODO: tell the plane to forget about me, and hide it
    }

    private double[] InputVector( Vector3 position )
    {
        return SoundEngineFeatures.InputVector( position );
    }

    void Start()
    {
        // nothing to do on start
    }


    // Update is called once per frame
    void Update()
    {
        float tempo = myDefaultTempo;
        if( haveTrained )
        {
            tempo = (float) myRegression.Run( SoundEngineFeatures.InputVector( objectToRunRegressionOn.position ) )[0];

            if( previousPosition != transform.position )
            {
                previousPosition = transform.position;
                myColorablePlane.UpdateColors();
            }
        }
        // always be updating the sound engine
        mySoundEngine.SetQuarterNoteTime( TempoBPMToQuarterNoteSeconds( tempo ) );
    }

    private float TempoBPMToQuarterNoteSeconds( float bpm )
    {
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
                // world to local point
                Vector3 point = transform.InverseTransformPoint( example.transform.position );

                // remember
                myRegression.RecordDataPoint( InputVector( point ), new double[] { example.myTempo } );
            }

            // train
            myRegression.Train();

            // remember
            haveTrained = true;

            // display
            myColorablePlane.UpdateColors();
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
