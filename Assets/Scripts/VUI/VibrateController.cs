using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class VibrateController : MonoBehaviour
{

    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Vibration vibration;


    // frequency: 0 - 320 hz
    // strength: 0 - 1
    public void Vibrate( float durationSeconds, float frequency, float strength )
    {
        vibration.Execute( 0, durationSeconds, frequency, strength, handType );
    }
}
