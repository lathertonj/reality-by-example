using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RightClickRotation : MonoBehaviour
{
    public int whichButton = 1;
    public float yRate, xRate;

    private bool dragInProgress = false;
    private Vector2 startPos;
    private Quaternion baseRotation;
    // Start is called before the first frame update
    void Start()
    {
        baseRotation = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 currentPos = GetMousePos();
        if( dragInProgress )
        {
            Vector2 diff = currentPos - startPos;
            Quaternion yaw = Quaternion.AngleAxis( diff.x.MapClamp( -1, 1, yRate, -yRate ), Vector3.up );
            Quaternion pitch = Quaternion.AngleAxis( diff.y.MapClamp( -1, 1, -xRate, xRate ), Vector3.right );
            transform.rotation = yaw * baseRotation * pitch;

            // // wtf. ensure that z is always 0.
            // Vector3 result = transform.rotation.eulerAngles;
            // result.z = 0;
            // transform.eulerAngles = result;
        }


        if( Input.GetMouseButtonDown( whichButton ) )
        {
            dragInProgress = true;
            startPos = currentPos;
            baseRotation = transform.rotation;
        }

        if( Input.GetMouseButtonUp( whichButton ) )
        {
            dragInProgress = false;
        }
    }

    Vector2 GetMousePos()
    {
        return new Vector2(
            Input.mousePosition.x.MapClamp( 0, Screen.width, 0, 1 ),
            Input.mousePosition.y.MapClamp( 0, Screen.height, 0, 1 )
        );
    }
}
