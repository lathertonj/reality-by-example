using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnedObject : MonoBehaviour , GripPlaceDeleteInteractable
{

    [HideInInspector] public LayerMask mask;
    private static List<SpawnedObject> allSpawnedObjects = new List<SpawnedObject>();

    // Start is called before the first frame update
    void Start()
    {
        allSpawnedObjects.Add( this );
    }

    public static void ResetSpawnedObjectHeights()
    {
        foreach( SpawnedObject o in allSpawnedObjects )
        {
            o.UpdateHeight();
        }
    }

    void UpdateHeight()
    {
        // find a terrain beneath me
        RaycastHit hit;
        if( Physics.Raycast( transform.position + 600 * Vector3.up, Vector3.down, out hit, 2000, mask ) )
        {
            // set my position
            transform.position = hit.point;
        }
    }

    void GripPlaceDeleteInteractable.JustPlaced()
    {
        // this will likely not be placed with the Grip interaction, but in case it is
        UpdateHeight();
        Debug.LogError( "Spawned object placed with grip will not have the right Mask assigned!" );
    }

    void GripPlaceDeleteInteractable.AboutToBeDeleted()
    {
        allSpawnedObjects.Remove( this );
    }
}
