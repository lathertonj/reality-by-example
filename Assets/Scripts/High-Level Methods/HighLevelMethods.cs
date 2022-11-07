using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class HighLevelMethods : MonoBehaviour , LaserPointerSelectable
{

    private bool lookForTerrain = false;
    protected ConnectedTerrainController currentTerrain = null;

    protected abstract void StartAction();
    protected abstract void StopAction();


    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Update()
    {
        if( lookForTerrain )
        {
            ConnectedTerrainController newTerrain = TerrainUtility.FindTerrain<ConnectedTerrainController>( transform.position );
            // did we find a new terrain?
            if( newTerrain != currentTerrain )
            {
                // stop what we were doing
                if( currentTerrain != null )
                {
                    StopAction();
                }

                // remember it
                currentTerrain = newTerrain;

                // start a new thing                
                StartAction();
            }
        }
    }

    void LaserPointerSelectable.Selected()
    {
        // find a terrain under me, continuously
        lookForTerrain = true;
    }

    void LaserPointerSelectable.Unselected()
    {
        // stop action in progress
        if( currentTerrain != null ) 
        { 
            StopAction();
        }

        // stop looking for terrain
        lookForTerrain = false;
        currentTerrain = null;
    }
}
