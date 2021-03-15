using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PhotonEnabledView : MonoBehaviour
{

    [PunRPC]
    public void PhotonEnabledViewSetActive( bool a )
    {
        gameObject.SetActive( a );
    }
}
