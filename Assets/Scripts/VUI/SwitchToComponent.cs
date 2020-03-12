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

    public static float hintTime = 1.1f;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    void OnEnable()
    {
        transform.localScale = originalScale;
    }


    bool gripInUse = false, triggerInUse = false, touchpadInUse = false;
    public void ActivateMode( GameObject controller )
    {
        // get and disable randomizer
        RandomizeTerrain randomizer = controller.transform.root.GetComponentInChildren<RandomizeTerrain>();
        randomizer.currentAction = RandomizeTerrain.ActionType.DoNothing;
        DisablePlacementInteractors( controller );
        DisableMovementInteractors( controller );
        // disable animation interactors
        AnimationActions animationAction = controller.GetComponent<AnimationActions>();
        if( animationAction ) { animationAction.DisablePreUIChange(); }

        gripInUse = false;
        triggerInUse = false;
        touchpadInUse = false;
        switch( switchTo )
        {
            case InteractionType.PlaceTerrainImmediate:
                SetGripPlacePrefab( controller );
                // also, show height hint
                TerrainHeightExample.ShowHints( hintTime );
                break;
            case InteractionType.PlaceTerrainGrowth:
                EnableComponent<TerrainGradualInteractor>( controller );
                gripInUse = true;
                // also, show height hint
                TerrainHeightExample.ShowHints( hintTime );
                break;
            case InteractionType.PlaceTerrainLocalRaiseLower:
                EnableComponent<TerrainLocalRaiseLowerInteractor>( controller );
                gripInUse = true;
                // also, show height hint
                TerrainHeightExample.ShowHints( hintTime );
                break;
            case InteractionType.PlaceTerrainLaserPointerRaiseLower:
                // enable the components we need
                EnableComponent<TerrainLaserRaiseLowerInteractor>( controller );
                EnableComponent<LaserPointerColliderSelector>( controller );
                touchpadInUse = true;
                // also, show height hint
                TerrainHeightExample.ShowHints( hintTime );
                break;
            case InteractionType.PlaceTexture:
                SetGripPlacePrefab( controller );
                // also, show texture hint
                TerrainTextureExample.ShowHints( hintTime );
                break;
            case InteractionType.PlaceGIS:
                SetGripPlacePrefab( controller );
                // also, show GIS hint
                TerrainGISExample.ShowHints( hintTime );
                break;
            case InteractionType.MoveTeleport:
                EnableComponent<FlyingTeleporter>( controller );
                touchpadInUse = true;
                break;
            case InteractionType.MoveFly:
                EnableComponent<FlyingMovement>( controller );
                touchpadInUse = true;
                break;
            case InteractionType.MoveGroundFly:
                EnableComponent<GroundFlyingMovement>( controller );
                touchpadInUse = true;
                break;
            case InteractionType.PlaceTempo:
                SoundEngineTempoRegressor.Activate();
                SetGripPlacePrefab( controller );
                // also, show tempo hint
                SoundTempoExample.ShowHints( hintTime );
                break;
            case InteractionType.PlaceTimbre:
                SoundEngine0To1Regressor.Activate( SoundEngine0To1Regressor.timbreRegressor );
                SetGripPlacePrefab( controller );
                // also, show timbre hint
                Sound0To1Example.ShowHints( SoundEngine0To1Regressor.timbreRegressor, hintTime );
                break;
            case InteractionType.PlaceDensity:
                SoundEngine0To1Regressor.Activate( SoundEngine0To1Regressor.densityRegressor );
                SetGripPlacePrefab( controller );
                // also, show density hint
                Sound0To1Example.ShowHints( SoundEngine0To1Regressor.densityRegressor, hintTime );
                break;
            case InteractionType.PlaceVolume:
                SoundEngine0To1Regressor.Activate( SoundEngine0To1Regressor.volumeRegressor );
                SetGripPlacePrefab( controller );
                // also, show volume hint
                Sound0To1Example.ShowHints( SoundEngine0To1Regressor.volumeRegressor, hintTime );
                break;
            case InteractionType.PlaceChord:
                SoundEngineChordClassifier.Activate();
                SetGripPlacePrefab( controller );
                // also, show chord hint
                SoundChordExample.ShowHints( hintTime );
                break;
            case InteractionType.SlowlySpawnPrefab:
                EnableComponent<SlowlySpawnPrefab>( controller );
                controller.GetComponent<SlowlySpawnPrefab>().prefabToSpawn = givenPrefab;
                touchpadInUse = true;
                break;
            case InteractionType.RandomizePerturbSmall:
                randomizer.currentAction = RandomizeTerrain.ActionType.PerturbSmall;
                gripInUse = true;
                break;
            case InteractionType.RandomizePerturbBig:
                randomizer.currentAction = RandomizeTerrain.ActionType.PerturbBig;
                gripInUse = true;
                break;
            case InteractionType.RandomizeCopy:
                randomizer.currentAction = RandomizeTerrain.ActionType.Copy;
                gripInUse = true;
                // we need the drag and drop for this one only
                EnableComponent<LaserPointerDragAndDrop>( controller );
                break;
            case InteractionType.RandomizeCurrent:
                randomizer.currentAction = RandomizeTerrain.ActionType.RandomizeCurrent;
                gripInUse = true;
                break;
            case InteractionType.RandomizeAll:
                randomizer.currentAction = RandomizeTerrain.ActionType.RandomizeAll;
                gripInUse = true;
                break;
            case InteractionType.CreatureExampleDelete:
                // be sure to enable grip deletion below
                gripInUse = false;
                break;
            case InteractionType.CreatureCreate:
            case InteractionType.CreatureClone:
            case InteractionType.CreatureExampleRecord: 
            case InteractionType.CreatureExampleClone:
            case InteractionType.CreatureConstantTimeMode:
            case InteractionType.CreatureMusicMode:
            case InteractionType.MoveFollowCreature:
                // we have another component for processing animation commands
                if( animationAction )
                {
                    animationAction.ProcessUIChange( switchTo, givenPrefab );
                }
                // all these things use the grip (TODO except music mode / time mode... might remove those though)
                gripInUse = true;
                break;
            // disabled
            case InteractionType.CreatureSelect:
            default:
                break;
        }

        // trigger haptic pulse
        controller.GetComponent<VibrateController>().Vibrate( 0.05f, 30, 0.8f );

        // animate
        if( previousAnimation != null ) { StopCoroutine( previousAnimation ); }
        previousAnimation = AnimateSwell( 0.14f, 0.4f, 0.03f, 1.3f );
        StartCoroutine( previousAnimation );

        // reenable what we can
        if( !gripInUse )
        {
            EnableComponent<GripPlaceDeleteInteraction>( controller );
            EnableComponent<RemoteGripPlaceDeleteInteraction>( controller );
        }
        if( !touchpadInUse )
        {
            EnableComponent<TouchpadLeftRightClickInteraction>( controller );
            EnableComponent<TouchpadUpDownInteraction>( controller );
            EnableComponent<RemoteTouchpadLeftRightClickInteraction>( controller );
            EnableComponent<RemoteTouchpadUpDownInteraction>( controller );
        }
        if( !triggerInUse )
        {
            EnableComponent<TriggerGrabMoveInteraction>( controller );
            EnableComponent<RemoteTriggerGrabMoveInteraction>( controller );
        }

    }

    private void OnTriggerEnter( Collider other )
    {
        FlyingTeleporter maybeController = other.GetComponent<FlyingTeleporter>();
        if( maybeController )
        {
            ActivateMode( maybeController.gameObject );
        }
    }

    private void SetGripPlacePrefab( GameObject o )
    {
        // do for standard grip
        GripPlaceDeleteInteraction gripPlace = o.GetComponent<GripPlaceDeleteInteraction>();
        if( gripPlace )
        {
            gripPlace.enabled = true;
            gripPlace.currentPrefabToUse = givenPrefab;
        }

        // and for remote grip
        RemoteGripPlaceDeleteInteraction remoteGripPlace = o.GetComponent<RemoteGripPlaceDeleteInteraction>();
        if( remoteGripPlace )
        {
            remoteGripPlace.enabled = true;
            remoteGripPlace.currentPrefabToUse = givenPrefab;
        }

        gripInUse = true;
    }

    private void DisableGripPlacers( GameObject o )
    {
        // do for standard grip
        GripPlaceDeleteInteraction gripPlace = o.GetComponent<GripPlaceDeleteInteraction>();
        if( gripPlace )
        {
            gripPlace.currentPrefabToUse = null;
            gripPlace.enabled = false;
        }

        // and for remote grip
        RemoteGripPlaceDeleteInteraction remoteGripPlace = o.GetComponent<RemoteGripPlaceDeleteInteraction>();
        if( remoteGripPlace )
        {
            remoteGripPlace.currentPrefabToUse = null;
            remoteGripPlace.enabled = false;
        }
    }

    


    private void DisableMovementInteractors( GameObject o )
    {
        DisableComponent<FlyingTeleporter>( o );
        DisableComponent<FlyingMovement>( o );
        DisableComponent<GroundFlyingMovement>( o );

        SlewToTransform slew = o.GetComponentInParent<SlewToTransform>();
        slew.objectToTrack = null;
        slew.enabled = false;
    }

    

    private void DisablePlacementInteractors( GameObject o )
    {
        // set prefab to null and disable grip deleters
        DisableGripPlacers( o );

        // disable others
        DisableComponent<TouchpadLeftRightClickInteraction>( o );
        DisableComponent<TouchpadUpDownInteraction>( o );
        DisableComponent<TriggerGrabMoveInteraction>( o );
        DisableComponent<RemoteTouchpadLeftRightClickInteraction>( o );
        DisableComponent<RemoteTouchpadUpDownInteraction>( o );
        DisableComponent<RemoteTriggerGrabMoveInteraction>( o );
        
        // special terrain height methods
        DisableComponent<TerrainGradualInteractor>( o );
        DisableComponent<TerrainLocalRaiseLowerInteractor>( o );
        DisableComponent<TerrainLaserRaiseLowerInteractor>( o );
        DisableComponent<LaserPointerColliderSelector>( o );
        DisableComponent<SlowlySpawnPrefab>( o );
        DisableComponent<LaserPointerDragAndDrop>( o );

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


    public static void EnableComponent<T>( GameObject o ) where T : MonoBehaviour
    {
        T component = o.GetComponent<T>();
        if( component != null )
        {
            component.enabled = true;
        }
    }
    
    
    public static void DisableComponent<T>( GameObject o ) where T : MonoBehaviour
    {
        T component = o.GetComponent<T>();
        if( component != null )
        {
            component.enabled = false;
        }
    }

}
