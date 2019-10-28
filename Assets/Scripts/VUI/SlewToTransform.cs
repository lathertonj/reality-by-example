using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlewToTransform : MonoBehaviour
{
    public Transform objectToTrack;
    public float slewSeconds = 1f;

    public bool slewYRotation = true;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        transform.position += slewSeconds * Time.deltaTime * ( objectToTrack.position - transform.position );
        if( slewYRotation )
        {
            Quaternion goalRotation = Quaternion.AngleAxis( objectToTrack.eulerAngles.y, Vector3.up );
            transform.rotation = Quaternion.Slerp( transform.rotation, goalRotation, slewSeconds * Time.deltaTime );
        }
    }
}
