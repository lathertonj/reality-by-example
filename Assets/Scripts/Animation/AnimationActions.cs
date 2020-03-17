using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class AnimationActions : MonoBehaviour
{

    public SteamVR_Input_Sources hand;
    public SteamVR_Action_Boolean actionButton;
    public SteamVR_Action_Boolean selectionButton;
    private SteamVR_Behaviour_Pose controller;
    private VibrateController vibration;



    private AnimationByRecordedExampleController selectedCreature = null;
    private AnimationByRecordedExampleController previouslySelectedCreature = null;

    public enum CurrentAction{ Clone, Nothing };

    public CurrentAction currentAction = CurrentAction.Nothing;
    public enum SelectionResponse{ FollowSelected, PrepareForRecording, Nothing };
    private SelectionResponse currentSelectionResponse = SelectionResponse.Nothing;

    public float creationDistance = 1.5f;

    // Start is called before the first frame update
    void Start()
    {
        vibration = GetComponent<VibrateController>();
        controller = GetComponent<SteamVR_Behaviour_Pose>();
    }

    // Update is called once per frame
    void Update()
    {
        if( actionButton.GetStateUp( hand ) )
        {
            // only do this if we're in select mode
            switch( currentAction )
            {
                case CurrentAction.Clone:
                    CloneCurrentCreature( true );
                    break;
                case CurrentAction.Nothing:
                    // nothing
                    break;
            }
        }
    }

    void LateUpdate()
    {
        if( selectionButton.GetStateUp( hand ) && !LaserPointerSelector.WasPressMenu() )
        {
            // respond to a potential selection
            if( FindSelectedCreature() )
            {
                // check if what we've selected is different than the previously selected creature
                if( selectedCreature != previouslySelectedCreature )
                {
                    // hide the previously selected creature's examples
                    if( previouslySelectedCreature != null ) { previouslySelectedCreature.HideExamples(); }
                    
                    previouslySelectedCreature = selectedCreature;
                }
                switch( currentSelectionResponse )
                {
                    case SelectionResponse.FollowSelected:
                            SlewToTransform slew = GetComponentInParent<SlewToTransform>();
                            slew.objectToTrack = selectedCreature.transform;
                            slew.enabled = true;
                        break;
                    case SelectionResponse.PrepareForRecording:
                        EnableSelectedCreatureAction();
                        break;
                    case SelectionResponse.Nothing:
                        break;
                }
            }
        }
    }



    public void DisablePreUIChange()
    {
        // do nothing
        currentAction = CurrentAction.Nothing;
        currentSelectionResponse = SelectionResponse.Nothing;
        // disable grip cloners
        SwitchToComponent.DisableComponent<CloneMoveInteraction>( gameObject );
        SwitchToComponent.DisableComponent<RemoteCloneMoveInteraction>( gameObject );
        // set animator mode to "do not respond to grip"
        DisableSelectedCreatureAction();
        // reset selected creature
        FindSelectedCreature();
    }

    // hacky custom UI...
    public void ProcessUIChange( SwitchToComponent.InteractionType interaction, Transform prefab )
    {
        switch( interaction )
        {
            case SwitchToComponent.InteractionType.CreatureCreate:
                // create new bird
                AnimationByRecordedExampleController newCreature = Instantiate( prefab, CalcSpawnPosition(), Quaternion.identity ).GetComponent<AnimationByRecordedExampleController>();
                newCreature.prefabThatCreatedMe = prefab;
                
                // set data sources
                newCreature.modelBaseDataSource = DefaultAnimationDataSources.theBaseDataSource;
                newCreature.modelRelativePointsDataSource = DefaultAnimationDataSources.theRelativePointsDataSources;
                
                // select new creature (forgetting currently selected one in the process)
                LaserPointerSelector.SelectNewObject( newCreature.gameObject );
                break;
            case SwitchToComponent.InteractionType.CreatureSelect:
                // this is no longer an option, now that we can select anything at any time
                break;
            case SwitchToComponent.InteractionType.CreatureClone:
                currentAction = CurrentAction.Clone;
                // also, show hint for examples of the still-selected creature
                ShowSelectedCreatureHints();
                break;
            case SwitchToComponent.InteractionType.CreatureExampleRecord:
                // turn on create new animation (in selected bird animator)
                EnableSelectedCreatureAction();
                // next time we hear a selection happened, try enabling it for recording
                currentSelectionResponse = SelectionResponse.PrepareForRecording; 
                break;
            case SwitchToComponent.InteractionType.CreatureExampleClone:
                // turn on cloning functionality
                SwitchToComponent.EnableComponent<CloneMoveInteraction>( gameObject );
                SwitchToComponent.EnableComponent<RemoteCloneMoveInteraction>( gameObject );
                break;
            case SwitchToComponent.InteractionType.CreatureExampleDelete:
                // pass -- handle this in SwitchToComponent
                break;
            case SwitchToComponent.InteractionType.CreatureConstantTimeMode:
                // switch mode for all creatures
                AnimationByRecordedExampleController.SwitchGlobalRecordingMode( AnimationByRecordedExampleController.RecordingType.ConstantTime );
                break;
            case SwitchToComponent.InteractionType.CreatureMusicMode:
                // switch mode for all creatures
                AnimationByRecordedExampleController.SwitchGlobalRecordingMode( AnimationByRecordedExampleController.RecordingType.MusicTempo );
                break;
            case SwitchToComponent.InteractionType.MoveFollowCreature:
                // next time we hear a selection happened, try to follow it
                currentSelectionResponse = SelectionResponse.FollowSelected;
                break;
            default:
                // do nothing
                Debug.Log( "I don't recognize non-creature command" );
                break;
        }
    }

    bool FindSelectedCreature()
    {
        GameObject selectedObject = LaserPointerSelector.GetSelectedObject();
        if( selectedObject == null )
        {
            selectedCreature = null;
            return false;
        }
        selectedCreature = selectedObject.GetComponent<AnimationByRecordedExampleController>();
        return selectedCreature != null;
    }

    void DisableSelectedCreatureAction()
    {
        if( FindSelectedCreature() )
        {
            selectedCreature.SetNextAction( AnimationByRecordedExampleController.AnimationAction.DoNothing, controller );
        }
    }

    void EnableSelectedCreatureAction()
    {
        if( FindSelectedCreature() )
        {
            selectedCreature.SetNextAction( AnimationByRecordedExampleController.AnimationAction.RecordAnimation, controller );
            selectedCreature.handType = hand;
        }

        // also, show hint for examples of the current creature
        ShowSelectedCreatureHints();
    }


    void CloneCurrentCreature( bool intoGroup )
    {
        if( FindSelectedCreature() )
        {
            // create new one 
            AnimationByRecordedExampleController newCreature = 
                Instantiate( selectedCreature.prefabThatCreatedMe, CalcSpawnPosition(), Quaternion.identity )
                .GetComponent<AnimationByRecordedExampleController>();
            
            // set data sources
            newCreature.modelBaseDataSource = DefaultAnimationDataSources.theBaseDataSource;
            newCreature.modelRelativePointsDataSource = DefaultAnimationDataSources.theRelativePointsDataSources;
            newCreature.SwitchRecordingMode( selectedCreature );
            newCreature.prefabThatCreatedMe = selectedCreature.prefabThatCreatedMe;
            

            if( intoGroup )
            {
                // initialize within a group
                newCreature.AddToGroup( selectedCreature );
            }
            else
            {
                // make independent. give it its own examples
                Transform _ = null;
                // clone each example and tell newCreature not to rescan provided examples yet
                foreach( AnimationExample e in selectedCreature.examples )
                {
                    newCreature.ProvideExample( e.CloneExample( newCreature, out _ ), false );
                }
                
                // and hide them for clarity
                newCreature.HideExamples();
            }

            newCreature.CloneAudioSystem( selectedCreature, intoGroup );
            newCreature.RescanMyProvidedExamples();

            // also, show hint to remind what was cloned
            ShowSelectedCreatureHints();

            // finally, copy color
            newCreature.GetComponent<AnimatedCreatureColor>().CopyColor( selectedCreature.GetComponent<AnimatedCreatureColor>() );
        }
    }

    void ShowSelectedCreatureHints()
    {
        if( selectedCreature != null )
        {
            // show hints of currently selected creature
            AnimationExample.ShowHints( selectedCreature, SwitchToComponent.hintTime );
        }
    }

    Vector3 CalcSpawnPosition()
    {
        return controller.transform.position + creationDistance * controller.transform.forward;
    }

}
