using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundEngine0To1Regressor : MonoBehaviour , ColorablePlaneDataSource , SerializableByExample
{
    public enum Parameter { Timbre, Density, Volume };
    public Parameter myParameter;

    public Transform objectToRunRegressionOn;
    private SoundEngine mySoundEngine;
    
    // regression
    private RapidMixRegression myRegression;
    [HideInInspector] public List<Sound0To1Example> myRegressionExamples;
    private bool haveTrained = false;
    public float myDefaultValue = 0.5f;
    private ColorablePlane myColorablePlane;
    private Vector3 previousPosition;
    private bool currentlyShowingData = false;

    public static SoundEngine0To1Regressor timbreRegressor, densityRegressor, volumeRegressor;

    public bool displayPlaneVisualization = true;

    public Sound0To1Example examplePrefab;



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
        previousPosition = transform.position;
    }

    static public void Activate( SoundEngine0To1Regressor me )
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

    string SerializableByExample.SerializeExamples()
    {
        Serializable0To1Examples examples = new Serializable0To1Examples();
        examples.examples = new List<Serializable0To1Example>();

        foreach( Sound0To1Example example in myRegressionExamples )
        {
            examples.examples.Add( example.Serialize() );
        }

        // convert to json
        return SerializationManager.ConvertToJSON<Serializable0To1Examples>( examples );
    }

    IEnumerator SerializableByExample.LoadExamples( string serializedExamples )
    {
        Serializable0To1Examples examples = 
            SerializationManager.ConvertFromJSON<Serializable0To1Examples>( serializedExamples );
        
        // height
        for( int i = 0; i < examples.examples.Count; i++ )
        {
            Sound0To1Example newExample = Instantiate( examplePrefab );
            newExample.ResetFromSerial( examples.examples[i] );
            newExample.Initialize( false );
        }

        RescanProvidedExamples();
        yield break;
    }

    string SerializableByExample.FilenameIdentifier()
    {
        switch( myParameter )
        {
            case Parameter.Density:
                return "density";
            case Parameter.Timbre:
                return "timbre";
            case Parameter.Volume:
                return "volume";
            default:
                return "_";
        }
    }
}



[System.Serializable]
public class Serializable0To1Examples
{
    public List< Serializable0To1Example > examples;
}