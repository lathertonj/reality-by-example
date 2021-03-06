using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class CommunicateSynthMapping : MonoBehaviour
{
    // TODO: make photon-ready
    //public CommunicateSynth synthPrefab;
    //private static CommunicateSynth myCommunicator;
    public CommunicateSynth myCommunicator;

    public SteamVR_Action_Boolean turnOnCommunicator;
    private SteamVR_Input_Sources handType;
    private SteamVR_Behaviour_Pose pose;

    public enum Mode { RecordExample, PlaybackExamples, ManualMapping1, ManualMapping2 };
    private Mode myMode = Mode.ManualMapping1;
    private bool amOn = false;

    private static RapidMixRegression communicatorRegression;
    private static List< double[] > communicatorRegressionInputs, communicatorRegressionOutputs;
    private bool haveTrained = false;

    public float recordingTime = 0.1f;
    private Coroutine recordingCoroutine = null;
    public Transform head;
    
    void Awake()
    {
        pose = GetComponent<SteamVR_Behaviour_Pose>();
        handType = pose.inputSource;

        // add to CommunicateSynth so that only my copy of it has a regression on it, not the network's
        if( communicatorRegression == null )
        {
            communicatorRegression = myCommunicator.gameObject.AddComponent<RapidMixRegression>();
            communicatorRegressionInputs = new List<double[]>();
            communicatorRegressionOutputs = new List<double[]>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if( myMode != Mode.RecordExample )
        {
            if( turnOnCommunicator.GetStateDown( handType ) ) 
            {
                myCommunicator.TurnOn();
                amOn = true;
            }
            if( turnOnCommunicator.GetStateUp( handType ) )
            {
                myCommunicator.TurnOff();
                amOn = false;
            }
        }
        else
        {
            if( turnOnCommunicator.GetStateDown( handType ) ) 
            {
                // record
                recordingCoroutine = StartCoroutine( RecordExamples() );
            }
            if( turnOnCommunicator.GetStateUp( handType ) )
            {
                // stop recording and train
                StopCoroutine( recordingCoroutine );
                recordingCoroutine = null;
                Train();
            }
        }

        if( amOn )
        {
            float volume = 0, pitch = 100, timbre = 0;
            Vector3 euler = GetModifiedEulerAngles();
            switch( myMode )
            {
                case Mode.ManualMapping1:
                    // speed = volume
                    volume = pose.GetVelocity().magnitude.PowMapClamp( 0, 4, 0, 1, 0.5f );
                    // euler x = pitch
                    pitch = euler.x.PowMapClamp( -80, 80, 1200, 100, 0.5f );
                    // euler z = timbre
                    timbre = euler.z.PowMapClamp( -80, 80, 0, 1, 1.5f );
                    break;
                case Mode.ManualMapping2:
                    // speed = timbre
                    timbre = pose.GetVelocity().magnitude.PowMapClamp( 0, 4, 0, 1, 0.5f );
                    // constant volume
                    volume = 1;
                    // euler z = pitch
                    pitch = euler.z.PowMapClamp( -80, 80, 1200, 100, 0.5f );
                    break;
                case Mode.PlaybackExamples:
                    volume = 0;
                    if( haveTrained )
                    {
                        double[] output = communicatorRegression.Run( FilterInput( InputVector() ) );
                        // clamp the pitch to 50-4000 Hz
                        pitch = Mathf.Clamp( (float) output[0], 50, 4000 );
                        // double the volume and clamp to 0, 1
                        volume = ((float) output[1]).MapClamp( 0, 0.5f, 0, 1 );
                        // map clamp spectral centroid into 0, 1 (not sure if this is right exponent)
                        timbre = ((float) output[2]).PowMapClamp( 50, 5000, 0, 1, 0.5f );
                        
                    }
                    break; 
                default:
                    // whatever it is, turn it off
                    volume = 0;
                    break;
            }
            myCommunicator.SetPitch( pitch );
            myCommunicator.SetTimbre( timbre );
            myCommunicator.SetAmplitude( volume );
        }
    }

    public void SetMode( Mode m )
    {
        myMode = m;
        switch( myMode )
        {
            // specific handling here?
            default:
                break;
        }
    }


    private double[] InputVector()
    {
        Vector3 velocity = pose.GetVelocity();
        Vector3 euler = GetModifiedEulerAngles();
        Vector3 position = GetLocalPosition();
        return new double[] {
            velocity.x,
            velocity.y,
            velocity.z,
            velocity.magnitude,
            euler.x,
            euler.y,
            euler.z,
            position.x,
            position.y,
            position.z,
            position.magnitude
        };
    }

    // TODO: be able to customize inputs
    private double[] FilterInput( double[] i )
    {
        return i;      
    }

    // TODO: be able to customize outputs? can do this in the runtime
    private double[] OutputVector()
    {
        float pitch, volume, centroid;
        GetAudioData.GetData( out pitch, out volume, out centroid, smoothCentroid: true );
        return new double[] {
            pitch,
            volume,
            centroid
        };
    }

    private void DebugOutput( double[] output )
    {
        Debug.Log( output[0] + " " + output[1] + " " + output[2] );
    }

    private IEnumerator RecordExamples()
    {
        while( true )
        {
            yield return new WaitForSecondsRealtime( recordingTime );
            communicatorRegressionInputs.Add( InputVector() );
            double[] o = OutputVector();
            communicatorRegressionOutputs.Add( o );
            DebugOutput( o );
            
        }
    }

    private void Train()
    {
        if( communicatorRegressionInputs.Count > 0 )
        {
            // reset it
            communicatorRegression.ResetRegression();

            // add examples
            for( int i = 0; i < communicatorRegressionInputs.Count; i++ )
            {
                communicatorRegression.RecordDataPoint( FilterInput( communicatorRegressionInputs[i] ), communicatorRegressionOutputs[i] );
            }

            // and train!
            communicatorRegression.Train();

            // remember for playback
            haveTrained = true;
        }
        else
        {
            // oops, nothing to train on!
            haveTrained = false;
        }
    }

    public void ResetExamples()
    {
        communicatorRegressionInputs.Clear();
        communicatorRegressionOutputs.Clear();
    }

    private Vector3 GetModifiedEulerAngles()
    {
        Vector3 euler = transform.localEulerAngles;
        if( euler.x >= 180 ) { euler.x -= 360; }
        if( euler.y >= 180 ) { euler.y -= 360; }
        if( euler.z >= 180 ) { euler.z -= 360; }
        return euler;
    }

    private Vector3 GetLocalPosition()
    {
        // specifically, local to the reference point of the head
        return transform.position - head.position;
    }
}
