using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PhotonAnimatedSoundSamplePositionView : MonoBehaviour , IPunObservable
{
    AnimationSoundRecorderPlaybackController mySound;
    void Awake()
    {
        mySound = GetComponent<AnimationSoundRecorderPlaybackController>();
    }
    
    void IPunObservable.OnPhotonSerializeView( PhotonStream stream, PhotonMessageInfo info )
    {
        // Write to others
        if (stream.IsWriting)
        {
            // send the audio data the current position
            stream.SendNext( mySound.GetSamplePosition() );
        }
        // Read from others
        else
        {
            // receive current sample position
            mySound.SetSamplePosition( (int) stream.ReceiveNext() );
        }
    }
}
