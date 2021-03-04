using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PhotonLaserParticleEmitterView : MonoBehaviour , IPunObservable
{
    private DrawInAirController myDrawer;

    public void Init( DrawInAirController c )
    {
        myDrawer = c;
    }

    void IPunObservable.OnPhotonSerializeView( PhotonStream stream, PhotonMessageInfo info )
    {
        // Write to others
        if( stream.IsWriting )
        {
            bool enabled = myDrawer.GetEnabled();
            float rate = myDrawer.GetEmissionRate();
            float size = myDrawer.GetSize();
            Color color = myDrawer.GetColor();

            stream.SendNext( enabled );
            stream.SendNext( rate );
            stream.SendNext( size );
            stream.SendNext( color.r );
            stream.SendNext( color.g );
            stream.SendNext( color.b );
            stream.SendNext( color.a );
        }
        // Read from others
        else
        {
            bool enabled = (bool) stream.ReceiveNext();
            float rate = (float) stream.ReceiveNext();
            float size = (float) stream.ReceiveNext();
            float r = (float) stream.ReceiveNext();
            float g = (float) stream.ReceiveNext();
            float b = (float) stream.ReceiveNext();
            float a = (float) stream.ReceiveNext();

            myDrawer.SetEmissionRate( rate );
            myDrawer.SetSize( size );
            myDrawer.SetColor( new Color( r, g, b, a ) );
            myDrawer.SetEnabled( enabled );
        }
    }
}
