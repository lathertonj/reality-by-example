using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainController : MonoBehaviour
{

    public GameObject topLand;
    public GameObject bottomLand;
    public GameObject vertexDebugPrefab;

    public Transform examplePointsContainer;

    private int verticesPerSide = 101;
    private float landSize;
    private float spaceBetweenVertices;
    public bool showDebugMarkers = false;

    // regression
    private RapidMixRegression myRegression;
    private List<Transform> myRegressionExamples;
    private bool haveTrained = false;

    public void ProvideExample( Transform example )
    {
        // remember
        myRegressionExamples.Add( example );

        // recompute
        RescanProvidedExamples();
    }

    public void RescanProvidedExamples()
    {
        // train and recompute
        TrainRegression();
        ComputeLandHeight();
    }

    private void TrainRegression()
    {
        // only do this when we have examples
        if( myRegressionExamples.Count > 0 )
        {
            // reset the regression
            myRegression.ResetRegression();

            // rerecord all points
            foreach( Transform example in myRegressionExamples )
            {
                // world to local point
                Vector3 point = transform.InverseTransformPoint( example.position );

                // remember
                myRegression.RecordDataPoint( InputVector( point.x, point.z ), new double[] { point.y } );
            }

            // train
            myRegression.Train();

            // remember
            haveTrained = true;
        }
    }

    private void ComputeLandHeight()
    {
        // only do this if we have trained at least once before
        if( haveTrained )
        {
            ComputeLandHeight( topLand, false );
            ComputeLandHeight( bottomLand, true );
        }
    }

    private double[] InputVector( float x, float z )
    {
        // kernel method
        return new double[] { x, z, x * x, z * z, x * z, x * x * x, z * z * z, x * x * z, x * z * z };
        // return new double[] { x, z, x*x, z*z, x*z };
    }

    // Use this for initialization
    void Start()
    {
        // grab component reference
        myRegression = GetComponent<RapidMixRegression>();

        // initialize list
        myRegressionExamples = new List<Transform>();

        // compute sizes
        landSize = 10; // it is invariant to scale. scaling up doesn't affect the computations here.
        spaceBetweenVertices = landSize / ( verticesPerSide - 1 );

        // construct meshes
        ConstructMesh( topLand );
        ConstructMesh( bottomLand );


        if( showDebugMarkers )
        {
            CreateDebugMarkers();
        }

        foreach( Transform example in examplePointsContainer )
        {
            ProvideExample( example );
        }

    }

    void CreateDebugMarkers()
    {
        Mesh m = topLand.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = m.vertices;
        // for( int i = 0; i < vertices.Length; i++ )
        // {
        //     Instantiate( vertexDebugPrefab, new Vector3( vertices[i].x, 2, vertices[i].z), Quaternion.identity );
        // }
        Instantiate( vertexDebugPrefab, new Vector3( -spaceBetweenVertices, 0, -spaceBetweenVertices ), Quaternion.identity );
        Instantiate( vertexDebugPrefab, new Vector3( -spaceBetweenVertices, 0, spaceBetweenVertices ), Quaternion.identity );
        Instantiate( vertexDebugPrefab, new Vector3( spaceBetweenVertices, 0, -spaceBetweenVertices ), Quaternion.identity );
        Instantiate( vertexDebugPrefab, new Vector3( spaceBetweenVertices, 0, spaceBetweenVertices ), Quaternion.identity );
    }

    // Update is called once per frame
    void Update()
    {

    }

    private Vector3 IndicesToCoordinates( int x, int z )
    {
        // 0, 0 is bottom left corner, NOT center
        return new Vector3( ( x - ( verticesPerSide / 2 ) ) * spaceBetweenVertices, 0, ( z - ( verticesPerSide / 2 ) ) * spaceBetweenVertices );
    }

    // // unused?
    // private Vector2Int CoordinatesToClosestIndices( Vector3 coords )
    // {
    //     int bestX = (int) ( coords.x / spaceBetweenVertices ) + verticesPerSide / 2;
    //     int bestZ = (int) ( coords.z / spaceBetweenVertices ) + verticesPerSide / 2;

    //     return new Vector2Int( bestX, bestZ );
    // }

    private void ConstructMesh( GameObject o )
    {
        Mesh mesh = new Mesh();
        o.GetComponent<MeshFilter>().mesh = mesh;
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

    private void ComputeLandHeight( GameObject o, bool reverseHeight )
    {
        Mesh m = o.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = m.vertices;

        // recompute height
        for( int i = 0; i < vertices.Length; i++ )
        {
            vertices[i].y = (float)myRegression.Run( InputVector( vertices[i].x, vertices[i].z ) )[0];
        }

        if( reverseHeight )
        {
            for( int i = 0; i < vertices.Length; i++ )
            {
                vertices[i].y *= -1;
            }
        }

        // set vertices
        m.vertices = vertices;
        m.RecalculateNormals();
    }



}
