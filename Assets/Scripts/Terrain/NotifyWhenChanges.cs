using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class NotifyWhenChanges : MonoBehaviour
{

    public static void Terrain()
    {
        aTerrainChanged.Invoke();
    }

    public static void NotifyIfTerrainChanges( UnityAction atThisAddress )
    {
        aTerrainChanged.AddListener( atThisAddress );
    }

    private static UnityEvent aTerrainChanged = null;

    // Start is called before the first frame update
    void Awake()
    {
        if( aTerrainChanged == null )
        {
            aTerrainChanged = new UnityEvent();
        }
    }
}
