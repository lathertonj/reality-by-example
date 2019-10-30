using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundEngineTempoRegressor : MonoBehaviour
{
    public Transform objectToRunRegressionOn;
    private SoundEngine mySoundEngine;
    
    // regression
    private RapidMixRegression myRegression;
    private List<SoundTempoExample> myRegressionExamples;
    private bool haveTrained = false;
    private float myDefaultTempo;


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

        // initialize list
        myRegressionExamples = new List<SoundTempoExample>();

        // initialize
        myDefaultTempo = 100f;

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
        }
        else
        {
            // no examples?  no training!
            haveTrained = false;
        }
    }





}
