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
        transform.Rotate( Vector3.up, Time.deltaTime * slewSeconds * ( objectToTrack.eulerAngles.y - transform.eulerAngles.y ) );  
    }
}
