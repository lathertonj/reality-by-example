using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UICone : MonoBehaviour
{
    public Transform myShape;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void SetLength( float length )
    {
        Vector3 newScale = myShape.localScale;
        newScale.y = length;
        myShape.localScale = newScale;
    }

    public void SetSize( float size )
    {
        Vector3 newScale = myShape.localScale;
        newScale.x = newScale.z = size;
        myShape.localScale = newScale;
    }
}
