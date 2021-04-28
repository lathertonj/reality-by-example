using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class AvatarColorUpdater : MonoBehaviour
{
    private static List<AvatarColorUpdater> avatars;
    private MeshRenderer me;
    private PhotonView myView;
    public float colorAlpha = 0.6f;
    
    void Awake()
    {
        if( avatars == null )
        {
            avatars = new List<AvatarColorUpdater>();
        }    
        avatars.Add( this );

        me = GetComponentInChildren<MeshRenderer>();
        myView = GetComponent<PhotonView>();
    }

    private void UpdateColor( Color newColor )
    {
        newColor.a = colorAlpha;
        me.material.color = newColor;
    }

    private bool BelongsToThisClient()
    {
        return myView.IsMine;
    }

    public static void UpdateColors( Color newColor )
    {
        foreach( AvatarColorUpdater avatar in avatars )
        {
            // only update the ones belonging to this client
            if( avatar.BelongsToThisClient() )
            {
                avatar.UpdateColor( newColor );
            }
        }
    }
}
