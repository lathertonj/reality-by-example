using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PhotonEnabledView : MonoBehaviour , IPunObservable
{
    void IPunObservable.OnPhotonSerializeView( PhotonStream stream, PhotonMessageInfo info )
    {
        // Write to others
        if (stream.IsWriting)
        {
            // is the renderer active?
            bool a = gameObject.activeSelf;
            stream.SendNext( a );
        }
        // Read from others
        else
        {
            bool a = (bool) stream.ReceiveNext();
            gameObject.SetActive( a );
        }
    }
}
