using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PhotonLaunchScript : MonoBehaviourPunCallbacks
{
    [SerializeField] private byte maxPlayersPerRoom = 4;
    private string gameVersion = "1";

    public static PhotonLaunchScript launcher;

    public static bool launchRescanInProgress = false;
    public RandomizeTerrain terrainInitializer;

    public static float delayRescanTime = 3f;

    void Awake()
    {
        launcher = this;
        // this makes sure we can use PhotonNetwork.LoadLevel() on the master client and all clients in the same room sync their level automatically
        PhotonNetwork.AutomaticallySyncScene = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        Connect();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Connect()
    {
        // we check if we are connected or not, we join if we are , else we initiate the connection to the server.
        if (PhotonNetwork.IsConnected)
        {
            AttemptToJoinRoom();
        }
        else
        {
            // #Critical, we must first and foremost connect to Photon Online Server.
            PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.GameVersion = gameVersion;
        }
    }

    private void AttemptToJoinRoom()
    {
        // #Critical we need at this point to attempt joining a Random Room. If it fails, we'll get notified in OnJoinRandomFailed() and we'll create one.
        PhotonNetwork.JoinRandomRoom();
    }

    
    public override void OnConnectedToMaster()
    {
        Debug.Log("OnConnectedToMaster() was called by PUN");
        AttemptToJoinRoom();
    }


    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarningFormat("OnDisconnected() was called by PUN with reason {0}", cause);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("OnJoinRandomFailed() was called -- no room available. Creating a room instead.");

        // TODO specify room options
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = maxPlayersPerRoom });
    }

    public override void OnJoinedRoom()
    {
        // note: this is called whether or not we create the rom
        Debug.Log("OnJoinedRoom() ws called -- now this client is in a room.");

        if( !PhotonNetwork.IsMasterClient )
        {
            // need to do initial rescan
            StartCoroutine( InitializeWorld() );
        }
    }

    public override void OnPlayerEnteredRoom( Player newPlayer )
    {
        // only if it's my room
        if( PhotonNetwork.IsMasterClient )
        {
            Debug.Log( "A player joined the room!" );
        }
    }

    // method for non-master client to do something with all available examples after joining room
    private IEnumerator InitializeWorld()
    {
        // pause any other rescans
        //launchRescanInProgress = true;

        // hopefully N second is enough time to receive all examples?
        yield return new WaitForSecondsRealtime( delayRescanTime );

        // Debug.Log( "starting rescan" );
        // rescan the world and wait
        // yield return StartCoroutine( terrainInitializer.RescanAll() );

        // we are done
        // launchRescanInProgress = false;
    }
}
