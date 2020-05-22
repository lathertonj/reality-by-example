using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestWolfAngle : MonoBehaviour
{

    public Transform towardThisObjectIsMyVelocity;
    public float hugTerrainHeight = 2f;

    public Transform visualizeMyForward;
    public Transform visualizeNormal;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 terrainNormal;
        transform.position = GetHugTerrainPoint( transform.position, out terrainNormal );
        transform.LookAt( towardThisObjectIsMyVelocity );
        // cross product gets a tangent to the normal
        // cross product with the left vector gets a tangent in roughly the forward direction
        Vector3 newForward = Vector3.Cross( terrainNormal, -transform.right );

        visualizeNormal.position = transform.position + 4 * terrainNormal;
        visualizeMyForward.position = transform.position + 4 * newForward;

        transform.rotation = Quaternion.LookRotation( newForward, Vector3.up ); 
    }

    private Vector3 GetHugTerrainPoint( Vector3 near, out Vector3 normalDirection )
    {
        Vector3 nearestPointOnTerrain;
        TerrainUtility.FindTerrain<Terrain>( near, out nearestPointOnTerrain, out normalDirection );
        return nearestPointOnTerrain + hugTerrainHeight * Vector3.up;
    }
}
