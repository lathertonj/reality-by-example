using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Photon0To1ExampleView : MonoBehaviour , IPunObservable
{
    Sound0To1Example myExample;

    void Awake()
    {
        myExample = GetComponent<Sound0To1Example>();
    }
    void IPunObservable.OnPhotonSerializeView( PhotonStream stream, PhotonMessageInfo info )
    {
        // Write to others
        if (stream.IsWriting)
        {
            float myValue = myExample.myValue;
            stream.SendNext( myValue );
        }
        // Read from others
        else
        {
            float myValue = (float) stream.ReceiveNext();
            myExample.UpdateMyValue( myValue );
        }
    }
}
