using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorablePlane : MonoBehaviour
{
    public Color32 myLowColor, myHighColor;
    public float distanceAboveTerrain = 1f;
    public int pointsPerSide = 26;
    Mesh myMesh;
    float myReferenceData;
    private static ColorablePlane thePlane;

    ColorablePlaneDataSource myDataSource;

    // Start is called before the first frame update
    void Awake()
    {
        ConstructMesh();
        thePlane = this;
        this.gameObject.SetActive( false );
    }

    public static void SetDataSource( ColorablePlaneDataSource source, float referenceData )
    {
        thePlane.myDataSource = source;
        thePlane.myReferenceData = referenceData;

        // enable the plane
        thePlane.gameObject.SetActive( true );

        // set colors
        UpdateColors();
    }

    public static void ClearDataSource( ColorablePlaneDataSource source )
    {
        // disable if it's the most recent one
        if( thePlane.myDataSource == source )
        {
            // disable the plane
            thePlane.gameObject.SetActive( false );
        }
    }

    public static void UpdateColors()
    {
        thePlane.UpdateMyColors();
    }

    private void UpdateMyColors()
    {
        if( myDataSource == null ) return;

        Vector3[] vertices = myMesh.vertices;
        Color32[] newColors = new Color32[ vertices.Length ];

        for (int i = 0; i < vertices.Length; i++)
        {
            // update height of position
            Vector3 closestPointOnTerrain;
            Vector3 worldPoint = transform.TransformPoint( vertices[i] );
            TerrainUtility.FindTerrain<ConnectedTerrainController>( worldPoint, out closestPointOnTerrain );
            vertices[i] = transform.InverseTransformPoint( closestPointOnTerrain + distanceAboveTerrain * Vector3.up );

            // set color based on this position
            newColors[i] = Color.Lerp( myLowColor, myHighColor, myDataSource.Intensity0To1( worldPoint, myReferenceData ) );
        }

        // assign the arrays of positions and colors back to the mesh
        myMesh.vertices = vertices;
        myMesh.RecalculateNormals();
        myMesh.RecalculateTangents();
        myMesh.RecalculateBounds();
        myMesh.colors32 = newColors;
    }

    // Copied shamelessly from an earlier project of mine.
    private void ConstructMesh()
    {
        myMesh = new Mesh();
        Vector3[] newVertices = new Vector3[pointsPerSide * pointsPerSide];
        Vector2[] newUVs = new Vector2[pointsPerSide * pointsPerSide];
        int[] newTriangles = new int[3 * 2 * (2 * pointsPerSide) * (2 * pointsPerSide )];
        Color32[] newColors = new Color32[ newVertices.Length ];
        // vertices
        // (plane is originally 10 units to a side)
        float vertexScale = 10.0f / ( pointsPerSide - 1 );
        for( int x = 0; x < pointsPerSide ; x++ )
        {
            for( int z = 0; z < pointsPerSide; z++ )
            {
                newVertices[x + z * pointsPerSide] = new Vector3((x - ((int) pointsPerSide/2)) * vertexScale, 0, (z - ((int) pointsPerSide/2)) * vertexScale);
                newUVs[x + z * pointsPerSide] = new Vector2( x * 1.0f / pointsPerSide, z * 1.0f / pointsPerSide );
                newColors[x + z * pointsPerSide] = myLowColor;
            }
        }
        // triangles
        int triangleIndex = 0;
        for( int x = 0; x < pointsPerSide - 1; x++ )
        {
            for( int z = 0; z < pointsPerSide - 1; z++ )
            {
                newTriangles[triangleIndex] = x + z * pointsPerSide; triangleIndex++;
                newTriangles[triangleIndex] = x + (z + 1) * pointsPerSide; triangleIndex++;
                newTriangles[triangleIndex] = x + 1 + z * pointsPerSide; triangleIndex++;

                newTriangles[triangleIndex] = x + 1 + z * pointsPerSide; triangleIndex++;
                newTriangles[triangleIndex] = x + (z + 1) * pointsPerSide; triangleIndex++;
                newTriangles[triangleIndex] = x + 1 + (z + 1) * pointsPerSide; triangleIndex++;
            }
        }
        myMesh.vertices = newVertices;
        myMesh.uv = newUVs;
        myMesh.triangles = newTriangles;
        myMesh.colors32 = newColors;
        myMesh.RecalculateNormals();
        myMesh.RecalculateTangents();
        myMesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = myMesh;
    }
}

public interface ColorablePlaneDataSource
{
    float Intensity0To1( Vector3 worldPos, float referenceData );
}