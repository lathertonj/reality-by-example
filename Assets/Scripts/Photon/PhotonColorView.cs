using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PhotonColorView : MonoBehaviour , IPunObservable
{

    MeshRenderer me;
    // Start is called before the first frame update
    void Awake()
    {
        me = GetComponentInChildren<MeshRenderer>();
    }

    void IPunObservable.OnPhotonSerializeView( PhotonStream stream, PhotonMessageInfo info )
    {
        // Write to others
        if (stream.IsWriting)
        {
            // use vector4 instead of Color
            Vector4 c = me.material.color;
            // unfortunately Photon has not implemented Vector4
            stream.SendNext( c.w );
            stream.SendNext( c.x );
            stream.SendNext( c.y );
            stream.SendNext( c.z );

        }
        // Read from others
        else
        {
            float w = (float) stream.ReceiveNext();
            float x = (float) stream.ReceiveNext();
            float y = (float) stream.ReceiveNext();
            float z = (float) stream.ReceiveNext();
            Vector4 c = new Vector4( x, y, z, w );
            me.material.color = c;
        }
    }
}
