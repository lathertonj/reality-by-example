using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundEngine0To1Regressor : MonoBehaviour , ColorablePlaneDataSource
{
    public enum Parameter { Timbre, Density, Volume };
    public Parameter myParameter;

    public Transform objectToRunRegressionOn;
    private SoundEngine mySoundEngine;
    
    // regression
    private RapidMixRegression myRegression;
    private List<Sound0To1Example> myRegressionExamples;
    private bool haveTrained = false;
    private float myDefaultValue;
    private ColorablePlane myColorablePlane;
    private Vector3 previousPosition;
    private bool currentlyShowingData = false;

    public static SoundEngine0To1Regressor timbreRegressor, densityRegressor, volumeRegressor;



    public void ProvideExample( Sound0To1Example example, bool rescan = true )
    {
        // remember
        myRegressionExamples.Add( example );

        // recompute
        if( rescan )
        { 
            RescanProvidedExamples();
        }
    }

    public void ForgetExample( Sound0To1Example example )
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
        
        switch( myParameter )
        {
            case Parameter.Density:
                densityRegressor = this;
                break;
            case Parameter.Timbre:
                timbreRegressor = this;
                break;
            case Parameter.Volume:
                volumeRegressor = this;
                break;
        }

        // initialize list
        myRegressionExamples = new List<Sound0To1Example>();

        // initialize
        myDefaultValue = 0.5f;
        previousPosition = transform.position;
    }

    static public void Activate( SoundEngine0To1Regressor me )
    {
        me.myColorablePlane.gameObject.SetActive( true );
        me.myColorablePlane.SetDataSource( me );
        me.currentlyShowingData = true;
    }

    static public void Deactivate( SoundEngine0To1Regressor me )
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
        StartCoroutine( UpdateValue() );
    }


    // Update is called once per frame
    IEnumerator UpdateValue()
    {
        while( true )
        {
            float value = myDefaultValue;
            if( haveTrained )
            {
                value = RunRegressionClamped( objectToRunRegressionOn.position );

                if( currentlyShowingData && previousPosition != transform.position )
                {
                    previousPosition = transform.position;
                    myColorablePlane.UpdateColors();
                }
            }
            // update the sound engine
            switch( myParameter )
            {
                case Parameter.Density:
                    mySoundEngine.SetDensity( value );
                    break;
                case Parameter.Timbre:
                    mySoundEngine.SetTimbre( value );
                    break;
                case Parameter.Volume:
                    mySoundEngine.SetVolume( value );
                    break;
            }

            yield return new WaitForSecondsRealtime( 0.1f );
        }
    }

    private void TrainRegression()
    {
        // only do this when we have examples
        if( myRegressionExamples.Count > 0 )
        {
            // reset the regression
            myRegression.ResetRegression();

            // rerecord all points
            foreach( Sound0To1Example example in myRegressionExamples )
            {
                // world point, NOT local
                Vector3 point = example.transform.position;

                // remember
                myRegression.RecordDataPoint( InputVector( point ), new double[] { example.myValue } );
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
