using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundEngineShakers : MonoBehaviour
{
    public float minDensityToMakeSound = 0.3f;
    public float densityAboveWhichAlwaysPlay = 0.9f;
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<ChuckSubInstance>().RunCode( string.Format( @"
            Shakers s => global JCRev theRev;
            {0} => float minCutoff;
            {1} => float maxCutoff;

            global Event quarterNoteHappened, eighthNoteHappened;
            global float densitySlider;

            fun int ShouldPlayShakerNote()
            {{
                return densitySlider >= maxCutoff || ( densitySlider > minCutoff && Math.random2f( minCutoff, maxCutoff ) < densitySlider );
            }}

            // params
            0.7 => s.decay; // 0 to 1
            50 => s.objects; // 0 to 128
            11 => s.preset; // 0 to 22.  0 is good for galloping. 
            // 11 is good for eighths
            fun void PlayShakers()
            {{
                while( true )
                {{
                    quarterNoteHappened => now;
                    if( ShouldPlayShakerNote() )
                    {{
                        Math.random2f( 0.7, 0.9 ) => s.energy;
                        1 => s.noteOn;
                    }}
                    

                    eighthNoteHappened => now;
    
                    if( ShouldPlayShakerNote() )
                    {{
                        Math.random2f( 0.3, 0.5 ) => s.energy;
                        1 => s.noteOn;
                    }}
                }}
            }}
            spork ~ PlayShakers() @=> Shred playShakersShred;
            fun void ShakersGain()
            {{
                float currentGain;
                0.001 => float upSlew;
                0.0003 => float downSlew;
                float goalGain;
                while( true ) 
                {{
                    Std.scalef( Std.clampf( densitySlider, minCutoff, maxCutoff ), minCutoff, maxCutoff, 0.3, 1 ) => goalGain;

                    if( goalGain > currentGain )
                    {{
                        upSlew * ( goalGain - currentGain ) +=> currentGain;
                    }}
                    else
                    {{
                        downSlew * ( goalGain - currentGain ) +=> currentGain;
                    }}
                    currentGain * 0.6 => s.gain;
                    1::ms => now;
                }}
            }}
            spork ~ ShakersGain();

            while( true ) {{ 1::second => now; }}            
        ", minDensityToMakeSound, densityAboveWhichAlwaysPlay ) );
    }

}
