using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimateHint
{
    public static IEnumerator AnimateHintFade( MeshRenderer hint, float pauseTimeBeforeFade )
    {
        // reset orientation to vertical
        hint.transform.rotation = Quaternion.identity;

        // colors
        float goalAlpha = 0, currentAlpha = 1, alphaSlew = 0.15f;
        Color baseColor = hint.material.color;
        // reset to full opaque
        baseColor.a = currentAlpha;
        Color currentColor = baseColor;
        hint.material.color = currentColor;
        hint.enabled = true; 

        // initial pause
        yield return new WaitForSecondsRealtime( pauseTimeBeforeFade );

        while( currentAlpha > 0.01f )
        {
            currentAlpha += alphaSlew * ( goalAlpha - currentAlpha );
            currentColor.a = currentAlpha;
            hint.material.color = currentColor;

            yield return new WaitForSecondsRealtime( 0.05f );
        }
        hint.enabled = false;
        hint.material.color = baseColor;
    }
}
