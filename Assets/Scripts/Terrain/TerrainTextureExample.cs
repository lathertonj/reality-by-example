using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class TerrainTextureExample : MonoBehaviour , TouchpadLeftRightClickInteractable , TriggerGrabMoveInteractable , GripPlaceDeleteInteractable , IPunInstantiateMagicCallback
{
    private static List< TerrainTextureExample > allExamples = new List< TerrainTextureExample >();

    public Material[] myMaterials;
    public Material[] myHintMaterials;

    [HideInInspector] public double[] myValues = new double[4];
    [HideInInspector] public string myLabel = "";
    [HideInInspector] public int myCurrentValue;
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
        return TerrainUtility.FindTerrain<ConnectedTerrainTextureController>( transform.position );
    }

    void GripPlaceDeleteInteractable.JustPlaced()
    {
        ConnectedTerrainTextureController maybeTerrain = FindTerrain();
        
        if( maybeTerrain == null )
        {
            PhotonView maybeNetworked = GetComponent<PhotonView>();
            if( maybeNetworked != null )
            {
                PhotonNetwork.Destroy( maybeNetworked );
            }
            else
            {
                Destroy( gameObject );
            }
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

    void IPunInstantiateMagicCallback.OnPhotonInstantiate( PhotonMessageInfo info )
    {
        // check who this came from
        PhotonView photonView = GetComponent<PhotonView>();
        if( !photonView.IsMine && PhotonNetwork.IsConnected )
        {
            // this example came from someone else
            ConnectedTerrainTextureController maybeTerrain = FindTerrain();
            if( maybeTerrain != null )
            {
                // inform this terrain that I exist, but don't rescan
                ManuallySpecifyTerrain( maybeTerrain );
                myTerrain.ProvideExample( this, false );
            }
        }
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

    public void SwitchTo( int newValue )
    {
        while( myCurrentValue != newValue )
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
