using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchToComponent : MonoBehaviour
{
    public enum InteractionType { PlaceTerrainImmediate, PlaceTerrainGrowth, PlaceTexture, 
        PlaceTerrainLocalRaiseLower, PlaceTerrainLaserPointerRaiseLower, MoveTeleport, MoveFly, MoveGroundFly,
        PlaceTempo, PlaceTimbre, PlaceDensity, PlaceVolume, PlaceChord,
        PlaceGIS,
        SlowlySpawnPrefab,
        RandomizePerturbSmall, RandomizePerturbBig, RandomizeCopy, RandomizeCurrent, RandomizeAll };
    public InteractionType switchTo;
    public Transform givenPrefab;

    private IEnumerator previousAnimation = null;

    private void OnTriggerEnter( Collider other )
    {
        FlyingTeleporter maybeController = other.GetComponent<FlyingTeleporter>();
        if( maybeController )
        {
            GameObject o = maybeController.gameObject;
            // get and disable randomizer
            RandomizeTerrain randomizer = o.transform.root.GetComponentInChildren<RandomizeTerrain>();
            randomizer.currentAction = RandomizeTerrain.ActionType.DoNothing;
            DisablePlacementInteractors( o );
            DisableMovementInteractors( o );
            switch( switchTo )
            {
                case InteractionType.PlaceTerrainImmediate:
                    SetPrefab( o );
                    break;
                case InteractionType.PlaceTerrainGrowth:
                    o.GetComponent<TerrainGradualInteractor>().enabled = true;
                    break;
                case InteractionType.PlaceTerrainLocalRaiseLower:
                    o.GetComponent<TerrainLocalRaiseLowerInteractor>().enabled = true;
                    break;
                case InteractionType.PlaceTerrainLaserPointerRaiseLower:
                    // enable the components we need
                    o.GetComponent<TerrainLaserRaiseLowerInteractor>().enabled = true;
                    o.GetComponent<LaserPointerColliderSelector>().enabled = true;
                    break;
                case InteractionType.PlaceTexture:
                    SetPrefab( o );
                    break;
                case InteractionType.PlaceGIS:
                    SetPrefab( o );
                    break;
                case InteractionType.MoveTeleport:
                    o.GetComponent<FlyingTeleporter>().enabled = true;
                    break;
                case InteractionType.MoveFly:
                    o.GetComponent<FlyingMovement>().enabled = true;
                    break;
                case InteractionType.MoveGroundFly:
                    o.GetComponent<GroundFlyingMovement>().enabled = true;
                    break;
                case InteractionType.PlaceTempo:
                    SoundEngineTempoRegressor.Activate();
                    SetPrefab( o );
                    break;
                case InteractionType.PlaceTimbre:
                    SoundEngine0To1Regressor.Activate( SoundEngine0To1Regressor.timbreRegressor );
                    SetPrefab( o );
                    break;
                case InteractionType.PlaceDensity:
                    SoundEngine0To1Regressor.Activate( SoundEngine0To1Regressor.densityRegressor );
                    SetPrefab( o );
                    break;
                case InteractionType.PlaceVolume:
                    SoundEngine0To1Regressor.Activate( SoundEngine0To1Regressor.volumeRegressor );
                    SetPrefab( o );
                    break;
                case InteractionType.PlaceChord:
                    SoundEngineChordClassifier.Activate();
                    SetPrefab( o );
                    break;
                case InteractionType.SlowlySpawnPrefab:
                    o.GetComponent<SlowlySpawnPrefab>().enabled = true;
                    o.GetComponent<SlowlySpawnPrefab>().prefabToSpawn = givenPrefab;
                    break;
                case InteractionType.RandomizePerturbSmall:
                    randomizer.currentAction = RandomizeTerrain.ActionType.PerturbSmall;
                    break;
                case InteractionType.RandomizePerturbBig:
                    randomizer.currentAction = RandomizeTerrain.ActionType.PerturbBig;
                    break;
                case InteractionType.RandomizeCopy:
                    randomizer.currentAction = RandomizeTerrain.ActionType.Copy;
                    // we need the drag and drop for this one only
                    o.GetComponent<LaserPointerDragAndDrop>().enabled = true;
                    break;
                case InteractionType.RandomizeCurrent:
                    randomizer.currentAction = RandomizeTerrain.ActionType.RandomizeCurrent;
                    break;
                case InteractionType.RandomizeAll:
                    randomizer.currentAction = RandomizeTerrain.ActionType.RandomizeAll;
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

    private void SetPrefab( GameObject o )
    {
        o.GetComponent<GripPlaceDeleteInteraction>().currentPrefabToUse = givenPrefab;
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
        o.GetComponent<GripPlaceDeleteInteraction>().currentPrefabToUse = null;
        
        o.GetComponent<LaserPointerColliderSelector>().HideLaser();
        o.GetComponent<LaserPointerColliderSelector>().enabled = false;

        o.GetComponent<TerrainGradualInteractor>().Abort();
        o.GetComponent<TerrainGradualInteractor>().enabled = false;
        
        o.GetComponent<TerrainLocalRaiseLowerInteractor>().Abort();
        o.GetComponent<TerrainLocalRaiseLowerInteractor>().enabled = false;

        o.GetComponent<TerrainLaserRaiseLowerInteractor>().Abort();
        o.GetComponent<TerrainLaserRaiseLowerInteractor>().enabled = false;

        o.GetComponent<SlowlySpawnPrefab>().enabled = false;

        LaserPointerDragAndDrop maybeDragDrop = o.GetComponent<LaserPointerDragAndDrop>();
        if( maybeDragDrop ) { maybeDragDrop.enabled = false; }

        SoundEngineTempoRegressor.Deactivate();
        SoundEngineChordClassifier.Deactivate();
        SoundEngine0To1Regressor.Deactivate( SoundEngine0To1Regressor.timbreRegressor );
        SoundEngine0To1Regressor.Deactivate( SoundEngine0To1Regressor.densityRegressor );
        SoundEngine0To1Regressor.Deactivate( SoundEngine0To1Regressor.volumeRegressor );
    }

    private IEnumerator AnimateSwell( float upSeconds, float upSlew, float downSlew, float increaseSizeBy )
    {
        Vector3 startSize = transform.localScale;
        float startSizeMultiplier = 1f;
        float currentSizeMultiplier = startSizeMultiplier;
        float maxSizeMultiplier = currentSizeMultiplier * increaseSizeBy;
        float startTime = Time.time;

        while( Time.time < startTime + upSeconds )
        {
            currentSizeMultiplier += ( maxSizeMultiplier - currentSizeMultiplier ) * upSlew;
            transform.localScale = currentSizeMultiplier * startSize;
            yield return null;
        }

        // currentSizeMultiplier started at 1
        while( currentSizeMultiplier - startSizeMultiplier > 0.001f )
        {
            currentSizeMultiplier += ( startSizeMultiplier - currentSizeMultiplier ) * downSlew;
            transform.localScale = currentSizeMultiplier * startSize;
            yield return null;
        }

        transform.localScale = startSize;
    }

}
