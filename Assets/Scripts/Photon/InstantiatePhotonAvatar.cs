using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class InstantiatePhotonAvatar : MonoBehaviourPunCallbacks
{
    public GameObject avatarPrefab;
    
    public override void OnJoinedRoom()
    {
        GameObject newAvatar = PhotonNetwork.Instantiate( avatarPrefab.name, transform.position, Quaternion.identity );
        newAvatar.transform.parent = transform;
        newAvatar.transform.localPosition = Vector3.zero;
        newAvatar.transform.localRotation = Quaternion.identity;
    }
    
}
