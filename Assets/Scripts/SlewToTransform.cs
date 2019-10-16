using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlewToTransform : MonoBehaviour
{
    public Transform objectToTrack;
    public float slewSeconds = 1f;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += slewSeconds * Time.deltaTime * ( objectToTrack.position - transform.position );   
    }
}
