using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundEngineTimbreRegressor : MonoBehaviour , ColorablePlaneDataSource
{
    public Transform objectToRunRegressionOn;
    private SoundEngine mySoundEngine;
    
    // regression
    private RapidMixRegression myRegression;
    private List<SoundTimbreExample> myRegressionExamples;
    private bool haveTrained = false;
    private float myDefaultTimbre;
    private ColorablePlane myColorablePlane;
    private Vector3 previousPosition;
    private bool currentlyShowingData = false;

    private static SoundEngineTimbreRegressor me;


    // TODO: it seems like some examples get forgotten about 
    // until they get modified slightly, then other examples get forgotten about...

    public void ProvideExample( SoundTimbreExample example )
    {
        // remember
        myRegressionExamples.Add( example );

        // recompute
        RescanProvidedExamples();
    }

    public void ForgetExample( SoundTimbreExample example )
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
        me = this;

        // initialize list
        myRegressionExamples = new List<SoundTimbreExample>();

        // initialize
        myDefaultTimbre = 0.5f;
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
        // me.myColorablePlane.gameObject.SetActive( false );

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
        float timbre = myDefaultTimbre;
        if( haveTrained )
        {
            timbre = RunRegressionClamped( objectToRunRegressionOn.position );

            if( currentlyShowingData && previousPosition != transform.position )
            {
                previousPosition = transform.position;
                myColorablePlane.UpdateColors();
            }
        }
        // always be updating the sound engine
        mySoundEngine.SetTimbre( timbre );
    }

    private void TrainRegression()
    {
        // only do this when we have examples
        if( myRegressionExamples.Count > 0 )
        {
            // reset the regression
            myRegression.ResetRegression();

            // rerecord all points
            foreach( SoundTimbreExample example in myRegressionExamples )
            {
                // world to local point
                Vector3 point = transform.InverseTransformPoint( example.transform.position );

                // remember
                myRegression.RecordDataPoint( InputVector( point ), new double[] { example.myTimbre } );
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

    private float RunRegressionClamped( Vector3 pos )
    {
        return Mathf.Clamp01( (float) myRegression.Run( SoundEngineFeatures.InputVector( pos ) )[0]);
    }

    public float Intensity0To1( Vector3 worldPos )
    {
        if( !haveTrained ) { return 0; }
        return RunRegressionClamped( worldPos );
    }
}
