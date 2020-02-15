using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlatWaterWaves : MonoBehaviour
{
    public Vector2 direction = Vector2.left;
    public float maxHeight = 0.7f;
    public float width = 1f;
    public float speed = 0.1f;
    private float progress;
    private Vector2 position = Vector2.zero;
    private Vector2 angle;
    private Vector2 perpAngle;
    //public float worldRadius = 100;  // ???
    //public float positionCutoff = 1.5f;
    private MeshFilter myMesh;

    public void Start()
    {
        // gesture direction will be used for a speed so normalize it for now
        direction = direction / direction.magnitude;

        // reset position
        position = Vector2.zero;

        // set angle
        angle = direction;
        perpAngle = RotateVector2( direction, 90 );

        progress = 0.0f;

        myMesh = GetComponent<MeshFilter>();
        ConstructMesh();
    }

    public void Update()
    {
        float dt = Time.deltaTime;
        position += speed * dt * angle;
        
        // wrap ????
        //if( position.magnitude > positionCutoff * worldRadius )
        //{
        //    position.x = -worldRadius * Mathf.Sign( position.x );
        //    position.y = -worldRadius * Mathf.Sign( position.y );
        //}

        // increment progress
        progress = progress + 0.002f * ( 60 * dt ) * 3;
        if( progress > 1.0f ) { progress -= 1.0f; }

        // 
        RecomputeHeight();
    }

    public void RecomputeHeight()
    {
        // get vertices
        Mesh m = myMesh.mesh;
        Vector3[] vertices = m.vertices;

        for( int i = 0; i < vertices.Length; i++ )
        {
            vertices[i].y = maxHeight * HeightAtPoint( vertices[i].x, vertices[i].z ); ;
        }

        // set vertices
        m.vertices = vertices;
        m.RecalculateNormals();
        m.RecalculateTangents();
    }

    // "y" === z 
    private float HeightAtPoint( float x, float y )
    {
        // define a line by 2 points
        Vector2 p1 = position;
        Vector2 p2 = position + perpAngle;

        // compute distance from input point to the line representing the wave
        float dist = Mathf.Abs( x * ( p2.y - p1.y ) - y * ( p2.x - p1.x ) + p2.x * p1.y - p2.y * p1.x ) / ( p2 - p1 ).magnitude;

        // height is NO LONGER a gaussian function of distance to that line
        //return Mathf.Exp( -1 * Mathf.Pow( dist, 2 ) / width );
        return SmoothPeak( dist, width );
    }

    private float SmoothPeak( float dist, float peakWidth )
    {
        float sharpnessConstant = 0.55f / 2;
        // map dist to [-2pi, 2pi]: first mod by peakWidth so in [0, peakWidth]
        // then divide by peakWidth so in [0, 1]
        // then lerp to [-2pi, 2pi]
        float normDist = Mathf.Lerp( -2 * Mathf.PI, 2 * Mathf.PI, ( dist % peakWidth ) / peakWidth );

        float height = ( 1.0f - sharpnessConstant ) / Mathf.Sqrt( 1 + sharpnessConstant * sharpnessConstant - 2 * sharpnessConstant * Mathf.Cos( normDist ) );

        // this height goes from 0.568627 to 1, for sharpnessconstant = 0.55f/2
        // map it to [0, 1]
        return Mathf.InverseLerp( 0.568627f, 1, height );
    }

    Vector2 RotateVector2( Vector2 v, float degrees )
    {
        float sindeg = Mathf.Sin( degrees * Mathf.Deg2Rad );
        float cosdeg = Mathf.Cos( degrees * Mathf.Deg2Rad );
        return new Vector2( cosdeg * v.x - sindeg * v.y, sindeg * v.x + cosdeg * v.y );
    }

    // Copied shamelessly from an earlier project of mine.
    private void ConstructMesh()
    {
        Mesh mesh = new Mesh();
        myMesh.mesh = mesh;
        int oceanSize = 26;//51; // so, N vertices per side (originally 201 but that's too much work)
        Vector3[] newVertices = new Vector3[oceanSize * oceanSize];
        Vector2[] newUVs = new Vector2[oceanSize * oceanSize];
        int[] newTriangles = new int[3 * 2 * (2 * oceanSize) * (2 * oceanSize )];
        // vertices
        float vertexScale = 0.05f * 2 * 2 * 2;// cus reduced from "200ish" to "25ish"
        for( int x = 0; x < oceanSize ; x++ )
        {
            for( int z = 0; z < oceanSize; z++ )
            {
                newVertices[x + z * oceanSize] = new Vector3((x - ((int) oceanSize/2)) * vertexScale, 0, (z - ((int) oceanSize/2)) * vertexScale);
                newUVs[x + z * oceanSize] = new Vector2( x * 1.0f / oceanSize, z * 1.0f / oceanSize );
            }
        }
        // triangles
        int triangleIndex = 0;
        for( int x = 0; x < oceanSize - 1; x++ )
        {
            for( int z = 0; z < oceanSize - 1; z++ )
            {
                newTriangles[triangleIndex] = x + z * oceanSize; triangleIndex++;
                newTriangles[triangleIndex] = x + (z + 1) * oceanSize; triangleIndex++;
                newTriangles[triangleIndex] = x + 1 + z * oceanSize; triangleIndex++;

                newTriangles[triangleIndex] = x + 1 + z * oceanSize; triangleIndex++;
                newTriangles[triangleIndex] = x + (z + 1) * oceanSize; triangleIndex++;
                newTriangles[triangleIndex] = x + 1 + (z + 1) * oceanSize; triangleIndex++;
            }
        }
        mesh.vertices = newVertices;
        mesh.uv = newUVs;
        mesh.triangles = newTriangles;
    }
}
