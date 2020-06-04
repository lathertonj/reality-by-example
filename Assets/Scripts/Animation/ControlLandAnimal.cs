using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlLandAnimal : MonoBehaviour , NeckRotatable
{
    public Transform trackHead, trackLeft, trackRight, trackBackLeft, trackBackRight;
    
    public Transform myHead;
    public DitzelGames.FastIK.FastIKFabric myLeft, myRight, myBackLeft, myBackRight;
    public Transform neckJoint;
    private Quaternion goalNeckRotation;

    public int frontToBackFrameDelay = 20;

    private Queue<Vector3> leftFrontToBackMemory, rightFrontToBackMemory;

    private Vector3 headOffset;

    public bool mirrorBackToFront = true;
    public bool reverseBackToFrontDirection = true;
    private float initialFrontLeftZ, initialFrontRightZ, initialBackLeftZ, initialBackRightZ;
    private int frontBackMultiplier;
    
    private float startHeadOrientation;

    public bool unparentBackLegs = true;

    // Start is called before the first frame update
    void Start()
    {
        // remember
        initialFrontLeftZ  = LocalZ( trackLeft );
        initialFrontRightZ = LocalZ( trackRight );
        initialBackLeftZ   = LocalZ( trackBackLeft );
        initialBackRightZ  = LocalZ( trackBackRight );

        // compute
        frontBackMultiplier = reverseBackToFrontDirection ? -1 : 1;

        // unparent (others should be unparented by animation component)
        if( unparentBackLegs )
        {
            trackBackLeft.parent = null;
            trackBackRight.parent = null;
        }

        // set rotation to identity for all
        // confusingly, the target rotation is interpreted in the
        // local space, even though the target position is
        // interpreted in the global space
        trackLeft.rotation = trackRight.rotation = trackBackLeft.rotation = trackBackRight.rotation = Quaternion.identity;
        startHeadOrientation = trackHead.rotation.eulerAngles.y;

        // assign
        myLeft.Target = trackLeft;
        myRight.Target = trackRight;
        myBackLeft.Target = trackBackLeft;
        myBackRight.Target = trackBackRight;

        headOffset = transform.position - myHead.position;

        leftFrontToBackMemory = new Queue<Vector3>();
        rightFrontToBackMemory = new Queue<Vector3>();

        // set to identity
        neckJoint.localRotation = goalNeckRotation = Quaternion.identity;

    }

    // Update is called once per frame
    void Update()
    {
        // follow the head position and rotation
        transform.position = trackHead.position + headOffset;
        transform.rotation = trackHead.rotation;

        // rotate the neck toward desired rotation
        neckJoint.localRotation = Quaternion.Slerp( neckJoint.localRotation, goalNeckRotation, 0.25f );

        // orient paws still forward
        trackLeft.rotation = trackRight.rotation = trackBackLeft.rotation = trackBackRight.rotation = GetFootOrientation();

        // animate back legs from memory
        if( mirrorBackToFront )
        {
            leftFrontToBackMemory.Enqueue( transform.InverseTransformPoint( trackLeft.position ) );
            rightFrontToBackMemory.Enqueue( transform.InverseTransformPoint( trackRight.position ) );
            if( leftFrontToBackMemory.Count >= frontToBackFrameDelay )
            {
                // same as front, but...
                Vector3 localBackLeft = leftFrontToBackMemory.Dequeue();
                // reverse z and put it centered on original back foot position
                localBackLeft.z = frontBackMultiplier * ( localBackLeft.z - initialFrontLeftZ ) + initialBackLeftZ;
                // same for right
                Vector3 localBackRight = rightFrontToBackMemory.Dequeue();
                localBackRight.z = frontBackMultiplier * ( localBackRight.z - initialFrontRightZ ) + initialBackRightZ;
                // move
                trackBackLeft.position = transform.TransformPoint( localBackLeft );
                trackBackRight.position = transform.TransformPoint( localBackRight );
            }
        }
    }

    Quaternion GetFootOrientation()
    {
        return Quaternion.AngleAxis( trackHead.rotation.eulerAngles.y - startHeadOrientation, Vector3.up );
    }

    void NeckRotatable.SetNeckRotation( Quaternion r )
    {
        goalNeckRotation = r; 
    }

    float LocalZ( Transform t )
    {
        return transform.InverseTransformPoint( t.position ).z;
    }
}

public interface NeckRotatable
{
    void SetNeckRotation( Quaternion r );
}
