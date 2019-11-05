using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FacePlayer : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine( FaceTheTransform() );
    }

    IEnumerator FaceTheTransform()
    {
        float updateTime = Random.Range( 0.05f, 0.1f );
        float damping = 0.5f;

        while( true )
        {
            yield return new WaitForSecondsRealtime( updateTime );

            var lookPos = ThePlayer.theTransform.position - transform.position;
            lookPos.y = 0;
            var rotation = Quaternion.LookRotation( lookPos );
            transform.rotation = Quaternion.Slerp( transform.rotation, rotation, damping );
        }
    }

}
