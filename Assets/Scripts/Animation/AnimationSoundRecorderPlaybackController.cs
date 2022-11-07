using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

#if UNITY_WEBGL
using CK_INT = System.Int32;
using CK_UINT = System.UInt32;
#else
using CK_INT = System.Int64;
using CK_UINT = System.UInt64;
#endif
using CK_FLOAT = System.Double;

public class AnimationSoundRecorderPlaybackController : MonoBehaviour
{
    // predict position in the sound file based on terrain input features
    // only do this once every animation frame, and then just randomly trigger grains around this position
    // until we receive the next update.

    // but, I anticipate this mapping will be very unsatisfying or confusing...

    // give US the input features
    // we will record what is the offset during that time


    // TODO: make a simpler version where randomly we pick between different phrases according to the prominence of the animation...
    
    private RapidMixRegression myRegression;

    private AudioSource myAudioSource;
    private ChuckSubInstance myChuck;
    private ChuckIntSyncer myCurrentRecordedSampleSyncer;
    private ChuckEventListener myTempoListener;

    private CK_FLOAT[] myAudioData;

    string myLisa, myCurrentRecordedSample = "", myNewSamplePositionReady, myNewSamplePosition, myStartRecording, myStopRecording;
    string mySamples = "";
    string mySerialInitEvent, myDisableEvent, myEnableEvent;
    string updateMyLisa;

    private bool haveTrained = false;

    List<AnimationAudioExample> myRegressionData = new List<AnimationAudioExample>();
    private bool wereWeCloned = false;

    public void CloneFrom( AnimationSoundRecorderPlaybackController other, bool shareSamples )
    {
        // flag for later
        wereWeCloned = true;

        // copy regression data
        myRegressionData = other.myRegressionData;
        RecordInAllExamples();
        Train();

        // copy chuck lisa samples
        if( shareSamples ) 
        { 
            mySamples = other.mySamples;
            myCurrentRecordedSample = other.myCurrentRecordedSample;
        }

        // TODO: consider refactoring to use InitFromSerial
        InitChuckVariableNames();
        myChuck.RunCode( string.Format( @"
            global LiSa {0}, {1};
            {1}.duration() => {0}.duration;
            global int {2};
            global float {3}[];
            global Event {4};


            // copy samples
            for( int i; i < {2}; i++ )
            {{
                {0}.valueAt( {3}[i], i::samp );
                // wait a little? this might cause problems below
                if( i % 100 == 0 ) {{ 1::ms => now; }}
            }}
            // signal to self below that all the samples are in place
            {4}.broadcast();

        ", myLisa, other.myLisa, other.myCurrentRecordedSample, other.mySamples, updateMyLisa ) );
    } 

    private bool variableNamesInit = false;
    void InitChuckVariableNames()
    {
        if( variableNamesInit ) { return; }

        myChuck = GetComponent<ChuckSubInstance>();
        myLisa = myChuck.GetUniqueVariableName( "lisa" );
        if( myCurrentRecordedSample == "" ) { myCurrentRecordedSample = myChuck.GetUniqueVariableName( "currentRecordedSample" ); }
        myNewSamplePositionReady = myChuck.GetUniqueVariableName( "newSamplePositionReady" );
        myNewSamplePosition = myChuck.GetUniqueVariableName( "newSamplePosition" );
        myStartRecording = myChuck.GetUniqueVariableName( "startRecording" );
        myStopRecording = myChuck.GetUniqueVariableName( "stopRecording" );
        if( mySamples == "" ) { mySamples = myChuck.GetUniqueVariableName( "mySamples" ); }
        updateMyLisa = myChuck.GetUniqueVariableName( "updateMyLisa" );
        mySerialInitEvent = myChuck.GetUniqueVariableName( "serialInitFinished" );
        myDisableEvent = myChuck.GetUniqueVariableName( "disableMySound" );
        myEnableEvent = myChuck.GetUniqueVariableName( "enableMySound" );
        variableNamesInit = true;
    }

    void Awake()
    {
        myRegression = gameObject.AddComponent<RapidMixRegression>();
        myTempoListener = gameObject.AddComponent<ChuckEventListener>();
        myAudioSource = GetComponent<AudioSource>();
    }

    void InitFromSerial( CK_FLOAT[] samples, int nextAudioFrame )
    {
        InitChuckVariableNames();

        myChuck.RunCode( string.Format( @"
            global float {0}[0];
            global int {1};
            global Event {2};

            // wait until finished
            while( {1} == 0 )
            {{
                1::samp => now;
            }}
            {2}.broadcast();
            
        ", mySamples, myCurrentRecordedSample, mySerialInitEvent ) );

        #if UNITY_WEBGL
            myChuck.ListenForChuckEventOnce( mySerialInitEvent, gameObject.name, "RespondToChuckSerialInit" );
        #else
            myChuck.ListenForChuckEventOnce( mySerialInitEvent, RespondToChuckSerialInit );
        #endif
        myChuck.SetFloatArray( mySamples, samples );
        myChuck.SetInt( myCurrentRecordedSample, nextAudioFrame );
    }

    bool serialInitSuccessful = false;
    void RespondToChuckSerialInit()
    {
        serialInitSuccessful = true;
    }

    void Start()
    {
        InitChuckVariableNames();
        string clonedAddition = wereWeCloned ? @"true => synth.shouldPlayGrains;" : "";
        myChuck.RunCode( string.Format( @"
            global LiSa {0};
            global float {7}[];
            if( {7} == null )
            {{
                float blankValues[ (10::second/samp) $ int ];
                blankValues @=> {7};
            }}
            global int {1};
            global Event {8};
            class AnimationSynth extends Chubgraph
			{{
                {0} => outlet;
				
				// spawn rate: how often a new grain is spawned (ms)
				25 =>  float grainSpawnRateMS;
				0 =>  float grainSpawnRateVariationMS;
				
				// position: where in the file is a grain (0 to 1)
				0 => int grainPosition;
				100::ms / 1::samp => float grainPositionRandomness;
				
				// grain length: how long is a grain (ms)
				300 =>  float grainLengthMS;
				10 =>  float grainLengthRandomnessMS;
				
				// grain rate: how quickly is the grain scanning through the file
				1.004 =>  float grainRate; // 1.002 == in-tune Ab
				0.015 =>  float grainRateRandomness;
				
				// ramp up/down: how quickly we ramp up / down
				50 =>  float rampUpMS;
				200 =>  float rampDownMS;
				
				
				fun float gain( float g )
				{{
					g => {0}.gain;
					return g;
				}}
				
				fun float gain()
				{{
					return {0}.gain();
				}}
				
				
				if( {0}.duration() != 5::minute )
                {{
				    5::minute => {0}.duration;
                }}

                int myPersonalSamplesAdded;
                fun void AddSample( float s )
                {{
                    {0}.valueAt( s, {1}::samp );
                    // potentially resize
                    if( {1} >= {7}.size() )
                    {{
                        ( {7}.size() * 1.6 ) $ int => {7}.size;
                    }}

                    // store in global variable too
                    s => {7}[{1}];
                    {1}++;
                    {1} => myPersonalSamplesAdded;
                }}

                
                fun void UpdateSamples()
                {{
                    while( true )
                    {{
                        //<<< ""trying to update my samples"", {7}.size() >>>;
                        if( {7}.size() == 0 )
                        {{
                            // we didn't receive it in time! wait a little longer
                            2::second => now;
                            //<<< ""trying again"", {7}.size() >>>;
                        }}
                        // do-while wait for 
                        // (first one: in case our values already contain something)
                        while( myPersonalSamplesAdded < {1} )
                        {{
                            {0}.valueAt( {7}[myPersonalSamplesAdded], myPersonalSamplesAdded::samp );
                            myPersonalSamplesAdded++;
                        }}

                        {8} => now;
                    }}
                }}
                spork ~ UpdateSamples();

                fun void SetGrainPosition( int g )
                {{
                    Std.clamp( g, 0, ({0}.duration() / samp) $ int - 1 ) => grainPosition;
                }}

				
				// LiSa params
				20 => {0}.maxVoices;
				0.5 => {0}.gain;
				true => {0}.loop;
				false => {0}.record;
				
                
				// only spawn grains if a flag is on
                false => int shouldPlayGrains;
				
				fun void SpawnGrains()
				{{
					// create grains
					while( true )
					{{
                        if( shouldPlayGrains )
                        {{
                            // grain length
                            ( grainLengthMS + Math.random2f( -grainLengthRandomnessMS / 2, grainLengthRandomnessMS / 2 ) )
                            * 1::ms => dur grainLength;
                            
                            // grain rate
                            grainRate + Math.random2f( -grainRateRandomness / 2, grainRateRandomness / 2 ) => float grainRate;
                            
                            // grain position
                            ( grainPosition + Math.random2f( -grainPositionRandomness / 2, grainPositionRandomness / 2 ) )
                            * 1::samp => dur playPos;
                            
                            // grain: grainlen, rampup, rampdown, rate, playPos
                            spork ~ PlayGrain( grainLength, rampUpMS::ms, rampDownMS::ms, grainRate, playPos);
                        }}
						
						
						// advance time (time per grain)
						// PARAM: GRAIN SPAWN RATE
						grainSpawnRateMS::ms => now;
					}}
				}}
				spork ~ SpawnGrains();
				
				// sporkee
				fun void PlayGrain( dur grainlen, dur rampup, dur rampdown, float rate, dur playPos )
				{{
					{0}.getVoice() => int newvoice;
					
					if( newvoice > -1 )
					{{
						{0}.rate( newvoice, rate );
						{0}.playPos( newvoice, playPos );
						{0}.rampUp( newvoice, rampup );
						( grainlen - ( rampup + rampdown ) ) => now;
						{0}.rampDown( newvoice, rampdown) ;
						rampdown => now;
					}}
				}}

			}}

            false => int shouldRecord;

            global Event {4}, {5};
            fun void CheckIfShouldRecord( AnimationSynth synth )
            {{
                while( true )
                {{
                    {4} => now;
                    false => synth.shouldPlayGrains;
                    true => shouldRecord;
                    {5} => now;
                    false => shouldRecord;
                    true => synth.shouldPlayGrains;
                }}
            }}

            fun void AddSamplesToLisa( AnimationSynth synth )
            {{
                while( true )
                {{
                    if( shouldRecord )
                    {{
                        adc.last() => synth.AddSample;
                    }}
                    1::samp => now;
                }}
            }}


            global Event {2};
            global int {3};
            fun void ListenForNewSamplePositions( AnimationSynth synth )
            {{
                while( true )
                {{
                    {2} => now;
                    {3} => synth.SetGrainPosition;
                }}
            }}


            AnimationSynth synth => dac;
            spork ~ AddSamplesToLisa( synth );
            spork ~ CheckIfShouldRecord( synth );
            spork ~ ListenForNewSamplePositions( synth );
            // if we were cloned, start playing grains right away
            {6}


            // disabled?
            false => int disabled;
            global Event {9};  // disable me
            global Event {10}; // enable me
            fun void ListenForDisabled()
            {{
                while( true )
                {{
                    {9} => now;
                    if( !disabled )
                    {{
                        true => disabled;
                        false => synth.shouldPlayGrains;
                        synth =< dac;
                    }}
                }}
            }}
            spork ~ ListenForDisabled();

            fun void ListenForEnabled()
            {{
                while( true )
                {{
                    {10} => now;
                    if( disabled )
                    {{
                        false => disabled;
                        true => synth.shouldPlayGrains;
                        synth => dac;
                    }}
                }}
            }}
            spork ~ ListenForEnabled();

            while( true ) {{ 1::second => now; }}

            
        ", myLisa, myCurrentRecordedSample, myNewSamplePositionReady, myNewSamplePosition, 
        myStartRecording, myStopRecording, clonedAddition, mySamples, updateMyLisa,
        myDisableEvent, myEnableEvent ) );
        
        myCurrentRecordedSampleSyncer = gameObject.AddComponent<ChuckIntSyncer>();
        myCurrentRecordedSampleSyncer.SyncInt( myChuck, myCurrentRecordedSample );


        // disable my sound on start if we're soloing audio
        if( SoloCreatureAudio.solo )
        {
            DisableSound();
        }
    }

    public void StartRecordingExamples()
    {
        // chuck should start recording examples (and stop playing back)
        myChuck.BroadcastEvent( myStartRecording );
        haveTrained = false;
    }

    public double[] ProvideExample( double[] input )
    {
        // make our own output
        double[] output = new double[] { myCurrentRecordedSampleSyncer.GetCurrentValue() };
        
        // record it
        ProvideExample( input, output );
        
        // return output
        return output;
    }

    public void ProvideExample( double[] input, double[] output )
    {
        // regression --> output is whatever our recorder has recorded most frequently

        // store
        AnimationAudioExample example = new AnimationAudioExample();
        example.input = input;
        example.output = output;
        myRegressionData.Add( example );

        // also record directly
        myRegression.RecordDataPoint( input, output );
    }

    public void StopRecordingExamples()
    {
        // chuck should stop recording examples and start playing back
        myChuck.BroadcastEvent( myStopRecording );

        #if UNITY_WEBGL
            // do not try to use callback-based function in WebGL
        #else
            // store audio samples in our thread too
            myChuck.GetFloatArray( mySamples, GetMySamples );
        #endif

        Train();
    }

    void RecordInAllExamples()
    {
        for( int i = 0; i < myRegressionData.Count; i++ )
        {
            myRegression.RecordDataPoint( myRegressionData[i].input, myRegressionData[i].output );
        }
    }

    void Train()
    {
        myRegression.Train();
        haveTrained = true;
    }

    public void Predict( double[] input )
    {
        if( !haveTrained ) { return; }
        // predict output and then set new chuck thing
        double[] o = myRegression.Run( input );
        while( o[0] < 0 ) { o[0] += UnityEngine.Random.Range( 1000, 20000 ); }
        myChuck.SetInt( myNewSamplePosition, (int) o[0] );
        myChuck.BroadcastEvent( myNewSamplePositionReady );
    }

    public void CatchUpToGroup()
    {
        // catch up the audio samples
        myChuck.BroadcastEvent( updateMyLisa );
    }

    public void DoActionToTempo( Action a )
    {
        myTempoListener.ListenForEvent( myChuck, "sixteenthNoteHappened", a );
    }

    public void StopDoingActionToTempo()
    {
        myTempoListener.StopListening();
    }

    public void GetMySamples( CK_FLOAT[] samples, CK_UINT numSamples )
    {
        myAudioData = samples;
    }

    public void DisableSound()
    {
        myChuck.BroadcastEvent( myDisableEvent );
        myChuck.enabled = false;
        myAudioSource.enabled = false;
    }

    public void EnableSound()
    {
        myAudioSource.enabled = true;
        myChuck.enabled = true;
        myChuck.BroadcastEvent( myEnableEvent );
    }


    public SerializedAnimationAudio Serialize()
    {
        SerializedAnimationAudio serial = new SerializedAnimationAudio();
        serial.audioData = myAudioData;
        serial.nextAudioFrame = myCurrentRecordedSampleSyncer.GetCurrentValue();
        serial.examples = myRegressionData;

        return serial;
    }

    public IEnumerator InitFromSerial( SerializedAnimationAudio serial )
    {
        // init audio data
        InitFromSerial( serial.audioData, serial.nextAudioFrame );

        // init IML
        myRegressionData = serial.examples;
        RecordInAllExamples();
        Train();

        // hang until successful
        while( !serialInitSuccessful )
        {
            yield return null;
        }
        // flag for starting to play grains immediately
        wereWeCloned = true;
    }
}

[System.Serializable]
public class AnimationAudioExample
{
    public double[] input;
    public double[] output;
}


[System.Serializable] 
public class SerializedAnimationAudio
{
    public List<AnimationAudioExample> examples;
    public int nextAudioFrame;
    public double[] audioData;
}