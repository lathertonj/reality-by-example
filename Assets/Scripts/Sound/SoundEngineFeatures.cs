using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundEngineFeatures
{
    public static double[] InputVector( Vector3 position )
    {
        // TODO: find features based on terrains below us? 
        float x = position.x, y = position.y, z = position.z;
        return new double[] {
            x, y, z,
            x*x, y*y, z*z, 
            x*y, x*z, y*z,
            x*x*x, y*y*y, z*z*z,
            x*x*y, x*x*z, y*y*z, 
            x*y*y, x*z*z, y*z*z, 
            x*y*z
        };
    }
}
