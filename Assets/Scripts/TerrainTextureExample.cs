using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainTextureExample : MonoBehaviour
{
    public Material[] myMaterials;

    [HideInInspector] public double[] myValues = new double[4];
    [HideInInspector] public string myLabel = "";
    private int myCurrentValue;
    private MeshRenderer myRenderer;

    [HideInInspector] public SerializableTerrainTextureExample serializableObject;

    void Awake()
    {
        serializableObject = new SerializableTerrainTextureExample();

        for( int i = 0; i < myValues.Length; i++ ) { myValues[i] = 0; }
        myCurrentValue = 0;
        myLabel = myCurrentValue.ToString();
        myValues[ myCurrentValue ] = 1;
        myRenderer = GetComponentInChildren<MeshRenderer>();
        UpdateMaterial();
        UpdatePosition();
    }

    public void SwitchToNextMaterial()
    {
        // compute new value for IML
        myValues[ myCurrentValue ] = 0;
        myCurrentValue++;
        myCurrentValue %= myValues.Length;
        myValues[ myCurrentValue ] = 1;
        myLabel = myCurrentValue.ToString();

        // also, change material
        UpdateMaterial();
    }

    public void SwitchToPreviousMaterial()
    {
        // compute new value for IML
        myValues[ myCurrentValue ] = 0;
        myCurrentValue = myCurrentValue - 1 + myValues.Length;
        myCurrentValue %= myValues.Length;
        myValues[ myCurrentValue ] = 1;
        myLabel = myCurrentValue.ToString();

        // also, change material
        UpdateMaterial();
    }

    public void UpdatePosition()
    {
        serializableObject.position = transform.position;
    }

    // Update is called once per frame
    void UpdateMaterial()
    {
        myRenderer.material = myMaterials[ myCurrentValue ];
        serializableObject.material = myCurrentValue;
    }


}

[System.Serializable]
public class SerializableTerrainTextureExample
{
    public Vector3 position;
    public int material;
}
