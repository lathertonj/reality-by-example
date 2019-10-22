using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableModeSwitcher : MonoBehaviour
{
    static private DisableModeSwitcher me;

    // Start is called before the first frame update
    void Start()
    {
        me = this;
    }

    public static void SetEnabled( bool e )
    {
        me.gameObject.SetActive( e );
    }
}
