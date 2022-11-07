using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyTerrain : MonoBehaviour
{
    
    TerrainData myTerrainData;
    void Start()
    {
        myTerrainData = GetComponent<Terrain>().terrainData;
        Flatten();
    }

    public void Flatten()
    {
        TerrainUtility.FlattenTerrainData( myTerrainData );
    }

}
