using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class TerrainGISExample : MonoBehaviour, TouchpadUpDownInteractable, TouchpadLeftRightClickInteractable, TriggerGrabMoveInteractable, GripPlaceDeleteInteractable , IPunInstantiateMagicCallback , IPhotonExample
{
    public enum GISType { Smooth = 0, Hilly = 1, River = 2, Boulder = 3, Mountain = 4, Spines = 5 };

    private static List< TerrainGISExample > allExamples = new List< TerrainGISExample >();
    

    [HideInInspector] public double[] myValues = new double[6];

    [HideInInspector] private ConnectedTerrainController myTerrain;
    // default to 1.0
    [HideInInspector] public float myValue = 1.0f;
    [HideInInspector] public GISType myType = GISType.Smooth;


    private TextMesh myText;

    void Awake()
    {
        myText = GetComponentInChildren<TextMesh>();
        for( int i = 0; i < myValues.Length; i++ ) { myValues[i] = 0; }
        UpdateMyValue( myType, myValue );
    }



    public void UpdateMyValue( GISType newType, float newValue )
    {
        // set previous one to zero
        myValues[ (int) myType ] = 0;

        // store
        myType = newType;

        // clamp to min / max
        myValue = Mathf.Clamp01( newValue );

        // store
        myValues[ (int) myType ] = myValue;

        // display according to mode
        switch( myType )
        {
            case GISType.Smooth:
                myText.text = string.Format( "Smooth: {0:0.00}", myValue );
                break;
            case GISType.Hilly:
                myText.text = string.Format( "Hilly: {0:0.00}", myValue );
                break;
            case GISType.River:
                myText.text = string.Format( "River: {0:0.00}", myValue );
                break;
            case GISType.Boulder:
                myText.text = string.Format( "Boulder: {0:0.00}", myValue );
                break;
            case GISType.Mountain:
                myText.text = string.Format( "Mountain: {0:0.00}", myValue );
                break;
            case GISType.Spines:
                myText.text = string.Format( "Spines: {0:0.00}", myValue );
                break;
        }
    }

    private void SwitchToNextGISType()
    {
        UpdateMyValue( myType.Next(), myValue );
    }

    private void SwitchToPreviousGISType()
    {
        UpdateMyValue( myType.Previous(), myValue );
    }

    private ConnectedTerrainController FindTerrain()
    {
        return TerrainUtility.FindTerrain<ConnectedTerrainController>( transform.position );
    }

    void GripPlaceDeleteInteractable.JustPlaced()
    {
        ConnectedTerrainController maybeTerrain = FindTerrain();

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

    public void ManuallySpecifyTerrain( ConnectedTerrainController c )
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
        SwitchToPreviousGISType();
        myTerrain.RescanProvidedExamples();
        // inform others on network
        this.AlertOthersToChanges();
    }

    void TouchpadLeftRightClickInteractable.InformOfRightClick()
    {
        SwitchToNextGISType();
        myTerrain.RescanProvidedExamples();
        // inform others on network
        this.AlertOthersToChanges();
    }

    void TriggerGrabMoveInteractable.InformOfTemporaryMovement( Vector3 currentPosition )
    {
        // do nothing (don't update terrain while moving temporarily)
    }

    void TriggerGrabMoveInteractable.FinalizeMovement( Vector3 endPosition )
    {
        // see if we're on a new terrain
        ConnectedTerrainController newTerrain = FindTerrain();
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
        // inform others on network
        this.AlertOthersToChanges();
    }



    void TouchpadUpDownInteractable.InformOfUpOrDownMovement( float verticalDisplacementSinceBeginning, float verticalDisplacementThisFrame )
    {
        float multiplier = 1f;
        if( verticalDisplacementThisFrame < 0 )
        {
            multiplier = verticalDisplacementThisFrame.MapClamp( -0.1f, 0f, 0.8f, 1f );
        }
        else
        {
            multiplier = verticalDisplacementThisFrame.MapClamp( 0f, 0.1f, 1f, 1.25f );
        }
        UpdateMyValue( myType, multiplier * myValue );
    }

    void TouchpadUpDownInteractable.FinalizeMovement()
    {
        // tell the controller to recompute tempo
        myTerrain.RescanProvidedExamples();
        // alert others on network
        this.AlertOthersToChanges();
    }


    public void Randomize( bool informMyTerrain = false )
    {
        // get random new value
        UpdateMyValue( myType, Random.Range( 0f, 1f ) );

        // get random new type (future proofed and thus somewhat skewed)
        int numSwitches = Random.Range( 0, 8 );
        for( int i = 0; i < numSwitches; i++ ) { SwitchToNextGISType(); }

        // inform my terrain
        if( informMyTerrain )
        {
            myTerrain.RescanProvidedExamples();
        }
        // alert others on network
        this.AlertOthersToChanges();
    }


    public void Perturb( float amount, bool informMyTerrain = false )
    {
        // get random new value
        UpdateMyValue( myType, myValue + Random.Range( -amount, amount ) );

        // don't get new type -- too drastic

        // inform my terrain
        if( informMyTerrain )
        {
            myTerrain.RescanProvidedExamples();
        }
        // alert others on network
        this.AlertOthersToChanges();
    }

    public void CopyFrom( TerrainGISExample other )
    {
        UpdateMyValue( other.myType, other.myValue );
    }



    public static void ShowHints( float pauseTimeBeforeFade )
    {
        foreach( TerrainGISExample e in allExamples )
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


    public SerializableTerrainGISExample Serialize( ConnectedTerrainController myTerrain )
    {
        SerializableTerrainGISExample serial = new SerializableTerrainGISExample();
        serial.localPosition = myTerrain.transform.InverseTransformPoint( transform.position );
        serial.type = myType;
        serial.value = myValue;
        return serial;
    }

    public void ResetFromSerial( SerializableTerrainGISExample serialized, ConnectedTerrainController myTerrain )
    {
        transform.position = myTerrain.transform.TransformPoint( serialized.localPosition );
        UpdateMyValue( serialized.type, serialized.value );
    }

    void IPhotonExample.AlertOthersToChanges()
    {
        AlertOthersToChanges();
    }

    void AlertOthersToChanges()
    {
        // if we have a PhotonView component...
        PhotonView maybeNetworked = GetComponent<PhotonView>();
        // and the corresponding object belongs to us and we're on the network
        if( maybeNetworked != null && maybeNetworked.IsMine && PhotonNetwork.IsConnected )
        {
            // then tell others they need to rescan
            maybeNetworked.RPC( "TerrainGISLazyRescan", RpcTarget.Others );
        }
    }

    [PunRPC]
    void TerrainGISLazyRescan()
    {
        // see if we've been moved to a new terrain
        ConnectedTerrainController newTerrain = FindTerrain();
        if( newTerrain != null && newTerrain != myTerrain )
        {
            myTerrain.ForgetExample( this, shouldRescan: false );
            newTerrain.ProvideExample( this, shouldRescan: false );
            PhotonRescanManager.LazyRescan( myTerrain );
            PhotonRescanManager.LazyRescan( newTerrain );
            myTerrain = newTerrain;
        }
        else
        {
            // stick with myTerrain
            PhotonRescanManager.LazyRescan( myTerrain );
        }
    }

    // photon: init
    void IPunInstantiateMagicCallback.OnPhotonInstantiate( PhotonMessageInfo info )
    {
        // check who this came from
        PhotonView photonView = GetComponent<PhotonView>();
        if( !photonView.IsMine && PhotonNetwork.IsConnected )
        {
            // this example came from someone else
            ConnectedTerrainController maybeTerrain = FindTerrain();
            if( maybeTerrain != null )
            {
                // inform this terrain that I exist
                ManuallySpecifyTerrain( maybeTerrain );
                myTerrain.ProvideExample( this, false );
                PhotonRescanManager.LazyRescan( myTerrain );
            }
        }
    }


    // handle photon.destroy
    void OnDestroy()
    {
        // if we have a PhotonView component...
        PhotonView maybeNetworked = GetComponent<PhotonView>();
        // and the corresponding object doesn't belong to us and we're on the network
        if( maybeNetworked != null && !maybeNetworked.IsMine && PhotonNetwork.IsConnected )
        {
            // then my terrain needs to forget me
            myTerrain.ForgetExample( this, shouldRescan: false );
            PhotonRescanManager.LazyRescan( myTerrain );
        }
    }
    
}


[System.Serializable]
public class SerializableTerrainGISExample
{
    public Vector3 localPosition;
    public TerrainGISExample.GISType type;
    public float value;
}


public static class GISExtensions
{
    // next and previous methods: this allows us to potentially eliminate some of the ones if we want to
    public static TerrainGISExample.GISType Next( this TerrainGISExample.GISType myEnum )
    {
        switch( myEnum )
        {
            case TerrainGISExample.GISType.Smooth:
                return TerrainGISExample.GISType.Hilly;
            case TerrainGISExample.GISType.Hilly:
                // disable river
                // return TerrainGISExample.GISType.River;
                return TerrainGISExample.GISType.Boulder;
            case TerrainGISExample.GISType.River:
                return TerrainGISExample.GISType.Boulder;
            case TerrainGISExample.GISType.Boulder:
                return TerrainGISExample.GISType.Mountain;
            case TerrainGISExample.GISType.Mountain:
                // disable spines
                // return TerrainGISExample.GISType.Spines;
                return TerrainGISExample.GISType.Smooth;
            case TerrainGISExample.GISType.Spines:
                return TerrainGISExample.GISType.Smooth;
            default:
                return TerrainGISExample.GISType.Smooth;
        }
    }

    // next and previous methods: this allows us to potentially eliminate some of the ones if we want to
    public static TerrainGISExample.GISType Previous( this TerrainGISExample.GISType myEnum )
    {
        switch( myEnum )
        {
            case TerrainGISExample.GISType.Smooth:
                // disable spines
                // return TerrainGISExample.GISType.Spines;
                return TerrainGISExample.GISType.Mountain;
            case TerrainGISExample.GISType.Hilly:
                return TerrainGISExample.GISType.Smooth;
            case TerrainGISExample.GISType.River:
                return TerrainGISExample.GISType.Hilly;
            case TerrainGISExample.GISType.Boulder:
                // disable river
                // return TerrainGISExample.GISType.River;
                return TerrainGISExample.GISType.Hilly;
            case TerrainGISExample.GISType.Mountain:
                return TerrainGISExample.GISType.Boulder;
            case TerrainGISExample.GISType.Spines:
                return TerrainGISExample.GISType.Mountain;
            default:
                return TerrainGISExample.GISType.Smooth;
        }
    }
}
