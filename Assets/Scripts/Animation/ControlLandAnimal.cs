using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlLandAnimal : MonoBehaviour
{
    public Transform trackHead, trackLeft, trackRight, trackBackLeft, trackBackRight;
    
    public Transform myHead;
    public DitzelGames.FastIK.FastIKFabric myLeft, myRight, myBackLeft, myBackRight;

    public int frontToBackFrameDelay = 20;
    public float frontToBackOffset;

    private Queue<Vector3> leftFrontToBackMemory, rightFrontToBackMemory;

    private Vector3 headOffset;
    

    // Start is called before the first frame update
    void Start()
    {
        // unparent (others should be unparented by animation component)
        trackBackLeft.parent = null;
        trackBackRight.parent = null;

        // assign
        myLeft.Target = trackLeft;
        myRight.Target = trackRight;
        myBackLeft.Target = trackBackLeft;
        myBackRight.Target = trackBackRight;

        headOffset = transform.position - myHead.position;

        leftFrontToBackMemory = new Queue<Vector3>();
        rightFrontToBackMemory = new Queue<Vector3>();

    }

    // Update is called once per frame
    void Update()
    {
        transform.position = trackHead.position + headOffset;
        transform.rotation = trackHead.rotation;

        trackLeft.rotation = trackHead.rotation;
        trackRight.rotation = trackHead.rotation;
        trackBackLeft.rotation = trackHead.rotation;
        trackBackRight.rotation = trackHead.rotation;

        // animate back legs from memory
        leftFrontToBackMemory.Enqueue( transform.InverseTransformPoint( trackLeft.position ) );
        rightFrontToBackMemory.Enqueue( transform.InverseTransformPoint( trackRight.position ) );
        if( leftFrontToBackMemory.Count >= frontToBackFrameDelay )
        {
            trackBackLeft.position = transform.TransformPoint( leftFrontToBackMemory.Dequeue() ) + transform.forward * frontToBackOffset * transform.localScale.z;
            trackBackRight.position = transform.TransformPoint( rightFrontToBackMemory.Dequeue() ) + transform.forward * frontToBackOffset * transform.localScale.z;
        }
    }
}
