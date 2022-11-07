using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatedCreatureColor : MonoBehaviour , TouchpadUpDownInteractable
{

    public Renderer creatureRenderer;
    public string materialColorProperty;
    public float sensitivity = 1f;

    private Color startColor;
    private float startH, startS, startV;
    private float currentH;
    public bool enforceIdenticalGroupColor = true;
    private AnimationByRecordedExampleController myAnimator;


    // Start is called before the first frame update
    void Awake()
    {
        for( int i = 0; i < creatureRenderer.materials.Length; i++ )
        {
            if( creatureRenderer.materials[i].HasProperty( materialColorProperty ) )
            {
                startColor = creatureRenderer.materials[i].GetColor( materialColorProperty );
            }
        }
        Color.RGBToHSV( startColor, out startH, out startS, out startV );
        currentH = startH;

        myAnimator = GetComponent<AnimationByRecordedExampleController>();
    }

    public void CopyColor( AnimatedCreatureColor other )
    {
        UpdateColor( other.currentH );
    }

    public void UpdateColor( float newH )
    {
        // boundary conditions
        while( newH > 1 ) { newH--; }
        while( newH < 0 ) { newH++; }

        // store and propagate to material
        currentH = newH;
        Color currentColor = Color.HSVToRGB( currentH, startS, startV );
        for( int i = 0; i < creatureRenderer.materials.Length; i++ )
        {
            if( creatureRenderer.materials[i].HasProperty( materialColorProperty ) )
            {
                creatureRenderer.materials[i].SetColor( materialColorProperty, currentColor );
            }
        }
    }

    void TouchpadUpDownInteractable.InformOfUpOrDownMovement( float verticalDisplacementSinceBeginning, float verticalDisplacementThisFrame )
    {
        // change in H
        float changeH = verticalDisplacementThisFrame.MapClamp( -0.1f, 0.1f, 0.05f * sensitivity, -0.05f * sensitivity );

        // change my color
        float newH = currentH + changeH;
        UpdateColor( newH );

        // change my group's color too
        if( enforceIdenticalGroupColor )
        {
            myAnimator.UpdateColor( newH );
        }
    }
    
    void TouchpadUpDownInteractable.FinalizeMovement()
    {
        // don't respond any further when the gesture stops
    }

    public float Serialize()
    {
        return currentH;
    }

    public void Deserialize( float serial )
    {
        UpdateColor( serial );
    }
}
