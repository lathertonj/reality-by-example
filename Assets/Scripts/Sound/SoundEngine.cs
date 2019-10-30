using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundEngine : MonoBehaviour
{

    ChuckSubInstance myChuck;

    // Start is called before the first frame update
    void Start()
    {
        myChuck = GetComponent<ChuckSubInstance>();
        myChuck.RunCode( @"
            // THINGS FOR THE INDIVIDUAL COMPONENTS TO RESPOND TO
            0 => global float timbreSlider;
            0.55 => global float densitySlider;
            global Event measureHappened, quarterNoteHappened, eighthNoteHappened, sixteenthNoteHappened;
            global Event chordsUpdated;
            // also: global int currentChord[ probably 4 ]


            // THINGS WE CONTROL
            2 => global int whichChords;
            0.5 => global float quarterNoteTempoSeconds;
            1 => global float volumeSlider;
            global JCRev theRev => dac;
            0.05 => theRev.mix;

            fun void SendTempoEvents()
            {
                while( true )
                {
                    measureHappened.broadcast();
                    repeat(4)
                    {
                        quarterNoteHappened.broadcast();
                        eighthNoteHappened.broadcast();
                        sixteenthNoteHappened.broadcast();
                        (quarterNoteTempoSeconds / 4)::second => now;

                        sixteenthNoteHappened.broadcast();
                        (quarterNoteTempoSeconds / 4)::second => now;

                        eighthNoteHappened.broadcast();
                        sixteenthNoteHappened.broadcast();
                        (quarterNoteTempoSeconds / 4)::second => now;

                        sixteenthNoteHappened.broadcast();
                        (quarterNoteTempoSeconds / 4)::second => now;
                    }
                }
            }
            spork ~ SendTempoEvents();


            // G is the lowest root note; F# root starts ABOVE middle C
            55 => int G;
            56 => int Gs;
            57 => int A;
            58 => int As;
            59 => int B;
            60 => int C;
            61 => int Cs;
            62 => int D;
            63 => int Ds;
            64 => int E;
            65 => int F;
            66 => int Fs;
            [ 
                // V:  AM7 x2, EM7 x2
                [ [A, Cs+12, E+12,  Gs+12], [A, Cs+12, E+12,  Gs+12], 
                  [E, B+12,  Ds+12, Gs+12], [E, B+12,  Ds+12, Gs+12] ],
                // V 2: AM7, EM7, g#7, g#7 sus4
                [ [A,  Cs+12, E+12,  Gs+12], 
                  [E,  B+12,  Ds+12, Gs+12],
                  [Gs, B+12,  Ds+12, Fs],
                  [Gs, Cs+12, Ds+12, Fs] ],
                // VII: f#, DM7, c#7, f#sus2
                [ [Fs, A+12,  Cs+12, Fs+12],
                  [D,  A+12,  Cs+12, Fs+12],
                  [Cs, Gs+12, B+12,  E+12],
                  [Fs, Gs+12, Cs+12, Fs+12] ],
                // I: b, f#7, GM7, DM7
                [ [B,  Fs,   B+12,  D+12], 
                  [Fs, A+12, Cs+12, E+12],
                  [G,  B+12, D+12,  Fs+12],
                  [D,  A+12, Cs+12, Fs+12] ],
                // IIIB/VIII: A+9+13, f#sus4, AM7sus2, E
                [ [A,  Cs+12, B+12, Fs+12],
                  [Fs, Cs+12, B+12, Fs+12], 
                  [A,  E+12,  B+12, Gs+24], 
                  [E,  E+12,  B+12, Gs+24] ],
                // IV: f#7+11 x2, E+9sus4 x2
                [ [Fs, A+12, E+12,  B+24], [Fs, A+12, E+12,  B+24], 
                  [E,  A+12, Fs+12, B+24], [E,  A+12, Fs+12, B+24] ]
            ] @=> int theChords[][][];
            global int currentChord[ theChords[0][0].size() ];

            fun void PopulateCurrentChord()
            {
                int whichChord;
                while( true )
                {
                    for( int i; i < currentChord.size(); i++ )
                    {
                        theChords[whichChords][whichChord][i] => currentChord[i];
                    }
                    whichChord++;
                    whichChord % 4 => whichChord;

                    chordsUpdated.broadcast();
                    
                    repeat(1) { measureHappened => now; }
                }
            }
            spork ~ PopulateCurrentChord();


            fun void ControlVolume()
            {
                while( true )
                {
                    volumeSlider => dac.gain;
                    10::ms => now;
                }
            }
            spork ~ ControlVolume();
            while( true ) { 1::second => now; }
        " ); 
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetChord( int whichChord )
    {
        myChuck.SetInt( "whichChord", whichChord );
    }

    public void SetTimbre( float zeroToOne )
    {
        myChuck.SetFloat( "timbreSlider", zeroToOne );
    }

    public void SetDensity( float zeroToOne )
    {
        myChuck.SetFloat( "densitySlider", zeroToOne );
    }

    public void SetVolume( float zeroToOne )
    {
        myChuck.SetFloat( "volumeSlider", zeroToOne );
    }

    public void SetQuarterNoteTime( float inSeconds )
    {
        myChuck.SetFloat( "quarterNoteTempoSeconds", inSeconds );
    }

    public void SetSong( int TODOWHATARGS )
    {

    }


}
