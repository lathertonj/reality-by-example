using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class AnimationModeSwitcher : MonoBehaviour
{

    public SteamVR_Input_Sources hand;
    public SteamVR_Action_Boolean showHideMenu;
    public SteamVR_Action_Boolean actionButton;

    public Transform menuPrefab;
    private Transform myMenu;
    private Transform head;
    private RandomizeTerrain randomizer;

    private bool menuVisible = false;
    GripPlaceDeleteInteraction myDeleter;
    CloneMoveInteraction myCloner;

    LaserPointerColliderSelector myLaser;

    public Transform baseDataSource;
    public Transform[] relativePointsDataSources;

    public AnimationByRecordedExampleController currentCreature;

    public Transform creaturePrefab;


    private enum CurrentAction{ Select, Clone, Nothing };
    private CurrentAction currentAction = CurrentAction.Nothing;

    // Start is called before the first frame update
    void Start()
    {
        head = transform.parent.GetComponentInChildren<Camera>().transform;
        randomizer = transform.parent.GetComponentInChildren<RandomizeTerrain>();
        myDeleter = GetComponent<GripPlaceDeleteInteraction>();
        myCloner = GetComponent<CloneMoveInteraction>();
        myLaser = GetComponent<LaserPointerColliderSelector>();
        myMenu = Instantiate( menuPrefab );
        HideMenu();
    }

    // Update is called once per frame
    void Update()
    {
        if( showHideMenu.GetStateDown( hand ) )
        {
            if( menuVisible )
            {
                // hide menu
                HideMenu();
            }
            else
            {
                // show menu
                ShowMenu();
            }
        }

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
                    CloneCurrentCreature();
                    break;
                case CurrentAction.Nothing:
                    // nothing
                    break;
            }
        }
    }

    void ShowMenu()
    {
        // place at my transform position, plus a little away from my hand
        Vector3 away = transform.position - head.position;
        myMenu.position = transform.position + 0.25f * away.normalized;

        // face menu toward head
        myMenu.LookAt( head );

        // reenable
        myMenu.gameObject.SetActive( true );

        menuVisible = true;
    }

    void HideMenu()
    {
        // disable
        myMenu.gameObject.SetActive( false );

        menuVisible = false;
    }

    // hacky custom UI...
    void OnTriggerEnter( Collider other )
    {
        // only respond to menu items
        if( !other.gameObject.CompareTag( "MenuItem" ) )
        {
            return;
        }


        // disable grip delete interactor
        myDeleter.enabled = false;
        // disable grip cloner
        myCloner.enabled = false;
        // disable grip laser pointer selector
        if( myLaser.enabled ) { myLaser.HideLaser(); }
        myLaser.enabled = false;
        currentAction = CurrentAction.Nothing;
        // set animator mode to "do not respond to grip"
        DisableCurrentCreatureAction();
        // disable new bird creation
        switch( other.gameObject.name )
        {
            case "DeleteExample":
                // turn on the grip delete interactor 
                myDeleter.enabled = true;
                break;
            case "CloneExample":
                // turn on cloning functionality
                myCloner.enabled = true;
                break;
            case "SelectBird":
                // turn on laser pointer selector
                myLaser.enabled = true;
                currentAction = CurrentAction.Select;
                break;
            case "CreateNewBird":
                // hide examples
                HideCurrentCreatureExamples();
                // forget current bird and create new one
                currentCreature = Instantiate( creaturePrefab, transform.position, Quaternion.identity ).GetComponent<AnimationByRecordedExampleController>();
                // set data sources
                currentCreature.modelBaseDataSource = baseDataSource;
                currentCreature.modelRelativePointsDataSource = relativePointsDataSources;
                break;
            case "RecordBirdAnimation":
                // turn on create new animation (in selected bird animator)
                EnableCurrentCreatureAction();
                // else
                // {
                //     // TODO: inform debug somehow that there is no creature selected
                // }
                break;
            case "CloneBird":
                currentAction = CurrentAction.Clone;
                break;
            default:
                // do nothing
                Debug.Log( "I don't recognize command: " + other.gameObject.name );
                break;
        }

        // also hide the UI when we're done with it
        HideMenu();
    }

    void DisableCurrentCreatureAction()
    {
        if( currentCreature != null )
        {
            // this will likely fail but also this script is not used in this branch
            currentCreature.SetNextAction( AnimationByRecordedExampleController.AnimationAction.DoNothing, GetComponent<SteamVR_Behaviour_Pose>() );
        }
    }

    void EnableCurrentCreatureAction()
    {
        if( currentCreature != null )
        {
            // this will likely fail but also this script is not used in this branch
            currentCreature.SetNextAction( AnimationByRecordedExampleController.AnimationAction.RecordAnimation, GetComponent<SteamVR_Behaviour_Pose>() );
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

    void CloneCurrentCreature()
    {
        if( currentCreature != null )
        {
            // create new one 
            AnimationByRecordedExampleController newCreature = 
                Instantiate( creaturePrefab, transform.position, Quaternion.identity )
                .GetComponent<AnimationByRecordedExampleController>();
            
            // set data sources
            newCreature.modelBaseDataSource = baseDataSource;
            newCreature.modelRelativePointsDataSource = relativePointsDataSources;
            
            Transform _ = null;
            // clone each example and tell newCreature not to rescan provided examples yet
            foreach( AnimationExample e in currentCreature.examples )
            {
                newCreature.ProvideExample( e.CloneExample( newCreature, out _ ), false );
            }

            newCreature.CloneAudioSystem( currentCreature, false );
            newCreature.RescanProvidedExamples();
            newCreature.HideExamples();
        }
    }
}
