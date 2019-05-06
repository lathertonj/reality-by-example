using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExtensionMethods
{
    public static float Map( this float value, float inputFrom, float inputTo, float outputFrom, float outputTo )
    {
        return ( value - inputFrom ) / ( inputTo - inputFrom ) * ( outputTo - outputFrom ) + outputFrom;
    }

    public static float MapClamp( this float value, float inputFrom, float inputTo, float outputFrom, float outputTo )
    {
        return Mathf.Clamp( value.Map( inputFrom, inputTo, outputFrom, outputTo ), Mathf.Min( outputFrom, outputTo ), Mathf.Max( outputFrom, outputTo ) );
    }

    public static float PowMap( this float value, float inputFrom, float inputTo, float outputFrom, float outputTo, float pow )
    {
        return Mathf.Pow( ( value - inputFrom ) / ( inputTo - inputFrom ), pow ) * ( outputTo - outputFrom ) + outputFrom;
    }

    public static float PowMapClamp( this float value, float inputFrom, float inputTo, float outputFrom, float outputTo, float pow )
    {
        return Mathf.Clamp( value.PowMap( inputFrom, inputTo, outputFrom, outputTo, pow ), Mathf.Min( outputFrom, outputTo ), Mathf.Max( outputFrom, outputTo ) );
    }

}