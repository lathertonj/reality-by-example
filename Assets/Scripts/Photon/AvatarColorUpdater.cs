using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class AvatarColorUpdater : MonoBehaviour
{
    private static List<AvatarColorUpdater> avatars;
    private MeshRenderer[] myMaterials;
    private PhotonView myView;
    
    void Awake()
    {
        if( avatars == null )
        {
            avatars = new List<AvatarColorUpdater>();
        }    
        avatars.Add( this );

        myMaterials = GetComponentsInChildren<MeshRenderer>();
        myView = GetComponent<PhotonView>();
    }

    private void UpdateColor( Color newColor )
    {
        foreach( MeshRenderer m in myMaterials )
        {
            m.material.color = new Color( newColor.r, newColor.g, newColor.b, m.material.color.a );
        }
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

    private void OnDestroy()
    {
        avatars.Remove( this );
    }
}
