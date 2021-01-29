using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PhotonTextureExampleView : MonoBehaviour , IPunObservable
{

    TerrainTextureExample myTexture;

    // Start is called before the first frame update
    void Awake()
    {
        myTexture = GetComponent<TerrainTextureExample>();
    }

    void IPunObservable.OnPhotonSerializeView( PhotonStream stream, PhotonMessageInfo info )
    {
        // Write to others
        if (stream.IsWriting)
        {
            int myType = myTexture.myCurrentValue;
            stream.SendNext( myType );
        }
        // Read from others
        else
        {
            int myNewType = (int) stream.ReceiveNext();
            myTexture.SwitchTo( myNewType );
        }
    }
}
