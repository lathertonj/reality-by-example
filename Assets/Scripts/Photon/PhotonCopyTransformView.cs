using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PhotonCopyTransformView : MonoBehaviour , IPunObservable
{
    public bool copyPosition = true;
    public bool copyRotation = true;
    public bool copyScale = false;


    void IPunObservable.OnPhotonSerializeView( PhotonStream stream, PhotonMessageInfo info )
    {
        // Write to others
        if (stream.IsWriting)
        {
            // first base position and rotation
            if( copyPosition )
            {
                stream.SendNext( transform.position );
            }

            if( copyRotation )
            {
                stream.SendNext( transform.rotation );
            }

            if( copyScale )
            {
                stream.SendNext( transform.localScale );
            }
        }
        // Read from others
        else
        {
            if( copyPosition )
            {
                transform.position = (Vector3) stream.ReceiveNext();
            }

            if( copyRotation )
            {
                transform.rotation = (Quaternion) stream.ReceiveNext();
            }

            if( copyScale )
            {
                transform.localScale = (Vector3) stream.ReceiveNext();
            }
        }
    }
}
