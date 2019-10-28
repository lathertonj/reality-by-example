using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnderTerrainController : MonoBehaviour
{

    private int verticesPerSide;
    private float spaceBetweenVertices;

    private Vector3 IndicesToCoordinates( int x, int z )
    {
        // 0, 0 is bottom left corner, NOT center
        return new Vector3( ( x - ( verticesPerSide / 2 ) ) * spaceBetweenVertices, 0, ( z - ( verticesPerSide / 2 ) ) * spaceBetweenVertices );
    }

    private Vector2Int CoordinatesToClosestIndices( Vector3 coords )
    {
        int bestX = (int)( coords.x / spaceBetweenVertices ) + verticesPerSide / 2;
        int bestZ = (int)( coords.z / spaceBetweenVertices ) + verticesPerSide / 2;

        return new Vector2Int( bestX, bestZ );
    }

    public void ConstructMesh( int verticesPerSide, float spaceBetweenVertices )
    {
        this.verticesPerSide = verticesPerSide;
        this.spaceBetweenVertices = spaceBetweenVertices;
        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        Vector3[] newVertices = new Vector3[verticesPerSide * verticesPerSide];
        Vector2[] newUVs = new Vector2[verticesPerSide * verticesPerSide];
        int[] newTriangles = new int[3 * 2 * ( 2 * verticesPerSide ) * ( 2 * verticesPerSide )];
        // vertices
        for( int x = 0; x < verticesPerSide; x++ )
        {
            for( int z = 0; z < verticesPerSide; z++ )
            {
                newVertices[x + z * verticesPerSide] = IndicesToCoordinates( x, z );
                newUVs[x + z * verticesPerSide] = new Vector2( x * 1.0f / verticesPerSide, z * 1.0f / verticesPerSide );
            }
        }
        // triangles
        int triangleIndex = 0;
        for( int x = 0; x < verticesPerSide - 1; x++ )
        {
            for( int z = 0; z < verticesPerSide - 1; z++ )
            {
                newTriangles[triangleIndex] = x + z * verticesPerSide; triangleIndex++;
                newTriangles[triangleIndex] = x + 1 + z * verticesPerSide; triangleIndex++;
                newTriangles[triangleIndex] = x + ( z + 1 ) * verticesPerSide; triangleIndex++;

                newTriangles[triangleIndex] = x + 1 + z * verticesPerSide; triangleIndex++;
                newTriangles[triangleIndex] = x + 1 + ( z + 1 ) * verticesPerSide; triangleIndex++;
                newTriangles[triangleIndex] = x + ( z + 1 ) * verticesPerSide; triangleIndex++;
            }
        }
        mesh.vertices = newVertices;
        mesh.uv = newUVs;
        mesh.triangles = newTriangles;
        mesh.RecalculateNormals();
    }

    public void SetHeight( float[,] heights, float heightMultiplier )
    {
        Mesh m = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = m.vertices;

        // recompute height
        for( int i = 0; i < vertices.Length; i++ )
        {
            Vector2Int coords = CoordinatesToClosestIndices( vertices[i] );
            // TODO: multiply by -1? how would that help? that would make it do the reverse underneath instead of just being flipped normals... right?
            vertices[i].y = heightMultiplier * heights[ coords.y, coords.x ];
        }

        // set vertices
        m.vertices = vertices;
        m.RecalculateNormals();
        m.RecalculateTangents();
        m.RecalculateBounds();
    }

}
