using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PhotonAnimatedCreatureView : MonoBehaviour , IPunObservable
{

    Transform myBase;
    Transform[] myLimbs;

    Vector3 myGoalBase;
    Quaternion myGoalRotation;
    Vector3[] myGoalLimbs;

    PhotonView myView;

    public float interpAmount = 0.1f;
    
    void Awake()
    {
        AnimationByRecordedExampleController myCreature = GetComponent<AnimationByRecordedExampleController>();
        myBase = myCreature.modelBaseToAnimate;
        myGoalBase = myBase.position;
        myGoalRotation = myBase.rotation;
        myLimbs = myCreature.modelRelativePointsToAnimate;
        myGoalLimbs = new Vector3[ myLimbs.Length ];
        myView = GetComponent<PhotonView>();
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
            myGoalBase = (Vector3) stream.ReceiveNext();
            myGoalRotation = (Quaternion) stream.ReceiveNext();
            for( int i = 0; i < myLimbs.Length; i++ )
            {
                myGoalLimbs[i] = (Vector3) stream.ReceiveNext();
            }
        }
    }


    void Update()
    {
        if( !myView.IsMine )
        {
            // lerp
            myBase.position += interpAmount * ( myGoalBase - myBase.position );
            myBase.rotation = Quaternion.Slerp( myBase.rotation, myGoalRotation, interpAmount );

            for( int i = 0; i < myLimbs.Length; i++ )
            {
                myLimbs[i].position += interpAmount * ( myGoalLimbs[i] - myLimbs[i].position );   
            }
        }
    }
}
