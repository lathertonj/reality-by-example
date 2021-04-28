using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class AvatarHintController : MonoBehaviour
{
    private static List<AvatarHintController> avatars;
    private PhotonView myPhotonView;

    void Awake()
    {
        if( avatars == null )
        {
            avatars = new List<AvatarHintController>();
        }
        avatars.Add( this );
        myPhotonView = GetComponent<PhotonView>();
    }

    public static void ShowOthers( float hintTime )
    {
        foreach( AvatarHintController a in avatars )
        {
            if( !a.myPhotonView.IsMine )
            {
                a.ShowAvatarHint( hintTime );
            }
        }
    }

    public static void ShowMe( float hintTime )
    {
        foreach( AvatarHintController a in avatars )
        {
            if( a.myPhotonView.IsMine )
            {
                a.myPhotonView.RPC( "ShowAvatarHint", RpcTarget.Others, hintTime );
            }
        }
    }

    public MeshRenderer myHint;
    private Coroutine hintCoroutine;

    [PunRPC]
    private void ShowAvatarHint( float pauseTimeBeforeFade )
    {
        StopHintAnimation();
        hintCoroutine = StartCoroutine( AnimateHint.AnimateHintFade( myHint, pauseTimeBeforeFade ) );
    }

    private void StopHintAnimation()
    {
        if( hintCoroutine != null )
        {
            StopCoroutine( hintCoroutine );
        }
    }

    private void OnDestroy()
    {
        avatars.Remove( this );
    }
}
