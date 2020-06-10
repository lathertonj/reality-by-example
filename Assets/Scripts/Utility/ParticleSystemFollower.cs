using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSystemFollower : MonoBehaviour
{

    public Transform objectToFollow;
    public bool followPosition = true;
    public bool followRotation = false;
    private ParticleSystemRenderer myRenderer;

    void Awake()
    {
        myRenderer = GetComponent<ParticleSystemRenderer>();
    }


    void LateUpdate()
    {
        if( objectToFollow != null )
        {
            if( objectToFollow.gameObject.activeInHierarchy )
            {
                // show if the other object is showing
                myRenderer.enabled = true;

                // exactly copy position
                if( followPosition )
                {
                    transform.position = objectToFollow.position;
                }

                // exactly copy rotation
                if( followRotation )
                {
                    transform.rotation = objectToFollow.rotation;
                }
            }
            else
            {
                // hide if the other object is hidden
                myRenderer.enabled = false;
            }
        }
    }

}

