using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PhotonLerpTransformView : MonoBehaviour , IPunObservable
{
    public bool lerpPosition = true;
    public bool lerpRotation = true;
    public bool lerpScale = false;
    public float lerpAmount = 0.1f;


    private Vector3 goalPosition;
    private Quaternion goalRotation;
    private Vector3 goalScale;
    private PhotonView myView;

    void Awake()
    {
        myView = GetComponent<PhotonView>();
    }

    void Start()
    {
        goalPosition = transform.position;
        goalRotation = transform.rotation;
        goalScale = transform.localScale;
    }

    void IPunObservable.OnPhotonSerializeView( PhotonStream stream, PhotonMessageInfo info )
    {
        // Write to others
        if (stream.IsWriting)
        {
            // first base position and rotation
            if( lerpPosition )
            {
                stream.SendNext( transform.position );
            }

            if( lerpRotation )
            {
                stream.SendNext( transform.rotation );
            }

            if( lerpScale )
            {
                stream.SendNext( transform.localScale );
            }
        }
        // Read from others
        else
        {
            if( lerpPosition )
            {
                goalPosition = (Vector3) stream.ReceiveNext();
            }

            if( lerpRotation )
            {
                goalRotation = (Quaternion) stream.ReceiveNext();
            }

            if( lerpScale )
            {
                goalScale = (Vector3) stream.ReceiveNext();
            }
        }
    }


    void Update()
    {
        if( !myView.IsMine )
        {
            // lerp
            if( lerpPosition ) 
            {
                transform.position += lerpAmount * ( goalPosition - transform.position );
            }
            if( lerpRotation )
            {
                transform.rotation = Quaternion.Slerp( transform.rotation, goalRotation, lerpAmount );
            }
            if( lerpScale )
            {
                transform.localScale += lerpAmount * ( goalScale - transform.localScale );
            }
        }
    }
}
