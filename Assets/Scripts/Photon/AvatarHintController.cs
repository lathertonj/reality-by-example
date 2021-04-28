using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class AvatarHintController : MonoBehaviour
{
    private static List<AvatarHintController> avatars;
    private PhotonView myPhotonView;
    public MeshRenderer myHintPrefab;

    private MeshRenderer myHint;
    private MeshRenderer myBody;

    void Awake()
    {
        if( avatars == null )
        {
            avatars = new List<AvatarHintController>();
        }
        avatars.Add( this );
        myPhotonView = GetComponent<PhotonView>();
        myBody = GetComponentInChildren<MeshRenderer>();
        // instantiate my hint (local object)
        myHint = Instantiate( myHintPrefab, transform.position, Quaternion.identity );
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
        
        // hint position and color
        PrepareHint();

        // do the animation
        hintCoroutine = StartCoroutine( AnimateHint.AnimateHintFade( myHint, pauseTimeBeforeFade ) );
    }

    private void PrepareHint()
    {
        // set position to be my position
        myHint.transform.position = transform.position;

        // and color to be my color
        Color c = myBody.material.color;
        myHint.material.color = new Color( c.r, c.g, c.b, myHint.material.color.a );
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
