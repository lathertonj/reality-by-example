using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PhotonAnimatedCreatureColorView : MonoBehaviour , IPunObservable
{
    AnimatedCreatureColor myColor;
    void Awake()
    {
        myColor = GetComponent<AnimatedCreatureColor>();
    }
    
    void IPunObservable.OnPhotonSerializeView( PhotonStream stream, PhotonMessageInfo info )
    {
        // Write to others
        if (stream.IsWriting)
        {
            // send the color
            stream.SendNext( myColor.Serialize() );
        }
        // Read from others
        else
        {
            // receive the color
            myColor.Deserialize( (float) stream.ReceiveNext() );
        }
    }

}
