using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PhotonGISExampleView : MonoBehaviour , IPunObservable
{
    TerrainGISExample myGIS;

    // Start is called before the first frame update
    void Awake()
    {
        myGIS = GetComponent<TerrainGISExample>();
    }

    void IPunObservable.OnPhotonSerializeView( PhotonStream stream, PhotonMessageInfo info )
    {
        // Write to others
        if (stream.IsWriting)
        {
            float myValue = myGIS.myValue;
            TerrainGISExample.GISType myType = myGIS.myType;
            stream.SendNext( myValue );
            stream.SendNext( myType );
        }
        // Read from others
        else
        {
            float myValue = (float) stream.ReceiveNext();
            TerrainGISExample.GISType myType = (TerrainGISExample.GISType) stream.ReceiveNext();
            myGIS.UpdateMyValue( myType, myValue );
        }
    }

}
