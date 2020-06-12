using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoloCreatureAudio : MonoBehaviour
{
    public static bool solo = false;
    
    void Start()
    {
        // enable if this component is in the scene and enabled
        solo = true;
    }

}
