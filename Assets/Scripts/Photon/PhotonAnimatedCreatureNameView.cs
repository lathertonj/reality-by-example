using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PhotonAnimatedCreatureNameView : MonoBehaviour , IPunObservable
{
    AnimationByRecordedExampleController creature;
    void Awake()
    {
        creature = GetComponent<AnimationByRecordedExampleController>();
    }
    
    void IPunObservable.OnPhotonSerializeView( PhotonStream stream, PhotonMessageInfo info )
    {
        // Write to others
        if (stream.IsWriting)
        {
            // send the base name and number
            stream.SendNext( creature.GetBaseName() );
            stream.SendNext( creature.GetBaseNumber() );
        }
        // Read from others
        else
        {
            // receive the name and number
            string baseName = (string) stream.ReceiveNext();
            int baseNumber = (int) stream.ReceiveNext();
            creature.SetDisplayName( baseName, baseNumber );
        }
    }
}
