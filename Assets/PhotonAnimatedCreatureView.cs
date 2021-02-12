using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PhotonAnimatedCreatureView : MonoBehaviour , IPunObservable
{

    Transform myBase;
    Transform[] myLimbs;
    void Awake()
    {
        AnimationByRecordedExampleController myCreature = GetComponent<AnimationByRecordedExampleController>();
        myBase = myCreature.modelBaseToAnimate;
        myLimbs = myCreature.modelRelativePointsToAnimate;
    }
    void IPunObservable.OnPhotonSerializeView( PhotonStream stream, PhotonMessageInfo info )
    {
        // Write to others
        if (stream.IsWriting)
        {
            // first base position and rotation
            stream.SendNext( myBase.position );
            stream.SendNext( myBase.rotation );
            // then limb positions
            for( int i = 0; i < myLimbs.Length; i++ )
            {
                stream.SendNext( myLimbs[i].position );
            }
        }
        // Read from others
        else
        {
            myBase.position = (Vector3) stream.ReceiveNext();
            myBase.rotation = (Quaternion) stream.ReceiveNext();
            for( int i = 0; i < myLimbs.Length; i++ )
            {
                myLimbs[i].position = (Vector3) stream.ReceiveNext();
            }
        }
    }
}
