using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationSoundRecorderPlaybackController : MonoBehaviour
{
    // predict position in the sound file based on terrain input features
    // only do this once every animation frame, and then just randomly trigger grains around this position
    // until we receive the next update.

    // but, I anticipate this mapping will be very unsatisfying or confusing...

    // give US the input features
    // we will record what is the offset during that time

    // TODO: generalize chuck code so that can have multiple of these components running

    // TODO: make a simpler version where randomly we pick between different phrases according to the prominence of the animation...

    // Start is called before the first frame update
    private RapidMixRegression myRegression;

    private ChuckSubInstance myChuck;
    private ChuckIntSyncer myCurrentRecordedSampleSyncer;

    string myLisa, myCurrentRecordedSample, myNewSamplePositionReady, myNewSamplePosition, myStartRecording, myStopRecording;
    string mySamples;

    private bool haveTrained = false;

    List< double[] > myInputData = new List< double[] >();
    List< double[] > myOutputData = new List< double[] >();
    private bool wereWeCloned = false;

    public void CloneFrom( AnimationSoundRecorderPlaybackController other )
    {
        // flag for later
        wereWeCloned = true;

        // copy regression data
        myInputData = other.myInputData;
        myOutputData = other.myOutputData;
        for( int i = 0; i < myInputData.Count; i++ )
        {
            myRegression.RecordDataPoint( myInputData[i], myOutputData[i] );
        }
        Train();

        // copy chuck lisa samples
        InitChuckVariableNames();

        myChuck.RunCode( string.Format( @"
            global LiSa {0}, {1};
            {1}.duration() => {0}.duration;
            global int {2};
            global float {3}[];


            // copy samples
            for( int i; i < {2}; i++ )
            {{
                {0}.valueAt( {3}[i], i::samp );
            }}
        ", myLisa, other.myLisa, other.myCurrentRecordedSample, other.mySamples ) );
    }

    private bool variableNamesInit = false;
    void InitChuckVariableNames()
    {
        if( variableNamesInit ) { return; }

        myChuck = GetComponent<ChuckSubInstance>();
        myLisa = myChuck.GetUniqueVariableName( "lisa" );
        myCurrentRecordedSample = myChuck.GetUniqueVariableName( "currentRecordedSample" );
        myNewSamplePositionReady = myChuck.GetUniqueVariableName( "newSamplePositionReady" );
        myNewSamplePosition = myChuck.GetUniqueVariableName( "newSamplePosition" );
        myStartRecording = myChuck.GetUniqueVariableName( "startRecording" );
        myStopRecording = myChuck.GetUniqueVariableName( "stopRecording" );
        mySamples = myChuck.GetUniqueVariableName( "mySamples" );
        variableNamesInit = true;
    }

    void Awake()
    {
        myRegression = gameObject.AddComponent<RapidMixRegression>();
    }

    void Start()
    {
        InitChuckVariableNames();
        string clonedAddition = wereWeCloned ? @"true => synth.shouldPlayGrains;" : "";
        myChuck.RunCode( string.Format( @"
            global LiSa {0};
            global float {7}[ (5::minute/samp) $ int];
            global int {1};
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

                fun void AddSample( float s )
                {{
                    {0}.valueAt( s, {1}::samp );
                    // store in global variable too
                    s => {7}[{1}];
                    {1}++;
                }}

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

            while( true ) {{ 1::second => now; }}

            
        ", myLisa, myCurrentRecordedSample, myNewSamplePositionReady, myNewSamplePosition, myStartRecording, myStopRecording, clonedAddition, mySamples ) );
        myCurrentRecordedSampleSyncer = gameObject.AddComponent<ChuckIntSyncer>();
        myCurrentRecordedSampleSyncer.SyncInt( myChuck, myCurrentRecordedSample );
    }

    public void StartRecordingExamples()
    {
        // chuck should start recording examples (and stop playing back)
        myChuck.BroadcastEvent( myStartRecording );
        haveTrained = false;
    }

    public void ProvideExample( double[] input )
    {
        myInputData.Add( input );
        // regression --> output is whatever our recorder has recorded most frequently
        double[] output = new double[] { myCurrentRecordedSampleSyncer.GetCurrentValue() };
        myOutputData.Add( output );
        // also record directly
        myRegression.RecordDataPoint( input, output );
    }

    public void StopRecordingExamples()
    {
        // chuck should stop recording examples and start playing back
        myChuck.BroadcastEvent( myStopRecording );

        Train();
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
        while( o[0] < 0 ) { o[0] += Random.Range( 1000, 20000 ); }
        myChuck.SetInt( myNewSamplePosition, (int) o[0] );
        myChuck.BroadcastEvent( myNewSamplePositionReady );
    }
}
