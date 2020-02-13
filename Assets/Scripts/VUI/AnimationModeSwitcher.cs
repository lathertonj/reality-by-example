using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class AnimationModeSwitcher : MonoBehaviour
{

    public SteamVR_Input_Sources hand;
    public SteamVR_Action_Boolean showHideMenu;

    public Transform menuPrefab;
    private Transform myMenu;
    private Transform head;
    private RandomizeTerrain randomizer;

    private bool menuVisible = false;
    GripPlaceDeleteInteraction myDeleter;
    CloneMoveInteraction myCloner;
    
    public AnimationByRecordedExampleController currentCreature;

    // Start is called before the first frame update
    void Start()
    {
        head = transform.parent.GetComponentInChildren<Camera>().transform;
        randomizer = transform.parent.GetComponentInChildren<RandomizeTerrain>();
        myDeleter = GetComponent<GripPlaceDeleteInteraction>();
        myCloner = GetComponent<CloneMoveInteraction>();
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
        // TODO
        // set animator mode to "do not respond to grip"
        if( currentCreature != null )
        {
            currentCreature.nextAction = AnimationByRecordedExampleController.AnimationAction.DoNothing;
        }
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
                // TODO
                break;
            case "CreateNewBird":
                // turn on create new bird
                // (when new bird is created, select it)
                // TODO: coopt grip place delete interaction?
                break;
            case "RecordBirdAnimation":
                // turn on create new animation (in selected bird animator)
                if( currentCreature != null )
                {
                    currentCreature.nextAction = AnimationByRecordedExampleController.AnimationAction.RecordAnimation;
                }
                break;
            default:
                // do nothing
                Debug.Log( "I don't recognize command: " + other.gameObject.name );
                break;
        }

        // also hide the UI when we're done with it
        HideMenu();
    }
}
