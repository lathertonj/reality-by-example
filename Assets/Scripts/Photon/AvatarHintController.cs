using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class AvatarHintController : MonoBehaviour , IPunInstantiateMagicCallback
{
    private static List<AvatarHintController> avatars;
    private PhotonView myPhotonView;
    public MeshRenderer myHintPrefab;

    private MeshRenderer myHint;

    void Awake()
    {
        if( avatars == null )
        {
            avatars = new List<AvatarHintController>();
        }
        avatars.Add( this );
        myPhotonView = GetComponent<PhotonView>();
    }

    void IPunInstantiateMagicCallback.OnPhotonInstantiate(PhotonMessageInfo info)
    {
        // instantiate my hint
        if( myPhotonView.IsMine )
        {
            myHint = PhotonNetwork.Instantiate( myHintPrefab.name, transform.position, Quaternion.identity )
                .GetComponent<MeshRenderer>();
            // my hint should change color when I change as well
            GetComponent<AvatarColorUpdater>().myMaterials.Add( myHint );
        }
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

    private Coroutine hintCoroutine;

    [PunRPC]
    private void ShowAvatarHint( float pauseTimeBeforeFade )
    {
        // stop previous animation
        StopHintAnimation();
        // put the hint where I am
        myHint.transform.position = transform.position;
        // do the animation
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
