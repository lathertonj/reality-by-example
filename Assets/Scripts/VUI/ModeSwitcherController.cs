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
                break;
            case Mode.Communication:
                me.communicationMode.SetActive( true );
                break;
        }
    }
}
