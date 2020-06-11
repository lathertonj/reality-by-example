using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickAndFollow : MonoBehaviour
{
    public int mouseButton = 0;
    public LayerMask clickMask;
    private SlewToTransform myFollower;
    private Camera myCamera;

    // Start is called before the first frame update
    void Start()
    {
        myFollower = GetComponent<SlewToTransform>();    
        myCamera = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        if( Input.GetMouseButtonDown( mouseButton ) )
        {
            ProcessClick();
        }
    }

    void ProcessClick()
    {
        RaycastHit hit; 
        Ray ray = myCamera.ScreenPointToRay( Input.mousePosition );
        if ( Physics.Raycast( ray, out hit, Mathf.Infinity, clickMask ) )
        { 
            Transform hitObject = hit.transform.root;
            if( hitObject.gameObject.CompareTag( "Creature" ) )
            {
                // yay! we can now follow a creature
                myFollower.objectToTrack = hitObject;
                myFollower.enabled = true;
                return;
            }
        }
        // either we clicked nothing, or we didn't click a creature
        myFollower.objectToTrack = null;
        myFollower.enabled = false;
    }
}
