using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowColliderLaserEndPoint : MonoBehaviour
{

    private LaserPointerColliderSelector myLaser;
    private bool following = false;
    private Transform objectToMove = null;

    // Start is called before the first frame update
    void Start()
    {
        myLaser = GetComponent<LaserPointerColliderSelector>();    
    }

    // Update is called once per frame
    void Update()
    {
        if( following && myLaser.IsIntersecting() )
        {
            objectToMove.position = myLaser.GetMostRecentIntersectionPoint();
        }
    }

    public void FollowEndPoint( Transform t )
    {
        following = true;
        objectToMove = t;
    }

    public void StopFollowing()
    {
        following = false;
        objectToMove = null;
    }
}
