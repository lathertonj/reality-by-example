using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainTextureExample : MonoBehaviour
{
    public Material[] myMaterials;

    [HideInInspector] public double[] myValues = new double[4];
    public string myLabel = "";
    private int myCurrentValue;
    private MeshRenderer myRenderer;

    [HideInInspector] public SerializableTerrainTextureExample serializableObject;

    void Awake()
    {
        serializableObject = new SerializableTerrainTextureExample();

        myCurrentValue = 0;
        myRenderer = GetComponentInChildren<MeshRenderer>();
        UpdateMaterial();
        UpdatePosition();
    }

    public void SwitchToNextMaterial()
    {
        // compute new value for IML
        myCurrentValue++;
        myCurrentValue %= myValues.Length;

        // change material and update IML
        UpdateMaterial();
    }

    public void SwitchToPreviousMaterial()
    {
        // compute new index
        myCurrentValue = myCurrentValue - 1 + myValues.Length;
        myCurrentValue %= myValues.Length;

        // change material and update IML
        UpdateMaterial();
    }

    public void UpdatePosition()
    {
        serializableObject.position = transform.position;
    }

    // Update is called once per frame
    void UpdateMaterial()
    {
        // display
        myRenderer.material = myMaterials[ myCurrentValue ];

        // store for serialize
        serializableObject.material = myCurrentValue;

        // store for IML
        for( int i = 0; i < myValues.Length; i++ ) { myValues[i] = 0; }
        myValues[ myCurrentValue ] = 1;
        myLabel = myCurrentValue.ToString();
    }

    public void ResetFromSerial( SerializableTerrainTextureExample serialized )
    {
        transform.position = serialized.position;
        myCurrentValue = serialized.material;
        UpdatePosition();
        UpdateMaterial();
    }
}

[System.Serializable]
public class SerializableTerrainTextureExample
{
    public Vector3 position;
    public int material;
}
