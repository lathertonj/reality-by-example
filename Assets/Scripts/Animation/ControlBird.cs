using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlBird : MonoBehaviour
{
    public Transform trackHead, trackLeft, trackRight;
    
    public Transform myHead;
    public DitzelGames.FastIK.FastIKFabric myLeft, myRight;

    private Vector3 headOffset;
    

    // Start is called before the first frame update
    void Start()
    {
        myLeft.Target = trackLeft;
        myRight.Target = trackRight;

        headOffset = transform.position - myHead.position;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = trackHead.position + headOffset;
        transform.rotation = trackHead.rotation;
    }

    void OnDisable()
    {
        // make the left/right arms start tracking something else
        // TODO
    }

    void OnEnable()
    {
        // make the left/right arms start tracking my left/right hands instead
        myLeft.Target = trackLeft;
        myRight.Target = trackRight;
    }
}
