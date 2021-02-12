using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

#if UNITY_WEBGL
using CK_INT = System.Int32;
using CK_UINT = System.UInt32;
#else
using CK_INT = System.Int64;
using CK_UINT = System.UInt64;
#endif
using CK_FLOAT = System.Double;

public class PhotonAnimatedCreatureSoundView : MonoBehaviour , IPunObservable
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
            // send the audio data and the next float, but not the input/output examples
            SerializedAnimationAudio serial = mySound.Serialize();
            stream.SendNext( serial.audioData );
            stream.SendNext( serial.nextAudioFrame );
        }
        // Read from others
        else
        {
            // pipe in audio data
            CK_FLOAT[] audioData = (CK_FLOAT[]) stream.ReceiveNext();
            int nextAudioFrame = (int) stream.ReceiveNext();
            mySound.SetAudioData( audioData, nextAudioFrame );

            // start playing it
            mySound.EnableSound();
        }
    }
}
