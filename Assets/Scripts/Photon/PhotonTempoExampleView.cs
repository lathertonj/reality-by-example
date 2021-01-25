using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PhotonTempoExampleView : MonoBehaviour , IPunObservable
{
    SoundTempoExample myExample;

    void Awake()
    {
        myExample = GetComponent<SoundTempoExample>();
    }

    void IPunObservable.OnPhotonSerializeView( PhotonStream stream, PhotonMessageInfo info )
    {
        // Write to others
        if (stream.IsWriting)
        {
            float myValue = myExample.myTempo;
            stream.SendNext( myValue );
        }
        // Read from others
        else
        {
            float myValue = (float) stream.ReceiveNext();
            myExample.UpdateMyTempo( myValue );
        }
    }
}
