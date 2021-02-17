using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

using CK_FLOAT = System.Double;


public class PhotonTransferFloatArray : MonoBehaviour
{
    public int packetSize = 4096;

    private CK_FLOAT[] packet;
    private PhotonView myView;

    private List<CK_FLOAT> receive;
    private System.Action< CK_FLOAT[] > myCallback = null;

    void Start()
    {
        packet = new CK_FLOAT[ packetSize ];   
        myView = GetComponent<PhotonView>();
        receive = new List<CK_FLOAT>();
    }

    public void InformWhenReceived( System.Action< CK_FLOAT[] > a )
    {
        myCallback = a;
    }

    // TODO: consider making a coroutine if it slows things down
    public void TransferArray( CK_FLOAT[] array, int maxIndex )
    {
        Debug.Log( "transferring array" );
        // start
        myView.RPC( "TransferFloatArrayStartReceiving", RpcTarget.Others );

        // send packets
        for( int i = 0; i < maxIndex; i+= packetSize )
        {
            System.Array.Copy( array, i, packet, 0, packetSize );
            myView.RPC( "TransferFloatArrayReceive", RpcTarget.Others, packet );
        }

        // end
        myView.RPC( "TransferFloatArrayFinishReceiving", RpcTarget.Others );

        Debug.Log( "finished transfer" );
    }

    [PunRPC]
    void TransferFloatArrayStartReceiving()
    {
        receive.Clear();
    }

    [PunRPC]
    void TransferFloatArrayReceive( CK_FLOAT[] p )
    {
        receive.AddRange( p );
    }

    [PunRPC]
    void TransferFloatArrayFinishReceiving()
    {
        if( myCallback != null )
        {
            myCallback( receive.ToArray() );
        }
        Debug.Log( "finished receiving float array, length " + receive.Count.ToString());
    }
}
