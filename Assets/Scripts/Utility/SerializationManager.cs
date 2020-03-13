using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SerializationManager : MonoBehaviour
{
    public GameObject[] entitiesToSaveOnQuit;
    public GameObject[] entitiesToLoadOnStart;
    public string worldName;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine( LoadAll() );
    }

    void OnApplicationQuit()
    {
        SaveAll();
    }

    IEnumerator LoadAll()
    {
        foreach( GameObject entity in entitiesToLoadOnStart )
        {
            // each game object can have multiple components that implement this interface!
            foreach( SerializableByExample component in entity.GetComponents<SerializableByExample>() )
            {
                yield return StartCoroutine( LoadExamples( component ) );
            }
        } 
    }

    void SaveAll()
    {
        foreach( GameObject entity in entitiesToSaveOnQuit )
        {
            // each game object can have multiple components that implement this interface!
            foreach( SerializableByExample component in entity.GetComponents<SerializableByExample>() )
            {
                SaveExamples( component );
            }
        }
    }

    string GetFilepath( SerializableByExample entity )
    {
        return Application.streamingAssetsPath + "/examples/" + worldName + "_" + entity.FilenameIdentifier() + ".examples";
    }

    void SaveExamples( SerializableByExample entity )
    {
        StreamWriter writer = new StreamWriter( GetFilepath( entity ), false );
        writer.Write( entity.SerializeExamples() );
        writer.Close();
    }

    IEnumerator LoadExamples( SerializableByExample entity )
    {
        StreamReader reader = new StreamReader( GetFilepath( entity ) );
        string json = reader.ReadToEnd();
        reader.Close();
        yield return StartCoroutine( entity.LoadExamples( json ) );
    }

    public static T ConvertFromJSON<T>( string examples )
    {
        return JsonUtility.FromJson<T>( examples );
    }

    public static string ConvertToJSON<T>( T examples )
    {
        return JsonUtility.ToJson( examples );
    }
}

public interface SerializableByExample
{
    string SerializeExamples();
    IEnumerator LoadExamples( string serializedExamples );
    string FilenameIdentifier();
}
