using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PhotonRendererEnabledView : MonoBehaviour , IPunObservable
{
    MeshRenderer me;
    void Awake()
    {
        me = GetComponent<MeshRenderer>();
    }

    void IPunObservable.OnPhotonSerializeView( PhotonStream stream, PhotonMessageInfo info )
    {
        // Write to others
        if (stream.IsWriting)
        {
            // is the renderer active?
            bool e = me.enabled;
            stream.SendNext( e );
        }
        // Read from others
        else
        {
            bool e = (bool) stream.ReceiveNext();
            me.enabled = e;
        }
    }
}
