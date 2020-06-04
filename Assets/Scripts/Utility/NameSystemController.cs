using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NameSystemController : MonoBehaviour
{
    private static bool inNamingMode = false;
    private static Nameable objectToName;

    private static Canvas myCanvas;
    public InputField nameInput;
    private static InputField myInput;

    public static void SetObjectToName( Nameable n )
    {
        objectToName = n;
        if( inNamingMode && n != null )
        {
            // update the UI
            myInput.text = n.GetDisplayName();
        }
    }
    
    void Start()
    {
        myCanvas = GetComponentInChildren<Canvas>();
        myCanvas.gameObject.SetActive( false );
        myInput = nameInput;
    }

    
    void Update()
    {
        if( Input.GetKeyDown( "enter" ) || Input.GetKeyDown( "return" ) )
        {
            inNamingMode = !inNamingMode;

            if( inNamingMode )
            {
                // turn on naming UI
                myCanvas.gameObject.SetActive( true );

                // add event listener to change
                myInput.onValueChanged.AddListener( UpdateName );

                // and put cursor into it
                myInput.Select();
                myInput.ActivateInputField();

                // set the text if we already have selected a nameable
                if( objectToName != null )
                {
                    myInput.text = objectToName.GetDisplayName();
                }                
            }
            else
            {
                // turn off naming UI
                myCanvas.gameObject.SetActive( false );
                
                // remove event listener
                myInput.onValueChanged.RemoveAllListeners();
            }
        }    
    }

    static void UpdateName( string newName )
    {
        if( objectToName != null )
        {
            objectToName.SetDisplayName( newName );
        }
    }
}

public interface Nameable
{
    void SetDisplayName( string newName );
    string GetDisplayName();
}
