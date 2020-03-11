using UnityEngine;

public class ModeSwitcherController : MonoBehaviour
{
    static private ModeSwitcherController me;

    private bool amEnabled = false;
    public Transform objectToSetPositionFrom;
    public GameObject theModeSwitcher;

    void Start()
    {
        me = this;
        theModeSwitcher.SetActive( false );
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
}
