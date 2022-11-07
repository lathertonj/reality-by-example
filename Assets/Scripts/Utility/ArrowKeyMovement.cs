using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowKeyMovement : MonoBehaviour
{
    public float movementPerSecond = 3f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 direction = Vector3.zero;
        if( Input.GetKey( KeyCode.LeftArrow ) || Input.GetKey( KeyCode.A ) )
        {
            direction += -transform.right;
        }
        if( Input.GetKey( KeyCode.RightArrow ) || Input.GetKey( KeyCode.D ) )
        {
            direction += transform.right;
        }
        if( Input.GetKey( KeyCode.UpArrow ) || Input.GetKey( KeyCode.W ) )
        {
            direction += transform.forward;
        }
        if( Input.GetKey( KeyCode.DownArrow ) || Input.GetKey( KeyCode.S ) )
        {
            direction += -transform.forward;
        }

        if( direction.magnitude > 0.01f )
        {
            transform.position += movementPerSecond * Time.deltaTime * direction.normalized;
        }
    }
}
