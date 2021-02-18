﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PhotonColorView : MonoBehaviour , IPunObservable
{

    MeshRenderer me;
    // Start is called before the first frame update
    void Start()
    {
        me = GetComponent<MeshRenderer>();
    }

    void IPunObservable.OnPhotonSerializeView( PhotonStream stream, PhotonMessageInfo info )
    {
        // Write to others
        if (stream.IsWriting)
        {
            Vector4 c = me.material.color;
            stream.SendNext( c );
        }
        // Read from others
        else
        {
            Color c = (Vector4) stream.ReceiveNext();
            me.material.color = c;
        }
    }
}
