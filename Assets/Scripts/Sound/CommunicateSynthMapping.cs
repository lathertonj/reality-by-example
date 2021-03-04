using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class CommunicateSynthMapping : MonoBehaviour
{
    // TODO: make photon-ready
    //public CommunicateSynth synthPrefab;
    //private static CommunicateSynth myCommunicator;
    public CommunicateSynth myCommunicator;

    public SteamVR_Action_Boolean turnOnCommunicator;
    private SteamVR_Input_Sources handType;
    private SteamVR_Behaviour_Pose pose;

    public enum Mode { RecordExample, PlaybackExamples, ManualMapping1, ManualMapping2 };
    public Mode myMode = Mode.ManualMapping1;
    private bool amOn = false;
    
    void Awake()
    {
        pose = GetComponent<SteamVR_Behaviour_Pose>();
        handType = pose.inputSource;
    }

    // Update is called once per frame
    void Update()
    {
        if( myMode != Mode.RecordExample )
        {
            if( turnOnCommunicator.GetStateDown( handType ) ) 
            {
                myCommunicator.TurnOn();
                amOn = true;
            }
            if( turnOnCommunicator.GetStateUp( handType ) )
            {
                myCommunicator.TurnOff();
                amOn = false;
            }
        }

        if( amOn )
        {
            switch( myMode )
            {
                case Mode.ManualMapping1:
                    // speed = volume
                    float volume = pose.GetVelocity().magnitude.PowMapClamp( 0, 4, 0, 1, 0.5f );
                    
                    // euler x = pitch
                    Vector3 euler = GetModifiedEulerAngles();
                    float pitch = euler.x.PowMapClamp( -80, 80, 1200, 100, 0.5f );

                    // euler z = timbre
                    float timbre = euler.z.PowMapClamp( -80, 80, 0, 1, 1.5f );

                    myCommunicator.SetPitch( pitch );
                    myCommunicator.SetTimbre( timbre );
                    myCommunicator.SetAmplitude( volume );
                    break;
                case Mode.ManualMapping2:
                    // speed = timbre
                    // constant volume
                    // ?? = pitch
                    break;
                case Mode.PlaybackExamples:
                default:
                    break;
            }
        }
    }

    private Vector3 GetModifiedEulerAngles()
    {
        Vector3 euler = transform.localEulerAngles;
        if( euler.x >= 180 ) { euler.x -= 360; }
        if( euler.y >= 180 ) { euler.y -= 360; }
        if( euler.z >= 180 ) { euler.z -= 360; }
        return euler;
    }
}
