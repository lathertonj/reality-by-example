﻿using System.Collections;
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
        if( randomizer != null )
        {
            randomizer.SetGripAction( RandomizeTerrain.ActionType.DoNothing, controller );
        }
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
                EnableComponent<SnapTurn>( controller );
                touchpadInUse = true;
                break;
            case InteractionType.MoveFly:
                EnableComponent<FlyingMovement>( controller );
                EnableComponent<SnapTurn>( controller );
                touchpadInUse = true;
                break;
            case InteractionType.MoveGroundFly:
                EnableComponent<GroundFlyingMovement>( controller );
                EnableComponent<SnapTurn>( controller );
                touchpadInUse = true;
                break;
            case InteractionType.PlaceTempo:
                SetGripPlacePrefab( controller );
                // also, show tempo hint
                SoundTempoExample.ShowHints( hintTime );
                break;
            case InteractionType.PlaceTimbre:
                SetGripPlacePrefab( controller );
                // also, show timbre hint
                Sound0To1Example.ShowHints( SoundEngine0To1Regressor.timbreRegressor, hintTime );
                break;
            case InteractionType.PlaceDensity:
                SetGripPlacePrefab( controller );
                // also, show density hint
                Sound0To1Example.ShowHints( SoundEngine0To1Regressor.densityRegressor, hintTime );
                break;
            case InteractionType.PlaceVolume:
                SetGripPlacePrefab( controller );
                // also, show volume hint
                Sound0To1Example.ShowHints( SoundEngine0To1Regressor.volumeRegressor, hintTime );
                break;
            case InteractionType.PlaceChord:
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
                if( randomizer != null ) 
                { 
                    randomizer.SetGripAction( RandomizeTerrain.ActionType.PerturbSmall, controller );
                    gripInUse = true;
                }
                break;
            case InteractionType.RandomizePerturbBig:
                if( randomizer != null ) 
                {
                    randomizer.SetGripAction( RandomizeTerrain.ActionType.PerturbBig, controller );
                    gripInUse = true;
                }
                break;
            case InteractionType.RandomizeCopy:
                if( randomizer != null ) 
                { 
                    randomizer.SetGripAction( RandomizeTerrain.ActionType.Copy, controller );
                    gripInUse = true;
                    // we need the drag and drop for this one only
                    EnableComponent<LaserPointerDragAndDrop>( controller );
                }
                break;
            case InteractionType.RandomizeCurrent:
                if( randomizer != null ) 
                { 
                    randomizer.SetGripAction( RandomizeTerrain.ActionType.RandomizeCurrent, controller );
                    gripInUse = true;
                }
                break;
            case InteractionType.RandomizeAll:
                if( randomizer != null ) 
                { 
                    randomizer.SetGripAction( RandomizeTerrain.ActionType.RandomizeAll, controller );
                    gripInUse = true;
                }
                break;
            case InteractionType.CreatureExampleDelete:
                // enable grip deletion
                EnableComponent<GripPlaceDeleteInteraction>( controller );
                EnableComponent<RemoteGripPlaceDeleteInteraction>( controller );

                // remote grip needs delete manually enabled so that it's not enabled otherwise
                RemoteGripPlaceDeleteInteraction remoteGrip = controller.GetComponent<RemoteGripPlaceDeleteInteraction>();
                if( remoteGrip ) { remoteGrip.isDeleteEnabled = true; }

                gripInUse = true;
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

                // annoyingly there is a special case for just this one
                if( switchTo == InteractionType.MoveFollowCreature )
                {
                    EnableComponent<SnapTurn>( controller );
                    touchpadInUse = true;
                }
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
            EnableTouchpadPrimaryInteractors( controller );
        }
        if( !triggerInUse )
        {
            EnableComponent<TriggerGrabMoveInteraction>( controller );
            
            RemoteTriggerGrabMoveInteraction remoteTrigger = controller.GetComponent<RemoteTriggerGrabMoveInteraction>();
            if( remoteTrigger != null )
            {
                remoteTrigger.enabled = true;
                remoteTrigger.touchpadMovingEnabled = !touchpadInUse;
            }
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
            remoteGripPlace.isDeleteEnabled = false;
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
            remoteGripPlace.isDeleteEnabled = false;
            remoteGripPlace.enabled = false;
        }
    }

    


    private void DisableMovementInteractors( GameObject o )
    {
        DisableComponent<FlyingTeleporter>( o );
        DisableComponent<FlyingMovement>( o );
        DisableComponent<GroundFlyingMovement>( o );
        DisableComponent<SnapTurn>( o );

        SlewToTransform slew = o.GetComponentInParent<SlewToTransform>();
        slew.objectToTrack = null;
        slew.enabled = false;
    }

    public static void DisableTouchpadAuxiliaryInteractors( GameObject o )
    {
        // movement
        DisableComponent<FlyingTeleporter>( o );
        DisableComponent<FlyingMovement>( o );
        DisableComponent<GroundFlyingMovement>( o );
        DisableComponent<SnapTurn>( o );

        // placing
        DisableComponent<SlowlySpawnPrefab>( o );
        DisableComponent<TerrainLaserRaiseLowerInteractor>( o );
        DisableComponent<LaserPointerColliderSelector>( o );
    }

    public static void EnableTouchpadPrimaryInteractors( GameObject o )
    {
        EnableComponent<TouchpadLeftRightClickInteraction>( o );
        EnableComponent<TouchpadUpDownInteraction>( o );
        EnableComponent<RemoteTouchpadLeftRightClickInteraction>( o );
        EnableComponent<RemoteTouchpadUpDownInteraction>( o );
        EnableComponent<RemoteTouchpadUpDownScrollInteraction>( o );
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
        DisableComponent<RemoteTouchpadUpDownScrollInteraction>( o );
        DisableComponent<RemoteTriggerGrabMoveInteraction>( o );
        
        // special terrain height methods
        DisableComponent<TerrainGradualInteractor>( o );
        DisableComponent<TerrainLocalRaiseLowerInteractor>( o );
        DisableComponent<TerrainLaserRaiseLowerInteractor>( o );
        DisableComponent<LaserPointerColliderSelector>( o );
        DisableComponent<SlowlySpawnPrefab>( o );
        DisableComponent<LaserPointerDragAndDrop>( o );
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
