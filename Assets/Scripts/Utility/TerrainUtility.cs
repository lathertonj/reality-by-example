using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainUtility
{
    public static T FindTerrain<T>( Vector3 nearLocation )
    {
        Vector3 _;
        return FindTerrain<T>( nearLocation, out _ );
    }

    public static T FindTerrain<T>( Vector3 nearLocation, out Vector3 hitPoint )
    {
        // Bit shift the index of the layer (8: Connected terrains) to get a bit mask
        int layerMask = 1 << 8;

        RaycastHit hit;
        // Check from a point really high above us, in the downward direction (in case we are below terrain)
        if( Physics.Raycast( nearLocation + 400 * Vector3.up, Vector3.down, out hit, Mathf.Infinity, layerMask ) )
        {
            T foundTerrain = hit.transform.GetComponentInParent<T>();
            hitPoint = hit.point;
            if( foundTerrain != null )
            {
                return foundTerrain;
            }
        }

        // ~ null
        hitPoint = default(Vector3);
        return default(T);
    }

    public static void FlattenTerrainData( TerrainData td )
    {
        float[,] newHeights = new float[td.heightmapWidth, td.heightmapHeight];
        td.SetHeights( 0, 0, newHeights );
    }

    
}
