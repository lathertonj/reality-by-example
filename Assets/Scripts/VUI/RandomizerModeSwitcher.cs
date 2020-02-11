using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class RandomizerModeSwitcher : MonoBehaviour
{

    public SteamVR_Input_Sources hand;
    public SteamVR_Action_Boolean showHideMenu;

    public Transform menuPrefab;
    private Transform myMenu;
    private Transform head;
    private RandomizeTerrain randomizer;
    private LaserPointerDragAndDrop dragAndDrop;

    private bool menuVisible = false;

    // Start is called before the first frame update
    void Start()
    {
        head = transform.parent.GetComponentInChildren<Camera>().transform;
        randomizer = transform.parent.GetComponentInChildren<RandomizeTerrain>();
        dragAndDrop = GetComponent<LaserPointerDragAndDrop>();
        dragAndDrop.enabled = false;
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

        // disable for most
        dragAndDrop.enabled = false;
        switch( other.gameObject.name )
        {
            case "PerturbSmall":
                randomizer.currentAction = RandomizeTerrain.ActionType.PerturbSmall;
                break;
            case "PerturbBig":
            // TODO: why doesn't this do anything?
                randomizer.currentAction = RandomizeTerrain.ActionType.PerturbBig;
                break;
            case "Copy":
                randomizer.currentAction = RandomizeTerrain.ActionType.Copy;
                // we need the drag and drop for this one only
                dragAndDrop.enabled = true;
                break;
            case "RandomizeCurrent":
                randomizer.currentAction = RandomizeTerrain.ActionType.RandomizeCurrent;
                break;
            case "RandomizeAll":
            // TODO: why doesn't this do anything?
                randomizer.currentAction = RandomizeTerrain.ActionType.RandomizeAll;
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
