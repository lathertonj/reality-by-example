using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlewFollower : MonoBehaviour
{

    public Transform objectToFollow;
    public float positionSlew = 1f, rotationSlew = 1f;
    private Vector3 currentPos, goalPos;
    private Quaternion currentRotation, goalRotation;


    // Use this for initialization
    void Start()
    {
        // set position
        transform.position = currentPos = goalPos = objectToFollow.position;
    }

    // Update is called once per frame
    void Update()
    {
        // slew position and orientation toward object we're following
        goalPos = objectToFollow.position;
        goalRotation = objectToFollow.rotation;

        // compute
        currentPos += positionSlew * Time.deltaTime * ( goalPos - currentPos );
        currentRotation = Quaternion.Slerp( currentRotation, goalRotation, rotationSlew * Time.deltaTime );

        // set
        transform.position = currentPos;
        transform.rotation = currentRotation;
    }

}

