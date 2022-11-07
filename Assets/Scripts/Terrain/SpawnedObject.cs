using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnedObject : MonoBehaviour , GripPlaceDeleteInteractable , DynamicSerializableByExample
{

    public LayerMask terrainsToPlaceOn;
    private static List<SpawnedObject> allSpawnedObjects = new List<SpawnedObject>();
    private static int nextID = 0;
    private int myID;

    public string prefabName;

    // Start is called before the first frame update
    void Start()
    {
        allSpawnedObjects.Add( this );
        myID = nextID;
        nextID++;
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
        if( Physics.Raycast( transform.position + 600 * Vector3.up, Vector3.down, out hit, 2000, terrainsToPlaceOn ) )
        {
            // set my position
            transform.position = hit.point;
        }
    }

    void GripPlaceDeleteInteractable.JustPlaced()
    {
        // this will likely not be placed with the Grip interaction, but in case it is
        UpdateHeight();
    }

    void GripPlaceDeleteInteractable.AboutToBeDeleted()
    {
        allSpawnedObjects.Remove( this );
    }


    bool DynamicSerializableByExample.ShouldSerialize()
    {
        return true;
    }

    string SerializableByExample.SerializeExamples()
    {
        SerializedSpawnedObject serial = new SerializedSpawnedObject();
        serial.position = transform.position;
        serial.rotation = transform.rotation;
        return SerializationManager.ConvertToJSON<SerializedSpawnedObject>( serial );
    }

    IEnumerator SerializableByExample.LoadExamples( string serializedExamples )
    {
        SerializedSpawnedObject serial = SerializationManager.ConvertFromJSON<SerializedSpawnedObject>( serializedExamples );
        transform.position = serial.position;
        transform.rotation = serial.rotation;
        
        // reset height just in case
        UpdateHeight();

        yield break;
    }

    string SerializableByExample.FilenameIdentifier()
    {
        return "spawned_object_" + myID.ToString();
    }
    
    string DynamicSerializableByExample.PrefabName()
    {
        return prefabName;
    }
}


[System.Serializable]
class SerializedSpawnedObject
{
    public Vector3 position;
    public Quaternion rotation;
}