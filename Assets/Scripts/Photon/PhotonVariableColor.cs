using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PhotonVariableColor : MonoBehaviour , IPunInstantiateMagicCallback
{
    public Color myColor, othersColor;

    void IPunInstantiateMagicCallback.OnPhotonInstantiate(PhotonMessageInfo info)
    {
        ParticleSystem.MainModule particles = GetComponent<ParticleSystem>().main;
        if( GetComponent<PhotonView>().IsMine )
        {
            particles.startColor = myColor;
        }
        else
        {
            particles.startColor = othersColor;
        }
    }

}
