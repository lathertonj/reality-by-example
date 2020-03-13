using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainTextureExample : MonoBehaviour , TouchpadLeftRightClickInteractable , TriggerGrabMoveInteractable , GripPlaceDeleteInteractable
{
    private static List< TerrainTextureExample > allExamples = new List< TerrainTextureExample >();

    public Material[] myMaterials;
    public Material[] myHintMaterials;

    [HideInInspector] public double[] myValues = new double[4];
    public string myLabel = "";
    private int myCurrentValue;
    private MeshRenderer myRenderer;

    [HideInInspector] private ConnectedTerrainTextureController myTerrain;

    void Awake()
    {
        myCurrentValue = 0;
        myRenderer = GetComponentInChildren<MeshRenderer>();
        UpdateMaterial();
    }

    private void SwitchToNextMaterial()
    {
        // compute new value for IML
        myCurrentValue++;
        myCurrentValue %= myValues.Length;

        // change material and update IML
        UpdateMaterial();
    }

    private void SwitchToPreviousMaterial()
    {
        // compute new index
        myCurrentValue = myCurrentValue - 1 + myValues.Length;
        myCurrentValue %= myValues.Length;

        // change material and update IML
        UpdateMaterial();
    }

    // Update is called once per frame
    void UpdateMaterial()
    {
        // display
        myRenderer.material = myMaterials[ myCurrentValue ];
        myHint.material = myHintMaterials[ myCurrentValue ];

        // store for IML
        for( int i = 0; i < myValues.Length; i++ ) { myValues[i] = 0; }
        myValues[ myCurrentValue ] = 1;
        myLabel = myCurrentValue.ToString();
    }


    private ConnectedTerrainTextureController FindTerrain()
    {
        // Bit shift the index of the layer (8: Connected terrains) to get a bit mask
        int layerMask = 1 << 8;

        RaycastHit hit;
        // Check from a point really high above us, in the downward direction (in case we are below terrain)
        if( Physics.Raycast( transform.position + 400 * Vector3.up, Vector3.down, out hit, Mathf.Infinity, layerMask ) )
        {
            return hit.transform.GetComponentInParent<ConnectedTerrainTextureController>();
        }
        return null;
    }

    void GripPlaceDeleteInteractable.JustPlaced()
    {
        ConnectedTerrainTextureController maybeTerrain = FindTerrain();
        
        if( maybeTerrain == null )
        {
            Destroy( gameObject );
        }
        else
        {
            ManuallySpecifyTerrain( maybeTerrain );
            myTerrain.ProvideExample( this );
        }
    }

    public void ManuallySpecifyTerrain( ConnectedTerrainTextureController c )
    {
        myTerrain = c;
        allExamples.Add( this );
    }

    void GripPlaceDeleteInteractable.AboutToBeDeleted()
    {
        myTerrain.ForgetExample( this );
        allExamples.Remove( this );
    }

    void TouchpadLeftRightClickInteractable.InformOfLeftClick()
    {
        SwitchToPreviousMaterial();
        myTerrain.RescanProvidedExamples();
    }

    void TouchpadLeftRightClickInteractable.InformOfRightClick()
    {
        SwitchToNextMaterial();
        myTerrain.RescanProvidedExamples();
    }

    void TriggerGrabMoveInteractable.InformOfTemporaryMovement( Vector3 currentPosition )
    {
        // do nothing (don't update terrain while moving temporarily)
    }

    void TriggerGrabMoveInteractable.FinalizeMovement( Vector3 endPosition )
    {
        // see if we're on a new terrain
        ConnectedTerrainTextureController newTerrain = FindTerrain();
        if( newTerrain != null && newTerrain != myTerrain )
        {
            myTerrain.ForgetExample( this );
            newTerrain.ProvideExample( this );
            myTerrain = newTerrain;
        }
        else
        {
            // stick with myTerrain
            myTerrain.RescanProvidedExamples();
        }
    }

    public void Randomize( bool informMyTerrain = false )
    {
        // choose a random material
        int n = Random.Range( 0, myMaterials.Length );
        for( int i = 0; i < n; i++ ) { SwitchToNextMaterial(); }

        // inform my terrain
        if( informMyTerrain )
        {
            myTerrain.RescanProvidedExamples();
        }
    }

    public void CopyFrom( TerrainTextureExample from )
    {
        while( myCurrentValue != from.myCurrentValue )
        {
            SwitchToNextMaterial();
        }
    }


    public static void ShowHints( float pauseTimeBeforeFade )
    {
        foreach( TerrainTextureExample e in allExamples )
        {
            e.ShowHint( pauseTimeBeforeFade );
        }
    }

    public MeshRenderer myHint;
    private Coroutine hintCoroutine;
    private void ShowHint( float pauseTimeBeforeFade )
    {
        StopHintAnimation();
        hintCoroutine = StartCoroutine( AnimateHint.AnimateHintFade( myHint, pauseTimeBeforeFade ) );
    }

    private void StopHintAnimation()
    {
        if( hintCoroutine != null )
        {
            StopCoroutine( hintCoroutine );
        }
    }
    

    public SerializableTerrainTextureExample Serialize( Transform myTerrain )
    {
        SerializableTerrainTextureExample serial = new SerializableTerrainTextureExample();
        serial.localPosition = myTerrain.InverseTransformPoint( transform.position );
        serial.material = myCurrentValue;
        return serial;
    }

    public void ResetFromSerial( SerializableTerrainTextureExample serialized, Transform myTerrain )
    {
        transform.position = myTerrain.TransformPoint( serialized.localPosition );
        myCurrentValue = serialized.material;
        UpdateMaterial();
    }
}

[System.Serializable]
public class SerializableTerrainTextureExample
{
    public Vector3 localPosition;
    public int material;
}
