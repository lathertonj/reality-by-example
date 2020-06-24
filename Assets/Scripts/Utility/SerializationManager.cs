using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class SerializationManager : MonoBehaviour
{
    public GameObject[] entitiesToSaveOnQuit;
    public GameObject[] entitiesToLoadOnStart;
    public string worldName;
    public bool findWorldNameInFile = false;
    public string fileWithWorldName;
    private string saveWorldName;
    private string loadWorldName;

    public bool saveDynamicEntities = false;
    public bool loadDynamicEntities = false;
    public bool loadLatestVersionOfWorld = false;
    public bool loadRandomIfNoneFound = true;
    public bool saveToUniqueNumber = false;

    public string[] manualLoadDynamicEntities;

    public float saveEverySeconds = 0;

    // Start is called before the first frame update
    void Start()
    {
        // what is world name?
        if( findWorldNameInFile )
        {
            FindWorldNameInFile();
        }
        
        // where to load from
        if( loadLatestVersionOfWorld )
        {
            // find first number not in use
            loadWorldName = FindLatestWorldName();
        }
        else
        {
            loadWorldName = worldName;
        }

        // where to save to
        if( saveToUniqueNumber )
        {
            // ensure worldName is not in use
            saveWorldName = FindUniqueWorldName();
        }
        else
        {
            saveWorldName = worldName;
        }

        #if UNITY_WEBGL
            // don't try to create folders on web
        #else
            // ensure load dir exists
            if( loadWorldName != "" )
            {
                string loadFolder = DynamicExamplesLocation( true );
                if( !Directory.Exists( loadFolder ) ) { Directory.CreateDirectory( loadFolder ); }
            }
            // ensure save dir exists
            string saveFolder = DynamicExamplesLocation( false );
            if( !Directory.Exists( saveFolder ) ) { Directory.CreateDirectory( saveFolder ); }
        #endif

        

        // load all
        if( loadWorldName != "" )
        {
            StartCoroutine( LoadAll() );
        }
        else if( loadRandomIfNoneFound )
        {
            // we thought we'd be able to load something, but failed
            StartCoroutine( RandomizeTerrain.RandomizeWorld() );
        }

        if( saveEverySeconds != 0 )
        {
            InvokeRepeating( "SaveAll", saveEverySeconds, saveEverySeconds );
        }
    }

    void OnApplicationQuit()
    {
        SaveAll();
    }

    void FindWorldNameInFile()
    {
        StreamReader reader = new StreamReader( Application.streamingAssetsPath + "/" + fileWithWorldName );
        worldName = reader.ReadLine().Split( null )[0];
    }

    string FindUniqueWorldName()
    {
        int worldNum = 0;
        string newWorldName;
        do
        {
            newWorldName = worldName + worldNum.ToString();
            worldNum++;

        } while( Directory.Exists( Application.streamingAssetsPath + "/examples/" + newWorldName ) );
        return newWorldName;
    }


    string FindLatestWorldName()
    {
        int worldNum = 0;
        string newWorldName = "";
        string prevWorldName;
        do
        {
            prevWorldName = newWorldName;
            newWorldName = worldName + worldNum.ToString();
            worldNum++;

        } while( Directory.Exists( Application.streamingAssetsPath + "/examples/" + newWorldName ) );
        return prevWorldName;
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
            #if UNITY_WEBGL
            // use the manually provided names
            foreach( string dynamicEntity in manualLoadDynamicEntities )
            {
                yield return StartCoroutine( DynamicLoadExamples( dynamicEntity ) );
            }

            #else
            // read all filenames
            foreach( string subdirectory in Directory.GetDirectories( DynamicExamplesLocation( true ) ) )
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
            #endif
        }
    }

    void SaveAll()
    {
        #if UNITY_WEBGL
        Debug.LogError( "can't save things on the web..." );
        #else
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
            DirectoryInfo dynamicDir = new DirectoryInfo( DynamicExamplesLocation( false ) );
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
        #endif
    }

    string WorldName( bool isLoad )
    {
        return isLoad ? loadWorldName : saveWorldName;
    }

    string GetFilepath( SerializableByExample entity, bool isLoad )
    {
        return Application.streamingAssetsPath + "/examples/" + WorldName( isLoad ) + "/" + entity.FilenameIdentifier() + FileExtension();
    }

    string GetFilepath( DynamicSerializableByExample entity, bool isLoad )
    {
        string folder = DynamicExamplesLocation( isLoad ) + entity.PrefabName() + "/";
        if( !Directory.Exists( folder ) ) { Directory.CreateDirectory( folder ); }
        return folder + entity.FilenameIdentifier() + FileExtension();
    }

    string DynamicExamplesLocation( bool isLoad )
    {
        return Application.streamingAssetsPath + "/examples/" + WorldName( isLoad ) + "/dynamic/";
    }

    string FileExtension()
    {
        return ".examples";
    }
    

    void SaveExamples( SerializableByExample entity )
    {
        StreamWriter writer = new StreamWriter( GetFilepath( entity, false ), false );
        writer.Write( entity.SerializeExamples() );
        writer.Close();
    }

    IEnumerator LoadExamples( SerializableByExample entity )
    {
        #if UNITY_WEBGL
        UnityWebRequest www = UnityWebRequest.Get( GetFilepath( entity, true ) );
        yield return www.SendWebRequest();
        if( !www.isNetworkError && !www.isHttpError )
        {
            yield return StartCoroutine( entity.LoadExamples( www.downloadHandler.text ) );
        }
        #else
        StreamReader reader = new StreamReader( GetFilepath( entity, true ) );
        string json = reader.ReadToEnd();
        reader.Close();
        yield return StartCoroutine( entity.LoadExamples( json ) );
        #endif
    }

    void DynamicSaveExamples( DynamicSerializableByExample entity )
    {
        StreamWriter writer = new StreamWriter( GetFilepath( entity, false ), false );
        writer.Write( entity.SerializeExamples() );
        writer.Close();
    }

    #if UNITY_WEBGL
    IEnumerator DynamicLoadExamples( string fileName )
    {
        string filePath = DynamicExamplesLocation( true ) + fileName + FileExtension();
        UnityWebRequest www = UnityWebRequest.Get( filePath );
        yield return www.SendWebRequest();
        if( !www.isNetworkError && !www.isHttpError )
        {
            // create
            string prefabName = fileName.Split('/')[0];
            GameObject prefab = (GameObject) Resources.Load( "Prefabs/" + prefabName );
            GameObject newObject = Instantiate( prefab );
            
            // initialize
            DynamicSerializableByExample entity = newObject.GetComponent<DynamicSerializableByExample>();
            yield return StartCoroutine( entity.LoadExamples( www.downloadHandler.text ) );
        }
    }

    #else

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
    #endif

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
