using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundEngineAhhChords : MonoBehaviour
{
    public float minDensityCutoff = 0.1f, maxDensityCutoff = 0.4f;
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<ChuckSubInstance>().RunCode( string.Format( @"
            class AhhSynth extends Chubgraph
			{{
				LiSa lisa => outlet;
				
				// spawn rate: how often a new grain is spawned (ms)
				25 =>  float grainSpawnRateMS;
				0 =>  float grainSpawnRateVariationMS;
				0.0 =>  float grainSpawnRateVariationRateMS;
				
				// position: where in the file is a grain (0 to 1)
				0.61 =>  float grainPosition;
				0.2 =>  float grainPositionRandomness;
				
				// grain length: how long is a grain (ms)
				300 =>  float grainLengthMS;
				10 =>  float grainLengthRandomnessMS;
				
				// grain rate: how quickly is the grain scanning through the file
				1.004 =>  float grainRate; // 1.002 == in-tune Ab
				0.015 =>  float grainRateRandomness;
				
				// ramp up/down: how quickly we ramp up / down
				50 =>  float rampUpMS;
				200 =>  float rampDownMS;
				
				// gain: how loud is everything overall
				1 =>  float gainMultiplier;
				
				float myFreq;
				fun float freq( float f )
				{{
					f => myFreq;
					61 => Std.mtof => float baseFreq;
					// 1.002 == in tune for 56 for aah4.wav
					// 1.004 == in tune for 60 for aah5.wav
					myFreq / baseFreq * 0.98 => grainRate;
					
					return myFreq;
				}}
				
				fun float freq()
				{{
					return myFreq;
				}}
				
				fun float gain( float g )
				{{
					g => lisa.gain;
					return g;
				}}
				
				fun float gain()
				{{
					return lisa.gain();
				}}
				
				
				
				SndBuf buf; 
				me.dir() + ""aah5.wav"" => buf.read;
				buf.length() => lisa.duration;
				// copy samples in
				for( int i; i < buf.samples(); i++ )
				{{
					lisa.valueAt( buf.valueAt( i ), i::samp );
				}}
				
				
				buf.length() => dur bufferlen;
				
				// LiSa params
				100 => lisa.maxVoices;
				0.1 => lisa.gain;
				true => lisa.loop;
				false => lisa.record;
				
				
				// modulate
				SinOsc freqmod => blackhole;
				0.1 => freqmod.freq;
				
				
				
				0.1 => float maxGain;
				
				fun void SetGain()
				{{
					while( true )
					{{
						maxGain * gainMultiplier => lisa.gain;
						1::ms => now;
					}}
				}}
				spork ~ SetGain();
				
				
				fun void SpawnGrains()
				{{
					// create grains
					while( true )
					{{
						// grain length
						( grainLengthMS + Math.random2f( -grainLengthRandomnessMS / 2, grainLengthRandomnessMS / 2 ) )
						* 1::ms => dur grainLength;
						
						// grain rate
						grainRate + Math.random2f( -grainRateRandomness / 2, grainRateRandomness / 2 ) => float grainRate;
						
						// grain position
						( grainPosition + Math.random2f( -grainPositionRandomness / 2, grainPositionRandomness / 2 ) )
						* bufferlen => dur playPos;
						
						// grain: grainlen, rampup, rampdown, rate, playPos
						spork ~ PlayGrain( grainLength, rampUpMS::ms, rampDownMS::ms, grainRate, playPos);
						
						// advance time (time per grain)
						// PARAM: GRAIN SPAWN RATE
						grainSpawnRateMS::ms  + freqmod.last() * grainSpawnRateVariationMS::ms => now;
						grainSpawnRateVariationRateMS => freqmod.freq;
					}}
				}}
				spork ~ SpawnGrains();
				
				// sporkee
				fun void PlayGrain( dur grainlen, dur rampup, dur rampdown, float rate, dur playPos )
				{{
					lisa.getVoice() => int newvoice;
					
					if(newvoice > -1)
					{{
						lisa.rate( newvoice, rate );
						lisa.playPos( newvoice, playPos );
						lisa.rampUp( newvoice, rampup );
						( grainlen - ( rampup + rampdown ) ) => now;
						lisa.rampDown( newvoice, rampdown) ;
						rampdown => now;
					}}
				}}
			}}
            global int currentChord[];
            global Event chordsUpdated;

			AhhSynth ahhChord[currentChord.size()];
            LPF lpf => global JCRev theRev;
            for( int i; i < ahhChord.size(); i++ )
            {{
                ahhChord[i] => lpf;
                // TODO bug this is 0...
                // currentChord[i] => Std.mtof => ahhChord[i].freq;
                57 - 12 => Std.mtof => ahhChord[i].freq;
            }}

			7000 => lpf.freq;


            fun void SetChords()
            {{
                while( true )
                {{
                    SlewChords();
                    chordsUpdated => now;
                }}
            }}
            spork ~ SetChords();

            fun void SlewChords()
            {{
                for( int i; i < ahhChord.size(); i++ )
                {{
                    ahhChord[i].freq() => Std.ftom => float currentMidi;
                    currentChord[i] - 12 => int newMidi;
                    // if( i == 2 ) {{ 12 +=> newMidi; }}
                    spork ~ SlewChordNote( i, currentMidi, newMidi );
                }}  
            }}

            fun void SlewChordNote( int i, float fromMidi, float toMidi )
            {{
                0.03 => float chordSlew;
                fromMidi => float currentMidi;
                while( Math.fabs( toMidi - currentMidi ) > 0.001 )
                {{
                    chordSlew * ( toMidi - currentMidi ) +=> currentMidi;
                    currentMidi => Std.mtof => ahhChord[i].freq;
                    1::ms => now;
                }}
            }}

            {0} => float minCutoff;
            {1} => float maxCutoff;
            global float densitySlider;

            fun void SetVolume()
            {{
                0.01 => float volumeUpSlew;
                0.002 => float volumeDownSlew;
                float currentVolume;
                while( true )
                {{
                    // compute 
                    0 => float goalVolume;
                    if( densitySlider > maxCutoff )
                    {{ 
                        1 => goalVolume; 
                    }}
                    else if( densitySlider > minCutoff )
                    {{
                        Std.scalef( densitySlider, minCutoff, maxCutoff, 0.25, 1 ) => goalVolume;
                    }}

                    // slew
                    if( goalVolume > currentVolume )
                    {{
                        volumeUpSlew * ( goalVolume - currentVolume ) +=> currentVolume;
                    }}
                    else
                    {{
                        volumeDownSlew * ( goalVolume - currentVolume ) +=> currentVolume;
                    }}
                    currentVolume * 0.8 => lpf.gain;
                    1::ms => now;
                }}
            }}
            spork ~ SetVolume();

            global float timbreSlider;
            fun void SetTimbre()
            {{
                while( true )
                {{
                    Std.scalef( timbreSlider, 0, 1, 60, 130 ) => Std.mtof => lpf.freq;
                    10::ms => now;
                }}
            }}
            spork ~ SetTimbre();

            while( true ) {{ 1::second => now; }}
        ", minDensityCutoff, maxDensityCutoff ) );
    }

}
