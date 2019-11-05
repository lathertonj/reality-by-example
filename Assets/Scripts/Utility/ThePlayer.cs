using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThePlayer : MonoBehaviour
{
    public static Transform theTransform;
    // Start is called before the first frame update
    void Start()
    {
        theTransform = transform;
    }
}
