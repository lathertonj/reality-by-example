using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class AnimationActions : MonoBehaviour
{

    public SteamVR_Input_Sources hand;
    public SteamVR_Action_Boolean actionButton;


    GripPlaceDeleteInteraction myDeleter;
    CloneMoveInteraction myCloner;

    LaserPointerColliderSelector myLaser;

    public Transform baseDataSource;
    public Transform[] relativePointsDataSources;

    private static AnimationByRecordedExampleController currentCreature;

    public enum CurrentAction{ Select, Clone, Nothing };
    public CurrentAction currentAction = CurrentAction.Nothing;

    // Start is called before the first frame update
    void Start()
    {
        myDeleter = GetComponent<GripPlaceDeleteInteraction>();
        myCloner = GetComponent<CloneMoveInteraction>();
        // hacky way to select between 2 laser pointers :(
        foreach( LaserPointerColliderSelector l in GetComponents<LaserPointerColliderSelector>() )
        {
            if( l.stopShowingOnUp )
            {
                myLaser = l;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if( actionButton.GetStateUp( hand ) )
        {
            // only do this if we're in select mode
            switch( currentAction )
            {
                case CurrentAction.Select:
                if( myLaser.IsIntersecting() )
                {
                    HideCurrentCreatureExamples();
                    DisableCurrentCreatureAction();
                    GameObject maybeCreature = myLaser.GetMostRecentIntersectedObject();
                    if( maybeCreature != null )
                    {
                        currentCreature = maybeCreature.GetComponent<AnimationByRecordedExampleController>();
                        ShowCurrentCreatureExamples();

                        // TODO: is it the right thing to switch into recording mode?
                        currentAction = CurrentAction.Nothing;
                        myLaser.HideLaser();
                        myLaser.enabled = false;
                        EnableCurrentCreatureAction();
                    }

                }
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



    public void DisablePreUIChange()
    {
        // do nothing
        currentAction = CurrentAction.Nothing;
        // disable grip cloner
        myCloner.enabled = false;
        // disable grip laser pointer selector
        if( myLaser.enabled ) { myLaser.HideLaser(); }
        myLaser.enabled = false;
        // set animator mode to "do not respond to grip"
        DisableCurrentCreatureAction();
        // TODO: disable new bird creation?
    }

    // hacky custom UI...
    public void ProcessUIChange( SwitchToComponent.InteractionType interaction, Transform prefab )
    {
        // only for our own actions, disable grip
        myDeleter.enabled = false;

        switch( interaction )
        {
            case SwitchToComponent.InteractionType.CreatureCreate:
                // hide examples
                HideCurrentCreatureExamples();
                // forget current bird and create new one
                currentCreature = Instantiate( prefab, transform.position, Quaternion.identity ).GetComponent<AnimationByRecordedExampleController>();
                currentCreature.prefabThatCreatedMe = prefab;
                // set data sources
                currentCreature.modelBaseDataSource = baseDataSource;
                currentCreature.modelRelativePointsDataSource = relativePointsDataSources;
                break;
            case SwitchToComponent.InteractionType.CreatureSelect:
                // turn on laser pointer selector
                myLaser.enabled = true;
                currentAction = CurrentAction.Select;
                break;
            case SwitchToComponent.InteractionType.CreatureClone:
                currentAction = CurrentAction.Clone;
                break;
            case SwitchToComponent.InteractionType.CreatureExampleRecord:
                // turn on create new animation (in selected bird animator)
                EnableCurrentCreatureAction();
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
                // turn on the grip delete interactor 
                myDeleter.enabled = true;
                break;
            case SwitchToComponent.InteractionType.CreatureConstantTimeMode:
                // switch mode for all creatures
                AnimationByRecordedExampleController.SwitchGlobalRecordingMode( AnimationByRecordedExampleController.RecordingType.ConstantTime );
                break;
            case SwitchToComponent.InteractionType.CreatureMusicMode:
                // switch mode for all creatures
                AnimationByRecordedExampleController.SwitchGlobalRecordingMode( AnimationByRecordedExampleController.RecordingType.MusicTempo );
                break;
            default:
                // do nothing
                Debug.Log( "I don't recognize non-creature command" );
                break;
        }
    }

    void DisableCurrentCreatureAction()
    {
        if( currentCreature != null )
        {
            currentCreature.nextAction = AnimationByRecordedExampleController.AnimationAction.DoNothing;
        }
    }

    void EnableCurrentCreatureAction()
    {
        if( currentCreature != null )
        {
            currentCreature.nextAction = AnimationByRecordedExampleController.AnimationAction.RecordAnimation;
        }
    }

    void HideCurrentCreatureExamples()
    {
        if( currentCreature != null )
        {
            currentCreature.HideExamples();
        }
    }

    void ShowCurrentCreatureExamples()
    {
        if( currentCreature != null )
        {
            currentCreature.ShowExamples();
        }
    }

    void CloneCurrentCreature( bool intoGroup )
    {
        if( currentCreature != null )
        {
            // create new one 
            AnimationByRecordedExampleController newCreature = 
                Instantiate( currentCreature.prefabThatCreatedMe, transform.position, Quaternion.identity )
                .GetComponent<AnimationByRecordedExampleController>();
            
            // set data sources
            newCreature.modelBaseDataSource = baseDataSource;
            newCreature.modelRelativePointsDataSource = relativePointsDataSources;
            newCreature.SwitchRecordingMode( currentCreature );
            newCreature.prefabThatCreatedMe = currentCreature.prefabThatCreatedMe;
            

            if( intoGroup )
            {
                // initialize within a group
                newCreature.AddToGroup( currentCreature );
            }
            else
            {
                // make independent. give it its own examples
                Transform _ = null;
                // clone each example and tell newCreature not to rescan provided examples yet
                foreach( AnimationExample e in currentCreature.examples )
                {
                    newCreature.ProvideExample( e.CloneExample( newCreature, out _ ), false );
                }
                
                // and hide them for clarity
                newCreature.HideExamples();
            }

            newCreature.CloneAudioSystem( currentCreature, intoGroup );
            newCreature.RescanMyProvidedExamples();
        }
    }

}
