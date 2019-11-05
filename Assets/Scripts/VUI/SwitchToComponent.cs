using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchToComponent : MonoBehaviour
{
    public enum InteractionType { PlaceTerrainImmediate, PlaceTerrainGrowth, PlaceTexture, 
        PlaceTerrainLocalRaiseLower, PlaceTerrainLaserPointerRaiseLower, MoveTeleport, MoveFly, MoveGroundFly,
        PlaceTempo, PlaceTimbre, PlaceChord };
    public InteractionType switchTo;
    public Transform givenPrefab;

    private IEnumerator previousAnimation = null;

    private void OnTriggerEnter( Collider other )
    {
        FlyingTeleporter maybeController = other.GetComponent<FlyingTeleporter>();
        if( maybeController )
        {
            GameObject o = maybeController.gameObject;
            DisablePlacementInteractors( o );
            switch( switchTo )
            {
                case InteractionType.PlaceTerrainImmediate:
                    o.GetComponent<TerrainInteractor>().enabled = true;
                    o.GetComponent<HeightExampleInteractor>().enabled = true;
                    break;
                case InteractionType.PlaceTerrainGrowth:
                    o.GetComponent<TerrainGradualInteractor>().enabled = true;
                    o.GetComponent<HeightExampleInteractor>().enabled = true;
                    break;
                case InteractionType.PlaceTerrainLocalRaiseLower:
                    o.GetComponent<TerrainLocalRaiseLowerInteractor>().enabled = true;
                    o.GetComponent<HeightExampleInteractor>().enabled = true;
                    break;
                case InteractionType.PlaceTerrainLaserPointerRaiseLower:
                    // disable movement interactors because this one uses its own laser pointer
                    DisableMovementInteractors( o );

                    // enable the components we need
                    o.GetComponent<TerrainLaserRaiseLowerInteractor>().enabled = true;
                    o.GetComponent<LaserPointerColliderSelector>().enabled = true;
                    o.GetComponent<HeightExampleInteractor>().enabled = true;
                    break;
                case InteractionType.PlaceTexture:
                    o.GetComponent<TerrainTextureInteractor>().enabled = true;
                    o.GetComponent<TextureExampleInteractor>().enabled = true;
                    break;
                case InteractionType.MoveTeleport:
                    DisableMovementInteractors( o );
                    o.GetComponent<FlyingTeleporter>().enabled = true;
                    break;
                case InteractionType.MoveFly:
                    DisableMovementInteractors( o );
                    o.GetComponent<FlyingMovement>().enabled = true;
                    break;
                case InteractionType.MoveGroundFly:
                    DisableMovementInteractors( o );
                    o.GetComponent<GroundFlyingMovement>().enabled = true;
                    break;
                case InteractionType.PlaceTempo:
                    DisableMovementInteractors( o );
                    SoundEngineTempoRegressor.Activate();
                    GripPlaceDeleteInteraction.currentPrefabToUse = givenPrefab;
                    break;
                case InteractionType.PlaceTimbre:
                    DisableMovementInteractors( o );
                    SoundEngineTimbreRegressor.Activate();
                    GripPlaceDeleteInteraction.currentPrefabToUse = givenPrefab;
                    break;
                case InteractionType.PlaceChord:
                    DisableMovementInteractors( o );
                    SoundEngineChordClassifier.Activate();
                    GripPlaceDeleteInteraction.currentPrefabToUse = givenPrefab;
                    break;
                default:
                    break;
            }

            // trigger haptic pulse
            o.GetComponent<VibrateController>().Vibrate( 0.05f, 30, 0.8f );

            // animate
            if( previousAnimation != null ) { StopCoroutine( previousAnimation ); }
            previousAnimation = AnimateSwell( 0.14f, 0.4f, 0.03f, 1.3f );
            StartCoroutine( previousAnimation );

        }
    }

    private void DisableMovementInteractors( GameObject o )
    {
        o.GetComponent<FlyingTeleporter>().HideLasers();
        o.GetComponent<FlyingTeleporter>().enabled = false;
        o.GetComponent<FlyingMovement>().HideLasers();
        o.GetComponent<FlyingMovement>().enabled = false;
        o.GetComponent<GroundFlyingMovement>().HideLasers();
        o.GetComponent<GroundFlyingMovement>().enabled = false;
    }

    

    private void DisablePlacementInteractors( GameObject o )
    {
        o.GetComponent<TerrainInteractor>().enabled = false;
        o.GetComponent<HeightExampleInteractor>().enabled = false;
        o.GetComponent<TerrainTextureInteractor>().enabled = false;
        o.GetComponent<TextureExampleInteractor>().enabled = false;

        o.GetComponent<LaserPointerColliderSelector>().HideLaser();
        o.GetComponent<LaserPointerColliderSelector>().enabled = false;

        o.GetComponent<TerrainGradualInteractor>().Abort();
        o.GetComponent<TerrainGradualInteractor>().enabled = false;
        
        o.GetComponent<TerrainLocalRaiseLowerInteractor>().Abort();
        o.GetComponent<TerrainLocalRaiseLowerInteractor>().enabled = false;

        o.GetComponent<TerrainLaserRaiseLowerInteractor>().Abort();
        o.GetComponent<TerrainLaserRaiseLowerInteractor>().enabled = false;

        SoundEngineTempoRegressor.Deactivate();
        SoundEngineTimbreRegressor.Deactivate();
        SoundEngineChordClassifier.Deactivate();
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

}
