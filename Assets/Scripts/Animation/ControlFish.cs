using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlFish : MonoBehaviour , NeckRotatable
{
    public Transform trackHead, trackLeft, trackRight;
    
    public Transform myHead;
    public DitzelGames.FastIK.FastIKFabric myLeft, myRight;
    public Transform neckJoint;
    public float percentageToRotateNeck = 0.3f;
    private Quaternion goalNeckRotation, currentNeckRotation;
    private Quaternion baseNeckRotation;


    private Vector3 headOffset;
    
    private float startHeadOrientation;

    // Start is called before the first frame update
    void Start()
    {

        // set rotation to identity for all
        // confusingly, the target rotation is interpreted in the
        // local space, even though the target position is
        // interpreted in the global space
        trackLeft.rotation = trackRight.rotation = Quaternion.identity;
        startHeadOrientation = trackHead.rotation.eulerAngles.y;

        // assign
        myLeft.Target = trackLeft;
        myRight.Target = trackRight;
        headOffset = transform.position - myHead.position;

        // set to identity * base
        baseNeckRotation = neckJoint.localRotation;
        goalNeckRotation = currentNeckRotation = Quaternion.identity;
    }

    // Update is called once per frame
    void Update()
    {
        // follow the head position and rotation
        transform.position = trackHead.position + headOffset;
        transform.rotation = trackHead.rotation;

        // rotate the neck toward desired rotation
        currentNeckRotation = Quaternion.Slerp( currentNeckRotation, goalNeckRotation, 0.25f );
        neckJoint.localRotation = Quaternion.Slerp( Quaternion.identity, currentNeckRotation, percentageToRotateNeck ) * baseNeckRotation;

        // orient forward
        trackLeft.rotation = trackRight.rotation = GetFinOrientation();
    }

    Quaternion GetFinOrientation()
    {
        return Quaternion.AngleAxis( trackHead.rotation.eulerAngles.y - startHeadOrientation, Vector3.up );
    }

    void NeckRotatable.SetNeckRotation( Quaternion r )
    {
        goalNeckRotation = r; 
    }
}
