using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchToComponent : MonoBehaviour
{
    public enum InteractionType { PlaceTerrainImmediate, PlaceTerrainGrowth, PlaceTexture, 
        PlaceTerrainLocalRaiseLower, PlaceTerrainLaserPointerRaiseLower, MoveTeleport, MoveFly, MoveGroundFly, MoveFollowCreature,
        PlaceTempo, PlaceTimbre, PlaceDensity, PlaceVolume, PlaceChord,
        PlaceGIS,
        SlowlySpawnPrefab,
        RandomizePerturbSmall, RandomizePerturbBig, RandomizeCopy, RandomizeCurrent, RandomizeAll,
        CreatureCreate, CreatureSelect, CreatureClone, CreatureExampleRecord, CreatureExampleClone, CreatureExampleDelete,
        CreatureConstantTimeMode, CreatureMusicMode };
    public InteractionType switchTo;
    public Transform givenPrefab;

    private IEnumerator previousAnimation = null;
    private Vector3 originalScale;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    void OnEnable()
    {
        transform.localScale = originalScale;
    }

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
            // disable animation interactors
            AnimationActions animationAction = o.GetComponent<AnimationActions>();
            if( animationAction ) { animationAction.DisablePreUIChange(); }
            switch( switchTo )
            {
                case InteractionType.PlaceTerrainImmediate:
                    SetGripPlacePrefab( o );
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
                    // hacky way to distinguish between two laser pointers
                    foreach( LaserPointerColliderSelector l in o.GetComponents<LaserPointerColliderSelector>() )
                    {
                        if( !l.stopShowingOnUp )
                        {
                            l.enabled = true;
                        }
                    }
                    break;
                case InteractionType.PlaceTexture:
                    SetGripPlacePrefab( o );
                    break;
                case InteractionType.PlaceGIS:
                    SetGripPlacePrefab( o );
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
                    SetGripPlacePrefab( o );
                    break;
                case InteractionType.PlaceTimbre:
                    SoundEngine0To1Regressor.Activate( SoundEngine0To1Regressor.timbreRegressor );
                    SetGripPlacePrefab( o );
                    break;
                case InteractionType.PlaceDensity:
                    SoundEngine0To1Regressor.Activate( SoundEngine0To1Regressor.densityRegressor );
                    SetGripPlacePrefab( o );
                    break;
                case InteractionType.PlaceVolume:
                    SoundEngine0To1Regressor.Activate( SoundEngine0To1Regressor.volumeRegressor );
                    SetGripPlacePrefab( o );
                    break;
                case InteractionType.PlaceChord:
                    SoundEngineChordClassifier.Activate();
                    SetGripPlacePrefab( o );
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
                case InteractionType.CreatureCreate:
                case InteractionType.CreatureSelect:
                case InteractionType.CreatureClone:
                case InteractionType.CreatureExampleRecord: 
                case InteractionType.CreatureExampleClone:
                case InteractionType.CreatureExampleDelete:
                case InteractionType.CreatureConstantTimeMode:
                case InteractionType.CreatureMusicMode:
                case InteractionType.MoveFollowCreature:
                    // we have another component for processing animation commands
                    if( animationAction )
                    {
                        animationAction.ProcessUIChange( switchTo, givenPrefab );
                    }
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

    private void SetGripPlacePrefab( GameObject o )
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
        SlewToTransform slew = o.GetComponentInParent<SlewToTransform>();
        slew.objectToTrack = null;
        slew.enabled = false;
    }

    

    private void DisablePlacementInteractors( GameObject o )
    {
        o.GetComponent<GripPlaceDeleteInteraction>().currentPrefabToUse = null;
        // enable the grip deleter; some may disable it later if they need it disabled
        o.GetComponent<GripPlaceDeleteInteraction>().enabled = true;
        
        foreach( LaserPointerColliderSelector l in o.GetComponents<LaserPointerColliderSelector>() )
        {
            l.HideLaser();
            l.enabled = false;
        }

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
        float startSizeMultiplier = 1f;
        float currentSizeMultiplier = startSizeMultiplier;
        float maxSizeMultiplier = currentSizeMultiplier * increaseSizeBy;
        float startTime = Time.time;

        while( Time.time < startTime + upSeconds )
        {
            currentSizeMultiplier += ( maxSizeMultiplier - currentSizeMultiplier ) * upSlew;
            transform.localScale = currentSizeMultiplier * originalScale;
            yield return null;
        }

        // currentSizeMultiplier started at 1
        while( currentSizeMultiplier - startSizeMultiplier > 0.001f )
        {
            currentSizeMultiplier += ( startSizeMultiplier - currentSizeMultiplier ) * downSlew;
            transform.localScale = currentSizeMultiplier * originalScale;
            yield return null;
        }

        transform.localScale = originalScale;
    }

}
