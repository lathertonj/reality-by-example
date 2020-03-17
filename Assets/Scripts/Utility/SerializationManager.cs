using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

public class SerializationManager : MonoBehaviour
{
    public GameObject[] entitiesToSaveOnQuit;
    public GameObject[] entitiesToLoadOnStart;
    public string worldName;

    public bool saveDynamicEntities = false;
    public bool loadDynamicEntities = false;

    // Start is called before the first frame update
    void Start()
    {
        // ensure dirs exist
        string folder = DynamicExamplesLocation();
        if( !Directory.Exists( folder ) ) { Directory.CreateDirectory( folder ); }

        // load all
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

        if( loadDynamicEntities )
        {
            // read all filenames
            foreach( string subdirectory in Directory.GetDirectories( DynamicExamplesLocation() ) )
            {
                // name of prefab is name of directory
                DirectoryInfo info = new DirectoryInfo( subdirectory );
                string prefabName = info.Name;
                GameObject prefab = (GameObject) Resources.Load( "Prefabs/" + prefabName );

                foreach( FileInfo fileInfo in info.GetFiles( "*" + FileExtension() ) )
                {
                    yield return StartCoroutine( DynamicLoadExamples( fileInfo, prefab ) );
                }
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

        if( saveDynamicEntities )
        {
            // first, delete previous dynamic entities
            DirectoryInfo dynamicDir = new DirectoryInfo( DynamicExamplesLocation() );
            foreach( DirectoryInfo dir in dynamicDir.EnumerateDirectories() )
            {
                dir.Delete(true); 
            }

            // now, save current dynamic entities
            foreach( GameObject o in SceneManager.GetActiveScene().GetRootGameObjects() )
            {
                DynamicSerializableByExample entity = o.GetComponent<DynamicSerializableByExample>();
                
                if( entity != null && entity.ShouldSerialize() )
                {
                    DynamicSaveExamples( entity );
                }
            }
        }
    }

    string GetFilepath( SerializableByExample entity )
    {
        return Application.streamingAssetsPath + "/examples/" + worldName + "/" + entity.FilenameIdentifier() + FileExtension();
    }

    string GetFilepath( DynamicSerializableByExample entity )
    {
        string folder = DynamicExamplesLocation() + entity.PrefabName() + "/";
        if( !Directory.Exists( folder ) ) { Directory.CreateDirectory( folder ); }
        return folder + entity.FilenameIdentifier() + FileExtension();
    }

    string DynamicExamplesLocation()
    {
        return Application.streamingAssetsPath + "/examples/" + worldName + "/dynamic/";
    }

    string FileExtension()
    {
        return ".examples";
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

    void DynamicSaveExamples( DynamicSerializableByExample entity )
    {
        StreamWriter writer = new StreamWriter( GetFilepath( entity ), false );
        writer.Write( entity.SerializeExamples() );
        writer.Close();
    }

    IEnumerator DynamicLoadExamples( FileInfo file, GameObject prefab )
    {
        // read json
        StreamReader reader = file.OpenText();
        string json = reader.ReadToEnd();
        reader.Close();

        // create and initialize
        GameObject newObject = Instantiate( prefab );
        DynamicSerializableByExample entity = newObject.GetComponent<DynamicSerializableByExample>();
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


public interface DynamicSerializableByExample : SerializableByExample
{
    string PrefabName();
    bool ShouldSerialize();
}
