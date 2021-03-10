using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PhotonCommunicateSynthView : MonoBehaviour , IPunObservable
{
    CommunicateSynth mySynth;

    void Awake()
    {
        mySynth = GetComponent<CommunicateSynth>();
    }

    void IPunObservable.OnPhotonSerializeView( PhotonStream stream, PhotonMessageInfo info )
    {
        // Write to others
        if (stream.IsWriting)
        {
            // get values
            float pitch, timbre, amplitude;
            mySynth.GetAll( out pitch, out amplitude, out timbre );
            stream.SendNext( pitch );
            stream.SendNext( timbre );
            stream.SendNext( amplitude );
        }
        // Read from others
        else
        {
            float pitch = (float) stream.ReceiveNext();
            float timbre = (float) stream.ReceiveNext();
            float amplitude = (float) stream.ReceiveNext();
            mySynth.SetAll( pitch, amplitude, timbre );
        }
    }
}
