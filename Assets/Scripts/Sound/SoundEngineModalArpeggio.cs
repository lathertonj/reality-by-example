using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundEngineModalArpeggio : MonoBehaviour
{
    public float minDensityCutoff = 0.1f, midDensityCutoff = 0.4f, maxDensityCutoff = 0.7f;
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<ChuckSubInstance>().RunCode( string.Format( @"
            // density: 16th notes, also 2 lines (one up the octave and not necessarily playing the same notes and on the offbeats)
			// maybe a 3rd line that is not on the offbeats but also up the octave not playing same notes
            global int currentChord[];
            global float timbreSlider;
            global float densitySlider;
            global Event quarterNoteHappened, eighthNoteHappened, sixteenthNoteHappened;

			// ugens
			global JCRev theRev;
			ModalBar modey1 => theRev;
			ModalBar modey2 => theRev;
			ModalBar modey3 => theRev;
			0.8 => modey2.gain;
			0.6 => modey3.gain;
			
			// for density slider
            {0} => float minCutoff;
            {1} => float midCutoff;
			{2} => float maxCutoff;

			fun int GetMidiNote( int minOctavesToAdd, int maxOctavesToAdd )
			{{
				return currentChord[ Math.random2( 0, currentChord.size() - 1 ) ] + 12 * Math.random2( minOctavesToAdd, maxOctavesToAdd );
			}}

			fun void PlayModey1()
			{{
				2 => modey1.preset;
				true => int hardPick;
				while( true )
				{{
					quarterNoteHappened => now;
					GetMidiNote( 0, 1 ) => Std.mtof => modey1.freq;

					// is it symmetric? then should be 0.5, 0.8 or 0.2, 0.5? 
					Std.scalef( timbreSlider, 0, 1, 0.2, 0.8 ) + Math.random2f( -0.1, 0.1 ) => modey1.strikePosition;

					// play
					Std.scalef( timbreSlider, 0, 1, 0.3, 0.4 ) + 0.2 * hardPick => modey1.strike;

					// next time
					!hardPick => hardPick;
				}}
			}}
			spork ~ PlayModey1();


			fun void PlayModey2()
			{{
				2 => modey2.preset;
				true => int hardPick;
				while( true )
				{{
					eighthNoteHappened => now;

					if( densitySlider > midCutoff || ( densitySlider > minCutoff && Math.random2f( minCutoff, midCutoff ) < densitySlider ) )
					{{
						GetMidiNote( 1, 2 ) => Std.mtof => modey2.freq;

						// is it symmetric? then should be 0.5, 0.8 or 0.2, 0.5? 
						Std.scalef( timbreSlider, 0, 1, 0.2, 0.8 ) + Math.random2f( -0.1, 0.1 ) => modey2.strikePosition;

						// play
						Std.scalef( timbreSlider, 0, 1, 0.3, 0.4 ) + 0.2 * hardPick => modey2.strike;
					}}

					// next time
					!hardPick => hardPick;

					eighthNoteHappened => now;
				}}
			}}
			spork ~ PlayModey2();

			fun void PlayModey3()
			{{
				2 => modey3.preset;
				true => int hardPick;
				while( true )
				{{
					sixteenthNoteHappened => now;

					if( densitySlider > maxCutoff || ( densitySlider > midCutoff && Math.random2f( midCutoff, maxCutoff ) < densitySlider ) )
					{{
						GetMidiNote( 1, 3 ) => Std.mtof => modey3.freq;

						// is it symmetric? then should be 0.5, 0.8 or 0.2, 0.5? 
						Std.scalef( timbreSlider, 0, 1, 0.2, 0.8 ) + Math.random2f( -0.1, 0.1 ) => modey3.strikePosition;

						// play
						Std.scalef( timbreSlider, 0, 1, 0.3, 0.4 ) + 0.2 * hardPick => modey3.strike;
					}}

					// next time
					!hardPick => hardPick;
				}}
			}}
			spork ~ PlayModey3();
			
            


            while( true ) {{ 1::second => now; }}
        ", minDensityCutoff, midDensityCutoff, maxDensityCutoff ) );
    }

}
