using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyTerrain : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        TerrainData myTerrainData = GetComponent<Terrain>().terrainData;
        int verticesPerSide = myTerrainData.heightmapWidth;
        float [,] heights = new float[verticesPerSide, verticesPerSide];
        myTerrainData.SetHeightsDelayLOD( 0, 0, heights );
        myTerrainData.SyncHeightmap();
    }

}
