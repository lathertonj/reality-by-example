using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Photon.Pun;


public class SlowlySpawnPrefab : MonoBehaviour
{
    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean spawn;

    public SteamVR_Action_Boolean changeConeAngle;
    public SteamVR_Action_Vector2 coneWidth;
    private SteamVR_Behaviour_Pose controllerPose;
    public LayerMask mask;
    public Transform prefabToSpawn;
    public bool isPrefabNetworked;
    public float timeBetweenSpawns = 1f;
    private bool shouldSpawn = false;
    private float currentSpawnRadius = 0f;
    private Vector3 currentSpawnPosition = Vector3.zero;
    private bool isCurrentSpawnPositionValid = false;

    public UICone uiconePrefab;
    private UICone myUICone;
    public Vector3 uiConeLocalOffset;

    // Start is called before the first frame update
    void Start()
    {
        controllerPose = GetComponent<SteamVR_Behaviour_Pose>();

        myUICone = Instantiate( uiconePrefab, transform.position, Quaternion.identity, transform );
        myUICone.transform.localPosition = uiConeLocalOffset;
        myUICone.gameObject.SetActive( false );
    }

    // Update is called once per frame
    void Update()
    {
        // spawn or not spawn
        if( spawn.GetStateDown( handType ) )
        {
            StartCoroutine( SpawnObjects() );
        }
        else if( spawn.GetStateUp( handType ) )
        {
            StopSpawningObjects();
        }

        // show cone or not show cone and calculate cone position
        if( changeConeAngle.GetStateDown( handType ) )
        {
            // show the radius of influence
            myUICone.gameObject.SetActive( true );
        }

        if( changeConeAngle.GetState( handType ) )
        {
            // set the width of the cone
            Vector2 thumbPosition = coneWidth.GetAxis( handType );
            currentSpawnRadius = thumbPosition.y.PowMap( -1f, 1f, 0.5f, 15f, 2f );
            myUICone.SetSize( currentSpawnRadius * 2 );

            // set the length of the cone and find where the spawn position is
            RaycastHit hit;
            if( Physics.Raycast( controllerPose.transform.position, controllerPose.transform.forward, out hit, 2000, mask ) )
            {
                // set spawn position
                currentSpawnPosition = hit.point;

                // allow spawns
                isCurrentSpawnPositionValid = true;

                // set cone length
                myUICone.SetLength( hit.distance );
            }
            else 
            {
                // disallow spawns when our cone isn't hitting terrain
                isCurrentSpawnPositionValid = false;

                // make cone very long when not hitting terrain
                myUICone.SetLength( 300 );

            }
            // set cone angle
            myUICone.transform.rotation = Quaternion.LookRotation( controllerPose.transform.forward, controllerPose.transform.up );
        }

        if( changeConeAngle.GetStateUp( handType ) )
        {    
            // hide the radius of influence
            myUICone.gameObject.SetActive( false );
        }
    }

    IEnumerator SpawnObjects()
    {
        shouldSpawn = true;

        while( shouldSpawn )
        {
            // spawn a thing
            if( isCurrentSpawnPositionValid )
            {
                // calculate its position
                Vector3 newPosition = GetSpawnPoint();

                // calculate its rotation
                Quaternion newRotation = Quaternion.AngleAxis( Random.Range( 0, 360 ), Vector3.up );

                // spawn it there!
                if( isPrefabNetworked )
                {
                    GameObject newObject = PhotonNetwork.Instantiate( prefabToSpawn.name, newPosition, newRotation );
                }
                else
                {
                    Transform newObject = Instantiate( prefabToSpawn, newPosition, newRotation );
                }
            }

            // every so often
            yield return new WaitForSeconds( timeBetweenSpawns );
        }
    }

    void StopSpawningObjects()
    {
        // stop coroutine
        shouldSpawn = false;
    }

    private Vector3 GetSpawnPoint()
    {
        // calculate where it will go
        float angle = Random.Range( 0, 2 * Mathf.PI );
        float radius = Random.Range( 0, currentSpawnRadius );
        Vector3 spawnPoint = currentSpawnPosition + new Vector3(
            radius * Mathf.Cos( angle ),
            0,
            radius * Mathf.Sin( angle )
        );

        // find the y position of the terrain there
        RaycastHit hit;
        if( Physics.Raycast( spawnPoint + 600 * Vector3.up, Vector3.down, out hit, 2000, mask ) )
        {
            // set spawn position
            spawnPoint = hit.point;
        }

        return spawnPoint;
    }
}
