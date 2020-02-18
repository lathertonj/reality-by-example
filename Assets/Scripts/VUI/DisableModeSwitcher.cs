using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class DisableModeSwitcher : MonoBehaviour
{
    static private DisableModeSwitcher me;

    public SteamVR_Action_Boolean toggleMenu;
    public SteamVR_Input_Sources handType;
    private bool amEnabled = false;
    public Transform objectToSetPositionFrom;
    public GameObject theModeSwitcher;

    void Start()
    {
        me = this;
        theModeSwitcher.SetActive( false );
    }

    void Update()
    {
        if( toggleMenu.GetStateDown( handType ) )
        {
            amEnabled = !amEnabled;
            theModeSwitcher.SetActive( amEnabled );
            if( amEnabled )
            {
                SetPosition();
            }
        }
    }

    public static void SetEnabled( bool e )
    {
        me.theModeSwitcher.SetActive( e );
        if( !me.amEnabled && e )
        {
            me.SetPosition();
        }
        me.amEnabled = e;
    }

    private void SetPosition()
    {
        theModeSwitcher.transform.position = objectToSetPositionFrom.position;
        theModeSwitcher.transform.rotation = Quaternion.AngleAxis( objectToSetPositionFrom.eulerAngles.y, Vector3.up );
    }
}
