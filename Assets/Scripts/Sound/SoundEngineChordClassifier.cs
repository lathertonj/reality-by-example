using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundEngineChordClassifier : MonoBehaviour , ColorablePlaneDataSource
{
    public Transform objectToRunRegressionOn;
    private SoundEngine mySoundEngine;
    
    // regression
    private RapidMixClassifier myClassifier;
    private List<SoundChordExample> myClassifierExamples;
    private bool haveTrained = false;
    private int myDefaultChord = 0;
    private ColorablePlane myColorablePlane;
    private Vector3 previousPosition;
    private bool currentlyShowingData = false;

    private static SoundEngineChordClassifier me;



    public void ProvideExample( SoundChordExample example )
    {
        // remember
        myClassifierExamples.Add( example );

        // recompute
        RescanProvidedExamples();
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
        myColorablePlane = GetComponentInChildren<ColorablePlane>();
        me = this;

        // initialize list
        myClassifierExamples = new List<SoundChordExample>();

        // initialize
        myDefaultChord = 0;
        previousPosition = transform.position;
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
        // nothing to do on start
    }


    // Update is called once per frame
    void Update()
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
        // always be updating the sound engine
        // TODO: only update every so often. same with other params.
        mySoundEngine.SetChord( chord );
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
                myClassifier.RecordDataPoint( InputVector( point ), example.myChord.ToString() );
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
        return int.Parse( myClassifier.Run( SoundEngineFeatures.InputVector( pos ) ) );
    }

    public float Intensity0To1( Vector3 worldPos )
    {
        if( !haveTrained ) { return 0; }
        return RunClassifier( worldPos ) * 1.0f / SoundChordExample.numChords;
    }
}
