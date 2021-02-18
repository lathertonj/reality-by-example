using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PhotonLerpTransformView : MonoBehaviour , IPunObservable
{
    public bool lerpPosition = true;
    public bool lerpRotation = true;
    public bool transmitScale = false;
    public float lerpAmount = 0.1f;


    private Vector3 goalPosition;
    private Quaternion goalRotation;
    private PhotonView myView;

    void Awake()
    {
        myView = GetComponent<PhotonView>();
    }

    void Start()
    {
        goalPosition = transform.position;
        goalRotation = transform.rotation;
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

            if( transmitScale )
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

            if( transmitScale )
            {
                transform.localScale = (Vector3) stream.ReceiveNext();
            }
        }
    }


    void Update()
    {
        if( !myView.IsMine )
        {
            // lerp
            transform.position += lerpAmount * ( goalPosition - transform.position );
            transform.rotation = Quaternion.Slerp( transform.rotation, goalRotation, lerpAmount );
        }
    }
}
