using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PhotonChordExampleView : MonoBehaviour , IPunObservable
{
    SoundChordExample myExample;

    void Awake()
    {
        myExample = GetComponent<SoundChordExample>();
    }

    void IPunObservable.OnPhotonSerializeView( PhotonStream stream, PhotonMessageInfo info )
    {
        // Write to others
        if (stream.IsWriting)
        {
            int myValue = myExample.myChord;
            stream.SendNext( myValue );
        }
        // Read from others
        else
        {
            int myValue = (int) stream.ReceiveNext();
            myExample.UpdateMyChord( myValue );
        }
    }
}
