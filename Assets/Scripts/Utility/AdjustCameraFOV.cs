using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdjustCameraFOV : MonoBehaviour
{
    public string keyToSwitch;
    public float minFOV;
    public float maxFOV;
    public float slewSeconds;
    private float currentFOV, goalFOV;
    private bool usingMaxFOV;
    private Camera myCamera;
    
    void Start()
    {
        currentFOV = goalFOV = maxFOV;
        usingMaxFOV = true;
        myCamera = GetComponent<Camera>();
    }

    void Update()
    {
        // switch fovs?
        if( Input.GetKeyDown( keyToSwitch ) )
        {
            usingMaxFOV = !usingMaxFOV;
            goalFOV = usingMaxFOV ? maxFOV : minFOV;
        }
        
        // smooth transition
        currentFOV += slewSeconds * Time.deltaTime * ( goalFOV - currentFOV );
        myCamera.fieldOfView = currentFOV;
    }
}
