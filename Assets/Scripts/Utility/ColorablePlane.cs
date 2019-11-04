using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorablePlane : MonoBehaviour
{
    public Color32 myLowColor, myHighColor;
    Mesh myMesh;

    ColorablePlaneDataSource myDataSource;

    // Start is called before the first frame update
    void Start()
    {
        myMesh = GetComponent<MeshFilter>().mesh;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetDataSource( ColorablePlaneDataSource source )
    {
        myDataSource = source;
    }

    public void UpdateColors()
    {
        if( myDataSource == null ) return;

        Vector3[] vertices = myMesh.vertices;
        Color32[] newColors = new Color32[ vertices.Length ];

        for (int i = 0; i < vertices.Length; i++)
        {
            newColors[i] = Color.Lerp( myLowColor, myHighColor, myDataSource.Intensity0To1( transform.TransformPoint( vertices[i] ) ) );
        }

        // assign the array of colors to the Mesh.
        myMesh.colors32 = newColors;
    }
}

public interface ColorablePlaneDataSource
{
    float Intensity0To1( Vector3 worldPos );
}