using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class LoadSavedWorldDemo : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void initializeFileInput( string id );

    TerrainTextureController me;

    void Start()
    {
        #if UNITY_WEBGL
        // the file uploader has ID "uploadWorld" in the index.html host
        initializeFileInput( "uploadWorld" );
        #endif
        me = GetComponent<TerrainTextureController>();
    }

    void FileSelected( string url )
    {
        StartCoroutine( LoadFile( url ) );
    }

    IEnumerator LoadFile( string url )
    {
        // TODO: consider migrating to UnityWebRequest; WWW is deprecated
        WWW file = new WWW( url );
        yield return file;

        // get text
        string fileContents = file.text;
        
        // replace contents
        me.ReplaceTrainingExamplesWithSerial( fileContents );
    }
}
