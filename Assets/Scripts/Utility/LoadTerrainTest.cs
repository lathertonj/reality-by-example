using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class LoadTerrainTest : MonoBehaviour
{

    public string filename;
    public Vector2Int offsetWithinFile;
    public int filenameSize = 513;
    public float filenameMaxHeight;
    public float terrainMaxHeight;
    public bool reload;

    // Start is called before the first frame update

    void Start()
    {
        LoadIt();
    }
    void LoadIt()
    {
        TerrainData aTerrain = GetComponent<Terrain>().terrainData;
        int h = aTerrain.heightmapResolution;
        int w = aTerrain.heightmapResolution;
        float[,] fileData = new float[filenameSize,filenameSize];
        float[,] data = new float[h, w];

        // read from file
        // using( System.IO.FileStream file = System.IO.File.OpenRead( filename ) )
        // using( System.IO.BinaryReader reader = new System.IO.BinaryReader( file ) )
        
        // read from Resources?
        TextAsset textAsset = Resources.Load<TextAsset>( filename );
        using( Stream stream = new MemoryStream( textAsset.bytes ) )
        using( BinaryReader reader = new BinaryReader( stream ) )
        {
            for( int y = 0; y < filenameSize; y++ )
            {
                for( int x = 0; x < filenameSize; x++ )
                {
                    float v = (float)reader.ReadUInt16() / 0xFFFF;
                    fileData[y, x] = v;
                }
            }
        }

        // crop and re-height
        for( int y = 0; y < h; y++ )
        {
            for( int x = 0; x < w; x++ )
            {
                data[y, x] = fileData[y + offsetWithinFile.y, x + offsetWithinFile.x]
                    * filenameMaxHeight / terrainMaxHeight;
            }
        }
        aTerrain.SetHeights( 0, 0, data );
    }

    // Update is called once per frame
    void Update()
    {
        if( reload )
        {
            LoadIt();
            reload = false;
        }
    }
}
