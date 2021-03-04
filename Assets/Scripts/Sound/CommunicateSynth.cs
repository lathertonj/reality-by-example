using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommunicateSynth : MonoBehaviour
{

    // TODO: make this class dynamically via photon
    string myPitch, myAmplitude, myTimbre;
    float currentPitch, currentAmplitude, currentTimbre;
    bool amOn;
    ChuckMainInstance myChuck;
    void Start()
    {
        myChuck = TheChuck.instance;
        myPitch = myChuck.GetUniqueVariableName( "pitch" );
        myAmplitude = myChuck.GetUniqueVariableName( "amplitude" );
        myTimbre = myChuck.GetUniqueVariableName( "timbre" );
        myChuck.RunCode( string.Format( @"
            TriOsc t => LPF l => dac;
            100 => float currentFreq => global float {0};
            0 => float currentAmplitude => global float {1};
            0 => float currentTimbre => global float {2};
            0.1 => float slew;


            fun void AssignParams()
            {{
                while( true )
                {{
                    // slew these params
                    if( !Math.isnan({0}) ) {{ slew * ( {0} - currentFreq ) +=> currentFreq; }}
                    if( !Math.isnan({1}) ) {{ slew * ( {1} - currentAmplitude ) +=> currentAmplitude; }}
                    if( !Math.isnan({2}) ) {{ slew * ( {2} - currentTimbre ) +=> currentTimbre; }}
                    currentFreq => t.freq;
                    currentAmplitude => t.gain;
                    Math.min( 50 + currentTimbre*10000, 18000 ) => l.freq;
                    10::ms => now;
                }}
            }}

            spork ~ AssignParams();
            while( true ) {{ 1::second => now; }}
        ", myPitch, myAmplitude, myTimbre ) );

        currentAmplitude = 0;
        currentPitch = 100;
        currentTimbre = 0;
        amOn = false;
    }

    public void TurnOn()
    {
        amOn = true;
        SetAmplitude( currentAmplitude );
    }
    
    public void TurnOff()
    {
        amOn = false;
        SetAmplitude( currentAmplitude );
    }

    public void SetAmplitude( float a )
    {
        currentAmplitude = a;
        if( amOn )
        {
            myChuck.SetFloat( myAmplitude, currentAmplitude );
        }
        else
        {
            myChuck.SetFloat( myAmplitude, 0 );
        }
    }

    public void SetPitch( float p )
    {
        currentPitch = p;
        myChuck.SetFloat( myPitch, currentPitch );
    }

    public void SetTimbre( float t )
    {
        currentTimbre = t;
        myChuck.SetFloat( myTimbre, currentTimbre );
    }
}
