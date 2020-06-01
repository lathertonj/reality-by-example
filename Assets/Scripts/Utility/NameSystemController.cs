using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NameSystemController : MonoBehaviour
{
    private static bool inNamingMode = false;
    private static Nameable objectToName;

    public static void SetObjectToName( Nameable n )
    {
        objectToName = n;
        if( inNamingMode )
        {
            // update the UI
            // UI.text = n.GetDisplayName();
        }
    }
    
    void Start()
    {
        
    }

    
    void Update()
    {
        if( Input.GetKeyDown( "enter" ) || Input.GetKeyDown( "return" ) )
        {
            inNamingMode = !inNamingMode;

            if( inNamingMode )
            {
                // turn on naming UI, add event listener to change
                // and put cursor into it

                // set the text if we already have selected a nameable
                if( objectToName != null )
                {
                    // UI.text = n.GetDisplayName();
                }                
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
