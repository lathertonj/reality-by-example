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

    private bool shouldSelect = false;

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
            if( shouldSelect )
            {
                HideCurrentCreatureExamples();
                DisableCurrentCreatureAction();
                GameObject maybeCreature = myLaser.GetMostRecentIntersectedObject();
                if( maybeCreature != null )
                {
                    currentCreature = maybeCreature.GetComponent<AnimationByRecordedExampleController>();
                    ShowCurrentCreatureExamples();

                    // TODO: is it the right thing to switch into recording mode?
                    shouldSelect = false;
                    myLaser.enabled = false;
                    EnableCurrentCreatureAction();
                }
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
        myLaser.enabled = false;
        shouldSelect = false;
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
                shouldSelect = true;
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
}
