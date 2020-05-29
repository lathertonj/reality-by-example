using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainUtility
{
    public static T FindTerrain<T>( Vector3 nearLocation )
    {
        Vector3 _, __;
        return FindTerrain<T>( nearLocation, out _, out __ );
    }

    public static T FindTerrain<T>( Vector3 nearLocation, out Vector3 hitPoint )
    {
        Vector3 _;
        return FindTerrain<T>( nearLocation, out hitPoint, out _ );
    }

    public static T FindTerrain<T>( Vector3 nearLocation, out Vector3 hitPoint, out Vector3 normalDirection )
    {
        // Bit shift the index of the layer (8: Connected terrains) to get a bit mask
        int layerMask = 1 << 8;

        RaycastHit hit;
        // Check from a point really high above us, in the downward direction (in case we are below terrain)
        if( Physics.Raycast( nearLocation + 400 * Vector3.up, Vector3.down, out hit, Mathf.Infinity, layerMask ) )
        {
            T foundTerrain = hit.transform.GetComponentInParent<T>();
            hitPoint = hit.point;
            normalDirection = hit.normal;
            if( foundTerrain != null )
            {
                return foundTerrain;
            }
        }

        // ~ null
        hitPoint = default(Vector3);
        normalDirection = default(Vector3);
        return default(T);
    }

    public static void FlattenTerrainData( TerrainData td )
    {
        float[,] newHeights = new float[td.heightmapWidth, td.heightmapHeight];
        td.SetHeights( 0, 0, newHeights );
    }

    public static bool AboveLayer( Vector3 position, int layer )
    {
        Vector3 _;
        return AboveLayer( position, layer, out _ );
    }

    public static bool AboveLayer( Vector3 position, int layer, out Vector3 hitPoint )
    {
        // Bit shift the index of the layer (8: Connected terrains) to get a bit mask
        int layerMask = 1 << layer;

        RaycastHit hit;
        // Check from a point really high above us, in the downward direction (in case we are below terrain)
        if( Physics.Raycast( position, Vector3.down, out hit, Mathf.Infinity, layerMask ) )
        {
            hitPoint = hit.point;
            return true;
        }
        else
        {
            hitPoint = default( Vector3 );
            return false;
        }
    }

    
}
