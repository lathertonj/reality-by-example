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

    private bool haveTrained = false;


    void Start()
    {
        myRegression = gameObject.AddComponent<RapidMixRegression>();
        myChuck = GetComponent<ChuckSubInstance>();
        myChuck.RunCode( @"
            class AnimationSynth extends Chubgraph
			{
				LiSa lisa => outlet;
				
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
				{
					g => lisa.gain;
					return g;
				}
				
				fun float gain()
				{
					return lisa.gain();
				}
				
				
				
				5::minute => lisa.duration;
                int currentSample;

                fun void AddSample( float s )
                {
                    lisa.valueAt( s, currentSample::samp );
                    currentSample++;
                }

                fun void SetGrainPosition( int g )
                {
                    Std.clamp( g, 0, (lisa.duration() / samp) $ int - 1 ) => grainPosition;
                }

				
				// LiSa params
				20 => lisa.maxVoices;
				0.5 => lisa.gain;
				true => lisa.loop;
				false => lisa.record;
				
                
				// only spawn grains if a flag is on
                false => int shouldPlayGrains;
				
				fun void SpawnGrains()
				{
					// create grains
					while( true )
					{
                        if( shouldPlayGrains )
                        {
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
                        }
						
						
						// advance time (time per grain)
						// PARAM: GRAIN SPAWN RATE
						grainSpawnRateMS::ms => now;
					}
				}
				spork ~ SpawnGrains();
				
				// sporkee
				fun void PlayGrain( dur grainlen, dur rampup, dur rampdown, float rate, dur playPos )
				{
					lisa.getVoice() => int newvoice;
					
					if( newvoice > -1 )
					{
						lisa.rate( newvoice, rate );
						lisa.playPos( newvoice, playPos );
						lisa.rampUp( newvoice, rampup );
						( grainlen - ( rampup + rampdown ) ) => now;
						lisa.rampDown( newvoice, rampdown) ;
						rampdown => now;
					}
				}

			}

            false => int shouldRecord;

            global Event startRecording, stopRecording;
            fun void CheckIfShouldRecord( AnimationSynth synth )
            {
                while( true )
                {
                    startRecording => now;
                    false => synth.shouldPlayGrains;
                    true => shouldRecord;
                    stopRecording => now;
                    false => shouldRecord;
                    true => synth.shouldPlayGrains;
                }
            }

            fun void AddSamplesToLisa( AnimationSynth synth )
            {
                while( true )
                {
                    if( shouldRecord )
                    {
                        adc.last() => synth.AddSample;
                    }
                    1::samp => now;
                }
            }

            global int currentRecordedSample;
            fun void RecordCurrentRecordedSample( AnimationSynth synth )
            {
                while( true )
                {
                    synth.currentSample => currentRecordedSample;
                    // TODO: what time fidelity is necessary?
                    10::ms => now; 
                }
            }


            global Event newSamplePositionReady;
            global int newSamplePosition;
            fun void ListenForNewSamplePositions( AnimationSynth synth )
            {
                while( true )
                {
                    newSamplePositionReady => now;
                    newSamplePosition => synth.SetGrainPosition;
                }
            }


            AnimationSynth synth => dac;
            spork ~ AddSamplesToLisa( synth );
            spork ~ RecordCurrentRecordedSample( synth );
            spork ~ CheckIfShouldRecord( synth );
            spork ~ ListenForNewSamplePositions( synth );


            while( true ) { 1::second => now; }

            
        " );
        myCurrentRecordedSampleSyncer = gameObject.AddComponent<ChuckIntSyncer>();
        myCurrentRecordedSampleSyncer.SyncInt( myChuck, "currentRecordedSample" );
    }

    public void StartRecordingExamples()
    {
        // chuck should start recording examples (and stop playing back)
        myChuck.BroadcastEvent( "startRecording" );
        haveTrained = false;
    }

    public void ProvideExample( double[] input )
    {
        // regression --> output is whatever our recorder has recorded most frequently
        myRegression.RecordDataPoint( input, new double[] { myCurrentRecordedSampleSyncer.GetCurrentValue() } );
        // Debug.Log( "AUDIO DATA: " + myCurrentRecordedSampleSyncer.GetCurrentValue().ToString() );
    }

    public void StopRecordingExamples()
    {
        // chuck should stop recording examples and start playing back
        myChuck.BroadcastEvent( "stopRecording" );

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
        myChuck.SetInt( "newSamplePosition", (int) o[0] );
        myChuck.BroadcastEvent( "newSamplePositionReady" );
    }
}
