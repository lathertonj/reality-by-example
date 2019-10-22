using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchToComponent : MonoBehaviour
{
    public enum InteractionType { PlaceTerrainImmediate, PlaceTerrainGrowth, PlaceTexture, 
        PlaceTerrainLocalRaiseLower, PlaceTerrainLaserPointerRaiseLower, MoveTeleport, MoveFly };
    public InteractionType switchTo;

    private IEnumerator previousAnimation = null;

    private void OnTriggerEnter( Collider other )
    {
        FlyingTeleporter maybeController = other.GetComponent<FlyingTeleporter>();
        if( maybeController )
        {
            DisableAllInteractors( maybeController.gameObject );
            switch( switchTo )
            {
                case InteractionType.PlaceTerrainImmediate:
                    maybeController.GetComponent<TerrainInteractor>().enabled = true;
                    maybeController.GetComponent<HeightExampleInteractor>().enabled = true;
                    break;
                case InteractionType.PlaceTerrainGrowth:
                    maybeController.GetComponent<TerrainGradualInteractor>().enabled = true;
                    maybeController.GetComponent<HeightExampleInteractor>().enabled = true;
                    break;
                case InteractionType.PlaceTerrainLocalRaiseLower:
                    maybeController.GetComponent<TerrainLocalRaiseLowerInteractor>().enabled = true;
                    maybeController.GetComponent<HeightExampleInteractor>().enabled = true;
                    break;
                case InteractionType.PlaceTerrainLaserPointerRaiseLower:
                    maybeController.GetComponent<TerrainLaserRaiseLowerInteractor>().enabled = true;
                    maybeController.GetComponent<LaserPointerColliderSelector>().enabled = true;
                    maybeController.GetComponent<HeightExampleInteractor>().enabled = true;
                    break;
                case InteractionType.PlaceTexture:
                    maybeController.GetComponent<TerrainTextureInteractor>().enabled = true;
                    maybeController.GetComponent<TextureExampleInteractor>().enabled = true;
                    break;
                default:
                    break;
            }

            // trigger haptic pulse
            maybeController.GetComponent<VibrateController>().Vibrate( 0.05f, 30, 0.8f );

            // animate
            if( previousAnimation != null ) { StopCoroutine( previousAnimation ); }
            previousAnimation = AnimateSwell( 0.14f, 0.4f, 0.03f, 1.3f );
            StartCoroutine( previousAnimation );

        }
    }

    private IEnumerator AnimateSwell( float upSeconds, float upSlew, float downSlew, float increaseSizeBy )
    {
        float startSize = transform.localScale.x;
        float currentSize = startSize;
        float maxSize = startSize * increaseSizeBy;
        float startTime = Time.time;

        while( Time.time < startTime + upSeconds )
        {
            currentSize += ( maxSize - currentSize ) * upSlew;
            transform.localScale = currentSize * Vector3.one;
            yield return null;
        }

        while( currentSize - startSize > 0.001f )
        {
            currentSize += ( startSize - currentSize ) * downSlew;
            transform.localScale = currentSize * Vector3.one;
            yield return null;
        }

        transform.localScale = startSize * Vector3.one;
    }

    private void DisableAllInteractors( GameObject o )
    {
        o.GetComponent<TerrainInteractor>().enabled = false;
        o.GetComponent<TerrainGradualInteractor>().enabled = false;
        o.GetComponent<HeightExampleInteractor>().enabled = false;
        o.GetComponent<TerrainTextureInteractor>().enabled = false;
        o.GetComponent<TextureExampleInteractor>().enabled = false;

        // TODO: this on other component types above
        o.GetComponent<TerrainLocalRaiseLowerInteractor>().Abort();
        o.GetComponent<TerrainLocalRaiseLowerInteractor>().enabled = false;

        o.GetComponent<LaserPointerColliderSelector>().enabled = false;

        o.GetComponent<TerrainLaserRaiseLowerInteractor>().Abort();
        o.GetComponent<TerrainLaserRaiseLowerInteractor>().enabled = false;
    }


    private void OnTriggerStay( Collider other )
    {

    }

    private void OnTriggerExit( Collider other )
    {

    }
}
