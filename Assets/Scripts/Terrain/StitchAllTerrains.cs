using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Stitchscape;

public class StitchAllTerrains : MonoBehaviour
{
    private static StitchAllTerrains theStitcher;

    public TerrainRow[] rows;
    private float stitchWidth = 0.1f;
    private float stitchStrength = 0f; 

    [System.Serializable]
    public class TerrainRow
    {
        public Terrain[] rowLeftToRight;
    }
    // Start is called before the first frame update
    void Start()
    {
        theStitcher = this;
    }

    private void StitchAll()
    {
        // first, flatten
        for( int i = 0; i < rows.Length; i++ )
        {
            for( int j = 0; j < rows[i].rowLeftToRight.Length; j++ )
            {
                DummyTerrain maybeDummy = rows[i].rowLeftToRight[j].GetComponent<DummyTerrain>();
                if( maybeDummy ) { maybeDummy.Flatten(); }
            }
        }

        // now stitch
        for( int i = 0; i < rows.Length; i++ )
        {
            // left to right
            for( int j = 0; j < rows[i].rowLeftToRight.Length - 1; j++ )
            {
                Stitch.TerrainStitch(
                    rows[i].rowLeftToRight[j].terrainData,
                    rows[i].rowLeftToRight[j+1].terrainData,
                    StitchDirection.Across,
                    stitchWidth,
                    stitchStrength,
                    false
                );
            }

            // top to bottom
            if( i > 0 )
            {
                // left to right
                for( int j = 0; j < rows[i].rowLeftToRight.Length; j++ )
                {
                    Stitch.TerrainStitch(
                        rows[i-1].rowLeftToRight[j].terrainData,
                        rows[i].rowLeftToRight[j].terrainData,
                        StitchDirection.Down,
                        stitchWidth,
                        stitchStrength,
                        false
                    );
                }
            }
        }
    }


    public static void Restitch()
    {
        theStitcher.StitchAll();
    }
}
