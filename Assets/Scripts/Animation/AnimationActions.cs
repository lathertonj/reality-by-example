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


    CloneMoveInteraction myCloner;


    public Transform baseDataSource;
    public Transform[] relativePointsDataSources;

    private AnimationByRecordedExampleController selectedCreature = null;

    public enum CurrentAction{ FollowSelected, Clone, Nothing };
    public CurrentAction currentAction = CurrentAction.Nothing;

    public float creationDistance = 1.5f;

    // Start is called before the first frame update
    void Start()
    {
        myCloner = GetComponent<CloneMoveInteraction>();
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
                case CurrentAction.FollowSelected:
                    // do nothing in response to action button
                    break;
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
        if( selectionButton.GetStateUp( hand ) )
        {
            // respond to a potential selection
            switch( currentAction )
            {
                case CurrentAction.FollowSelected:
                    if( FindSelectedCreature() )
                    {
                        SlewToTransform slew = GetComponentInParent<SlewToTransform>();
                        slew.objectToTrack = selectedCreature.transform;
                        slew.enabled = true;
                    }
                    break;
                // TODO: things for recording new examples?
                default:
                    break;
            }
        }
    }



    public void DisablePreUIChange()
    {
        // do nothing
        currentAction = CurrentAction.Nothing;
        // disable grip cloner
        myCloner.enabled = false;
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
                newCreature.modelBaseDataSource = baseDataSource;
                newCreature.modelRelativePointsDataSource = relativePointsDataSources;
                
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
                // else
                // {
                //     // TODO: inform debug somehow that there is no creature selected
                // }

                
                break;
            case SwitchToComponent.InteractionType.CreatureExampleClone:
                // turn on cloning functionality
                myCloner.enabled = true;
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
                // turn on laser pointer selector
                currentAction = CurrentAction.FollowSelected;
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
            selectedCreature.nextAction = AnimationByRecordedExampleController.AnimationAction.DoNothing;
        }
    }

    void EnableSelectedCreatureAction()
    {
        if( FindSelectedCreature() )
        {
            selectedCreature.nextAction = AnimationByRecordedExampleController.AnimationAction.RecordAnimation;
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
            newCreature.modelBaseDataSource = baseDataSource;
            newCreature.modelRelativePointsDataSource = relativePointsDataSources;
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
