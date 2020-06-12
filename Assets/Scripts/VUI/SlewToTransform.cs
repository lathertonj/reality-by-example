using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlewToTransform : MonoBehaviour
{
    public Transform objectToTrack;
    public float slewSeconds = 1f;

    public bool slewYRotation = true;

    public bool keepDistance = false;
    public float distanceToKeep = 1f;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        transform.position += slewSeconds * Time.deltaTime * ( GoalPosition() - transform.position );
        if( slewYRotation )
        {
            Quaternion goalRotation = Quaternion.AngleAxis( objectToTrack.eulerAngles.y, Vector3.up );
            transform.rotation = Quaternion.Slerp( transform.rotation, goalRotation, slewSeconds * Time.deltaTime );
        }
    }

    Vector3 GoalPosition()
    {
        if( keepDistance )
        {
            // the place to go to is actually most, but not all of the way toward object
            Vector3 objectToMe = transform.position - objectToTrack.position;
            return objectToTrack.position + distanceToKeep * objectToMe.normalized;
        }
        else
        {
            return objectToTrack.position;
        }
    }
}
