using UnityEngine;

public class ModeSwitcherController : MonoBehaviour
{

    public enum Mode { Main, Animation, Communication };
    
    static private ModeSwitcherController me;

    private bool amEnabled = false;
    public Transform objectToSetPositionFrom;
    public GameObject theModeSwitcher;
    public GameObject mainMode;
    public GameObject animationMode;
    public GameObject communicationMode;

    private Mode myMode;
    private Mode prevMode;

    void Start()
    {
        me = this;

        // default to invisible
        theModeSwitcher.SetActive( false );
        
        // default to main mode
        SetMode( Mode.Main );
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

    public static void ToggleEnabled()
    {
        SetEnabled( !me.amEnabled );
    }

    public static void CycleToNextMode()
    {
        // special case for animation
        if( me.myMode == Mode.Animation )
        {
            ToggleEnabled();
            return;
        }

        // turn on
        if( !me.amEnabled )
        {
            // turn on to first menu (main)
            SetMode( Mode.Main );
            SetEnabled( true );
        }
        // switch to next menu
        else
        {
            switch( me.myMode )
            {
                // main leads to communication
                case Mode.Main:
                    SetMode( Mode.Communication );
                    break;
                // communication leads to off
                case Mode.Communication:
                // animation currently only used when you select an animated object
                case Mode.Animation:
                default:
                    SetEnabled( false );
                    break;
            }
        }
    }

    private void SetPosition()
    {
        theModeSwitcher.transform.position = objectToSetPositionFrom.position;
        theModeSwitcher.transform.rotation = Quaternion.AngleAxis( objectToSetPositionFrom.eulerAngles.y, Vector3.up );
    }

    public static void SetMode( Mode newMode )
    {
        // first, set all modes to be invisible
        me.mainMode.SetActive( false );
        me.animationMode.SetActive( false );
        me.communicationMode.SetActive( false );

        // then, enable the right one
        switch( newMode )
        {
            case Mode.Main:
                me.mainMode.SetActive( true );
                break;
            case Mode.Animation:
                me.animationMode.SetActive( true );
                // animation is special case that can temporarily override something
                me.prevMode = me.myMode;
                break;
            case Mode.Communication:
                me.communicationMode.SetActive( true );
                break;
        }

        me.myMode = newMode;
    }

    public static void ResetMode()
    {
        SetMode( me.prevMode );
    }
}
