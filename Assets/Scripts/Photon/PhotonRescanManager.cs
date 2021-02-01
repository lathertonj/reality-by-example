using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhotonRescanManager : MonoBehaviour
{
    private static PhotonRescanManager theManager;
    public float lazyRescanTime = 2f;
    public float checkFrequency = 0.25f;

    private Dictionary< IPhotonExampleRescanner, float > rescanTimes;
    private List< IPhotonExampleRescanner > _toRemove;
    private Dictionary< IPhotonExampleRescanner, float > _toAdd;

    void Awake()
    {
        theManager = this;
        rescanTimes = new Dictionary<IPhotonExampleRescanner, float>();
        _toAdd = new Dictionary<IPhotonExampleRescanner, float>();
        _toRemove = new List<IPhotonExampleRescanner>();
    }

    void Start()
    {
        StartCoroutine( CheckRescans() );
    }

    IEnumerator CheckRescans()
    {
        while( true )
        {
            // add any waiting ones
            foreach( KeyValuePair< IPhotonExampleRescanner, float > pair in _toAdd )
            {
                rescanTimes[pair.Key] = pair.Value;
            }
            _toAdd.Clear();

            // rescan ones whose time has been reached
            foreach( KeyValuePair< IPhotonExampleRescanner, float > pair in rescanTimes )
            {
                if( pair.Value <= Time.time )
                {
                    // rescan
                    pair.Key.RescanProvidedExamples();
                    // wait for rescan to finish
                    for( int i = 0; i < pair.Key.NumFramesToRescan(); i++ ) { yield return null; }
                    // forget this later
                    _toRemove.Add( pair.Key );
                    // don't do any more rescans right now
                    break;
                }
            }

            // forget the ones that rescanned
            foreach( IPhotonExampleRescanner r in _toRemove )
            {
                rescanTimes.Remove( r );
            }
            _toRemove.Clear();

            yield return new WaitForSecondsRealtime( checkFrequency );
        }
    }

    // assign the rescanner to be rescanned in the future,
    // overwriting any possible earlier time
    public static void LazyRescan( IPhotonExampleRescanner r )
    {
        float futureTime = Time.time + theManager.lazyRescanTime;
        if( theManager.rescanTimes.ContainsKey( r ) )
        {
            // update the time to be later
            theManager.rescanTimes[r] = futureTime;
        }
        else
        {
            // avoid updating the collection of keys while it's possibly being enumerated over
            theManager._toAdd[r] = futureTime;
        }
    }
}
